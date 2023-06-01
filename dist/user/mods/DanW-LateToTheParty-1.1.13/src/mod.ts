/* eslint-disable @typescript-eslint/naming-convention */
import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";
import { BotConversionHelper } from "./BotConversionHelper";
import { LootRankingGenerator } from "./LootRankingGenerator";

import { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import type { IPostAkiLoadMod } from "@spt-aki/models/external/IPostAkiLoadMod";
import type {StaticRouterModService} from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import type {DynamicRouterModService} from "@spt-aki/services/mod/dynamicRouter/DynamicRouterModService";

import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { ConfigServer } from "@spt-aki/servers/ConfigServer";
import { ILocationConfig, LootMultiplier } from "@spt-aki/models/spt/config/ILocationConfig";
import { IInRaidConfig } from "@spt-aki/models/spt/config/IInRaidConfig";
import { IBotConfig } from "@spt-aki/models/spt/config/IBotConfig";
import { ConfigTypes } from "@spt-aki/models/enums/ConfigTypes";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";
import { LocaleService } from "@spt-aki/services/LocaleService";
import { BotWeaponGenerator } from "@spt-aki/generators/BotWeaponGenerator";
import { HashUtil } from "@spt-aki/utils/HashUtil";

const modName = "LateToTheParty";

class LateToTheParty implements IPreAkiLoadMod, IPostDBLoadMod, IPostAkiLoadMod
{
    private commonUtils: CommonUtils
    private botConversionHelper: BotConversionHelper
    private lootRankingGenerator: LootRankingGenerator
    
    private logger: ILogger;
    private locationConfig: ILocationConfig;
    private inRaidConfig: IInRaidConfig;
    private iBotConfig: IBotConfig;
    private configServer: ConfigServer;
    private databaseServer: DatabaseServer;
    private databaseTables: IDatabaseTables;
    private vfs: VFS;
    private localeService: LocaleService;
    private botWeaponGenerator: BotWeaponGenerator;
    private hashUtil: HashUtil;

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
                    this.botConversionHelper = new BotConversionHelper(this.commonUtils, this.iBotConfig);
                    this.generateLootRankingData(sessionId);

                    return output;
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

        this.locationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);
        this.inRaidConfig = this.configServer.getConfig(ConfigTypes.IN_RAID);
        this.iBotConfig = this.configServer.getConfig(ConfigTypes.BOT);
        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);

        if (!modConfig.enabled)
        {
            this.commonUtils.logInfo("Mod disabled in config.json.");
            return;
        }

        // Make the Scav cooldown timer very short for debugging
        if (modConfig.debug)
            this.databaseTables.globals.config.SavagePlayCooldown = 1;
    }

    public postAkiLoad(): void
    {
        if (!modConfig.enabled)
        {
            return;
        }

        // Store the original static and loose loot multipliers
        this.getLootMultipliers();
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
}
module.exports = {mod: new LateToTheParty()}