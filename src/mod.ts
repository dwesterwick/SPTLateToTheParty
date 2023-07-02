/* eslint-disable @typescript-eslint/naming-convention */
import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";
import { BotConversionHelper } from "./BotConversionHelper";
import { LootRankingGenerator } from "./LootRankingGenerator";
import { TraderAssortGenerator } from "./TraderAssortGenerator";

import { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import type { IPostAkiLoadMod } from "@spt-aki/models/external/IPostAkiLoadMod";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import type { DynamicRouterModService } from "@spt-aki/services/mod/dynamicRouter/DynamicRouterModService";

import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { ConfigServer } from "@spt-aki/servers/ConfigServer";
import { ILocationConfig, LootMultiplier } from "@spt-aki/models/spt/config/ILocationConfig";
import { IInRaidConfig } from "@spt-aki/models/spt/config/IInRaidConfig";
import { IBotConfig } from "@spt-aki/models/spt/config/IBotConfig";
import { IAirdropConfig } from "@spt-aki/models/spt/config/IAirdropConfig";
import { ITraderConfig } from "@spt-aki/models/spt/config/ITraderConfig";
import { ConfigTypes } from "@spt-aki/models/enums/ConfigTypes";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";
import { LocaleService } from "@spt-aki/services/LocaleService";
import { BotWeaponGenerator } from "@spt-aki/generators/BotWeaponGenerator";
import { HashUtil } from "@spt-aki/utils/HashUtil";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { TimeUtil } from "@spt-aki/utils/TimeUtil";
import { RandomUtil } from "@spt-aki/utils/RandomUtil";
import { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";
import { TraderController } from "@spt-aki/controllers/TraderController";
import { FenceService } from "@spt-aki/services/FenceService";
import { FenceBaseAssortGenerator } from "@spt-aki/generators/FenceBaseAssortGenerator";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { Traders } from "@spt-aki/models/enums/Traders";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";

const modName = "LateToTheParty";

class LateToTheParty implements IPreAkiLoadMod, IPostDBLoadMod, IPostAkiLoadMod
{
    private commonUtils: CommonUtils
    private botConversionHelper: BotConversionHelper
    private lootRankingGenerator: LootRankingGenerator
    private traderAssortGenerator: TraderAssortGenerator
    
    private logger: ILogger;
    private locationConfig: ILocationConfig;
    private inRaidConfig: IInRaidConfig;
    private iBotConfig: IBotConfig;
    private iAirdropConfig: IAirdropConfig;
    private iTraderConfig: ITraderConfig;
    private configServer: ConfigServer;
    private databaseServer: DatabaseServer;
    private databaseTables: IDatabaseTables;
    private vfs: VFS;
    private localeService: LocaleService;
    private botWeaponGenerator: BotWeaponGenerator;
    private hashUtil: HashUtil;
    private jsonUtil: JsonUtil;
    private timeutil: TimeUtil;
    private randomUtil: RandomUtil;
    private profileHelper: ProfileHelper;
    private httpResponseUtil: HttpResponseUtil;
    private fenceService: FenceService;
    private traderController: TraderController;
    private fenceBaseAssortGenerator: FenceBaseAssortGenerator;

    private originalLooseLootMultipliers : LootMultiplier
    private originalStaticLootMultipliers : LootMultiplier
	
    public preAkiLoad(container: DependencyContainer): void
    {
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
        this.logger = container.resolve<ILogger>("WinstonLogger");

        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`,
            [{
                url: "/LateToTheParty/GetConfig",
                action: () => 
                {
                    return JSON.stringify(modConfig);
                }
            }], "GetConfig"
        );

        // Get the logging directory for bepinex crash reports
        staticRouterModService.registerStaticRouter(`StaticGetLoggingPath${modName}`,
            [{
                url: "/LateToTheParty/GetLoggingPath",
                action: () => 
                {
                    return JSON.stringify({ path: __dirname + "/../log/" });
                }
            }], "GetLoggingPath"
        );

        if (!modConfig.enabled)
        {
            return;
        }

        // Game start
        // Needed to initialize bot conversion helper instance and loot ranking generator after any other mods have potentially changed config settings
        staticRouterModService.registerStaticRouter(`StaticAkiGameStart${modName}`,
            [{
                url: "/client/game/start",
                action: (url: string, info: any, sessionId: string, output: string) => 
                {
                    this.traderAssortGenerator.clearLastAssortData();

                    this.botConversionHelper = new BotConversionHelper(this.commonUtils, this.iBotConfig);
                    this.generateLootRankingData(sessionId);

                    if (modConfig.debug.enabled)
                    {
                        this.updateScavTimer(sessionId);
                    }

                    return output;
                }
            }], "aki"
        );

        // Update trader inventory
        dynamicRouterModService.registerDynamicRouter(`DynamicGetTraderAssort${modName}`,
            [{
                url: "/client/trading/api/getTraderAssort/",
                action: (url: string, info: any, sessionId: string, output: string) => 
                {
                    if (!modConfig.fence_assort_changes.enabled)
                    {
                        return output;
                    }

                    const traderID = url.replace("/client/trading/api/getTraderAssort/", "");
                    const assort = this.getUpdatedTraderAssort(traderID, sessionId);
                    return this.httpResponseUtil.getBody(assort);
                }
            }], "aki"
        );

        // Game end
        // Needed for disabling the recurring task that modifies PMC-conversion chances
        staticRouterModService.registerStaticRouter(`StaticAkiRaidEnd${modName}`,
            [{
                url: "/client/match/offline/end",
                action: (output: string) => 
                {
                    BotConversionHelper.stopRaidTimer();                    
                    return output;
                }
            }], "aki"
        );
        
        // Get lootRanking.json for loot ranking
        staticRouterModService.registerStaticRouter(`StaticGetLootRankingData${modName}`,
            [{
                url: "/LateToTheParty/GetLootRankingData",
                action: () => 
                {
                    return JSON.stringify(this.lootRankingGenerator.getLootRankingDataFromFile());
                }
            }], "GetLootRankingData"
        );

        // Get an array of all car extract names
        staticRouterModService.registerStaticRouter(`StaticGetCarExtractNames${modName}`,
            [{
                url: "/LateToTheParty/GetCarExtractNames",
                action: () => 
                {
                    return JSON.stringify(this.inRaidConfig.carExtracts);
                }
            }], "GetCarExtractNames"
        );

        // Adjust the static and loose loot multipliers
        dynamicRouterModService.registerDynamicRouter(`DynamicSetLootMultipliers${modName}`,
            [{
                url: "/LateToTheParty/SetLootMultiplier/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const factor = Number(urlParts[urlParts.length - 1]);

                    this.setLootMultipliers(factor);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "SetLootMultiplier"
        );

        // Sets the escape time for the map and the current time remaining
        dynamicRouterModService.registerDynamicRouter(`DynamicSetEscapeTime${modName}`,
            [{
                url: "/LateToTheParty/EscapeTime/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const escapeTime = Number(urlParts[urlParts.length - 2]);
                    const timeRemaining = Number(urlParts[urlParts.length - 1]);
                    
                    this.botConversionHelper.setEscapeTime(escapeTime, timeRemaining);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "SetEscapeTime"
        );
    }

    public postDBLoad(container: DependencyContainer): void
    {
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.vfs = container.resolve<VFS>("VFS");
        this.localeService = container.resolve<LocaleService>("LocaleService");
        this.botWeaponGenerator = container.resolve<BotWeaponGenerator>("BotWeaponGenerator");
        this.hashUtil = container.resolve<HashUtil>("HashUtil");
        this.jsonUtil = container.resolve<JsonUtil>("JsonUtil");
        this.timeutil = container.resolve<TimeUtil>("TimeUtil");
        this.randomUtil = container.resolve<RandomUtil>("RandomUtil");
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        this.traderController = container.resolve<TraderController>("TraderController");
        this.fenceService = container.resolve<FenceService>("FenceService");
        this.fenceBaseAssortGenerator = container.resolve<FenceBaseAssortGenerator>("FenceBaseAssortGenerator");

        this.locationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);
        this.inRaidConfig = this.configServer.getConfig(ConfigTypes.IN_RAID);
        this.iBotConfig = this.configServer.getConfig(ConfigTypes.BOT);
        this.iAirdropConfig = this.configServer.getConfig(ConfigTypes.AIRDROP);
        this.iTraderConfig = this.configServer.getConfig(ConfigTypes.TRADER);
        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);
        
        if (!modConfig.enabled)
        {
            this.commonUtils.logInfo("Mod disabled in config.json.");
            return;
        }

        // Adjust parameters to make debugging easier
        if (modConfig.debug.enabled)
        {
            this.commonUtils.logInfo("Applying debug options...");

            if (modConfig.debug.scav_cooldown_time < this.databaseTables.globals.config.SavagePlayCooldown)
            {
                this.databaseTables.globals.config.SavagePlayCooldown = modConfig.debug.scav_cooldown_time;
            }

            if (modConfig.debug.free_labs_access)
            {
                this.databaseTables.locations.laboratory.base.AccessKeys = [];
            }

            this.databaseTables.globals.config.RagFair.minUserLevel = modConfig.debug.min_level_for_flea;

            //this.iAirdropConfig.airdropChancePercent.bigmap = 100;
            //this.iAirdropConfig.airdropChancePercent.woods = 100;
            //this.iAirdropConfig.airdropChancePercent.lighthouse = 100;
            //this.iAirdropConfig.airdropChancePercent.shoreline = 100;
            //this.iAirdropConfig.airdropChancePercent.interchange = 100;
            //this.iAirdropConfig.airdropChancePercent.reserve = 100;
            //this.iAirdropConfig.airdropChancePercent.tarkovStreets = 100;
        }
    }

    public postAkiLoad(): void
    {
        if (!modConfig.enabled)
        {
            return;
        }

        // Store the original static and loose loot multipliers
        this.getLootMultipliers();

        // Initialize trader assort data
        if (modConfig.fence_assort_changes.enabled)
        {
            this.traderAssortGenerator = new TraderAssortGenerator(
                this.commonUtils,
                this.databaseTables,
                this.jsonUtil,
                this.fenceService,
                this.fenceBaseAssortGenerator,
                this.iTraderConfig,
                this.randomUtil,
                this.timeutil
            );
        }
    }

    private updateScavTimer(sessionId: string): void
    {
        const pmcData = this.profileHelper.getPmcProfile(sessionId);
        const scavData = this.profileHelper.getScavProfile(sessionId);
		
        if ((scavData.Info === null) || (scavData.Info === undefined))
        {
            this.commonUtils.logInfo("Scav profile hasn't been created yet.");
            return;
        }
		
        // In case somebody disables scav runs and later wants to enable them, we need to reset their Scav timer unless it's plausible
        const worstCooldownFactor = this.getWorstSavageCooldownModifier();
        if (scavData.Info.SavageLockTime - pmcData.Info.LastTimePlayedAsSavage > this.databaseTables.globals.config.SavagePlayCooldown * worstCooldownFactor * 1.1)
        {
            this.commonUtils.logInfo(`Resetting scav timer for sessionId=${sessionId}...`);
            scavData.Info.SavageLockTime = 0;
        }
    }
	
    // Return the highest Scav cooldown factor from Fence's rep levels
    private getWorstSavageCooldownModifier(): number
    {
        // Initialize the return value at something very low
        let worstCooldownFactor = 0.01;

        for (const level in this.databaseTables.globals.config.FenceSettings.Levels)
        {
            if (this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier > worstCooldownFactor)
                worstCooldownFactor = this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier;
        }
        return worstCooldownFactor;
    }

    private getLootMultipliers(): void
    {
        this.originalLooseLootMultipliers = 
        {
            bigmap: this.locationConfig.looseLootMultiplier.bigmap,
            develop: this.locationConfig.looseLootMultiplier.develop,
            factory4_day: this.locationConfig.looseLootMultiplier.factory4_day,
            factory4_night: this.locationConfig.looseLootMultiplier.factory4_night,
            hideout: this.locationConfig.looseLootMultiplier.hideout,
            interchange: this.locationConfig.looseLootMultiplier.interchange,
            laboratory: this.locationConfig.looseLootMultiplier.laboratory,
            lighthouse: this.locationConfig.looseLootMultiplier.lighthouse,
            privatearea: this.locationConfig.looseLootMultiplier.privatearea,
            rezervbase: this.locationConfig.looseLootMultiplier.rezervbase,
            shoreline: this.locationConfig.looseLootMultiplier.shoreline,
            suburbs: this.locationConfig.looseLootMultiplier.suburbs,
            tarkovstreets: this.locationConfig.looseLootMultiplier.tarkovstreets,
            terminal: this.locationConfig.looseLootMultiplier.terminal,
            town: this.locationConfig.looseLootMultiplier.town,
            woods: this.locationConfig.looseLootMultiplier.woods
        }

        this.originalStaticLootMultipliers = 
        {
            bigmap: this.locationConfig.staticLootMultiplier.bigmap,
            develop: this.locationConfig.staticLootMultiplier.develop,
            factory4_day: this.locationConfig.staticLootMultiplier.factory4_day,
            factory4_night: this.locationConfig.staticLootMultiplier.factory4_night,
            hideout: this.locationConfig.staticLootMultiplier.hideout,
            interchange: this.locationConfig.staticLootMultiplier.interchange,
            laboratory: this.locationConfig.staticLootMultiplier.laboratory,
            lighthouse: this.locationConfig.staticLootMultiplier.lighthouse,
            privatearea: this.locationConfig.staticLootMultiplier.privatearea,
            rezervbase: this.locationConfig.staticLootMultiplier.rezervbase,
            shoreline: this.locationConfig.staticLootMultiplier.shoreline,
            suburbs: this.locationConfig.staticLootMultiplier.suburbs,
            tarkovstreets: this.locationConfig.staticLootMultiplier.tarkovstreets,
            terminal: this.locationConfig.staticLootMultiplier.terminal,
            town: this.locationConfig.staticLootMultiplier.town,
            woods: this.locationConfig.staticLootMultiplier.woods
        }
    }

    private setLootMultipliers(factor: number): void
    {
        this.commonUtils.logInfo(`Adjusting loot multipliers by a factor of ${factor}...`);

        this.locationConfig.looseLootMultiplier.bigmap = this.originalLooseLootMultipliers.bigmap * factor;
        this.locationConfig.looseLootMultiplier.develop = this.originalLooseLootMultipliers.develop * factor;
        this.locationConfig.looseLootMultiplier.factory4_day = this.originalLooseLootMultipliers.factory4_day * factor;
        this.locationConfig.looseLootMultiplier.factory4_night = this.originalLooseLootMultipliers.factory4_night * factor;
        this.locationConfig.looseLootMultiplier.hideout = this.originalLooseLootMultipliers.hideout * factor;
        this.locationConfig.looseLootMultiplier.interchange = this.originalLooseLootMultipliers.interchange * factor;
        this.locationConfig.looseLootMultiplier.laboratory = this.originalLooseLootMultipliers.laboratory * factor;
        this.locationConfig.looseLootMultiplier.lighthouse = this.originalLooseLootMultipliers.lighthouse * factor;
        this.locationConfig.looseLootMultiplier.privatearea = this.originalLooseLootMultipliers.privatearea * factor;
        this.locationConfig.looseLootMultiplier.rezervbase = this.originalLooseLootMultipliers.rezervbase * factor;
        this.locationConfig.looseLootMultiplier.shoreline = this.originalLooseLootMultipliers.shoreline * factor;
        this.locationConfig.looseLootMultiplier.suburbs = this.originalLooseLootMultipliers.suburbs * factor;
        this.locationConfig.looseLootMultiplier.tarkovstreets = this.originalLooseLootMultipliers.tarkovstreets * factor;
        this.locationConfig.looseLootMultiplier.terminal = this.originalLooseLootMultipliers.terminal * factor;
        this.locationConfig.looseLootMultiplier.town = this.originalLooseLootMultipliers.town * factor;
        this.locationConfig.looseLootMultiplier.woods = this.originalLooseLootMultipliers.woods * factor;

        this.locationConfig.staticLootMultiplier.bigmap = this.originalStaticLootMultipliers.bigmap * factor;
        this.locationConfig.staticLootMultiplier.develop = this.originalStaticLootMultipliers.develop * factor;
        this.locationConfig.staticLootMultiplier.factory4_day = this.originalStaticLootMultipliers.factory4_day * factor;
        this.locationConfig.staticLootMultiplier.factory4_night = this.originalStaticLootMultipliers.factory4_night * factor;
        this.locationConfig.staticLootMultiplier.hideout = this.originalStaticLootMultipliers.hideout * factor;
        this.locationConfig.staticLootMultiplier.interchange = this.originalStaticLootMultipliers.interchange * factor;
        this.locationConfig.staticLootMultiplier.laboratory = this.originalStaticLootMultipliers.laboratory * factor;
        this.locationConfig.staticLootMultiplier.lighthouse = this.originalStaticLootMultipliers.lighthouse * factor;
        this.locationConfig.staticLootMultiplier.privatearea = this.originalStaticLootMultipliers.privatearea * factor;
        this.locationConfig.staticLootMultiplier.rezervbase = this.originalStaticLootMultipliers.rezervbase * factor;
        this.locationConfig.staticLootMultiplier.shoreline = this.originalStaticLootMultipliers.shoreline * factor;
        this.locationConfig.staticLootMultiplier.suburbs = this.originalStaticLootMultipliers.suburbs * factor;
        this.locationConfig.staticLootMultiplier.tarkovstreets = this.originalStaticLootMultipliers.tarkovstreets * factor;
        this.locationConfig.staticLootMultiplier.terminal = this.originalStaticLootMultipliers.terminal * factor;
        this.locationConfig.staticLootMultiplier.town = this.originalStaticLootMultipliers.town * factor;
        this.locationConfig.staticLootMultiplier.woods = this.originalStaticLootMultipliers.woods * factor;
    }

    private generateLootRankingData(sessionId: string): void
    {
        this.lootRankingGenerator = new LootRankingGenerator(this.commonUtils, this.databaseTables, this.vfs, this.botWeaponGenerator, this.hashUtil);
        this.lootRankingGenerator.generateLootRankingData(sessionId);
    }

    private getUpdatedTraderAssort(traderID: string, sessionId: string): ITraderAssort
    {
        // Refresh Fence's assorts
        if (traderID == Traders.FENCE)
        {
            this.traderAssortGenerator.updateFenceAssort();
        }

        // Update stock for trader
        const assort = this.traderController.getAssort(sessionId, traderID);
        this.traderAssortGenerator.updateTraderStock(traderID, assort, traderID == Traders.FENCE);

        // Check if Fence's assorts need to be regenerated
        if (traderID == Traders.FENCE)
        {
            const pmcProfile = this.profileHelper.getPmcProfile(sessionId);
            const maxLL = pmcProfile.TradersInfo[Traders.FENCE].loyaltyLevel;
            this.traderAssortGenerator.replenishFenceStockIfNeeded(assort, maxLL);
        }

        return assort;
    }
}
module.exports = {mod: new LateToTheParty()}