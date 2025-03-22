/* eslint-disable @typescript-eslint/naming-convention */
import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";
import { LootRankingGenerator } from "./LootRankingGenerator";

import type { DependencyContainer } from "tsyringe";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import type { DynamicRouterModService } from "@spt/services/mod/dynamicRouter/DynamicRouterModService";

import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { ConfigServer } from "@spt/servers/ConfigServer";
import type { ILocationConfig, ILootMultiplier } from "@spt/models/spt/config/ILocationConfig";
import type { IInRaidConfig } from "@spt/models/spt/config/IInRaidConfig";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import type { DatabaseServer } from "@spt/servers/DatabaseServer";
import type { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import type { FileSystemSync } from "@spt/utils/FileSystemSync";
import type { LocaleService } from "@spt/services/LocaleService";
import type { BotWeaponGenerator } from "@spt/generators/BotWeaponGenerator";
import type { HashUtil } from "@spt/utils/HashUtil";

const modName = "LateToTheParty";

class LateToTheParty implements IPreSptLoadMod, IPostDBLoadMod, IPostSptLoadMod
{
    private commonUtils: CommonUtils
    private lootRankingGenerator: LootRankingGenerator
    
    private logger: ILogger;
    private locationConfig: ILocationConfig;
    private inRaidConfig: IInRaidConfig;
    private configServer: ConfigServer;
    private databaseServer: DatabaseServer;
    private databaseTables: IDatabaseTables;
    private fileSystem: FileSystemSync;
    private localeService: LocaleService;
    private botWeaponGenerator: BotWeaponGenerator;
    private hashUtil: HashUtil;

    private originalLooseLootMultipliers : ILootMultiplier
    private originalStaticLootMultipliers : ILootMultiplier
	
    public preSptLoad(container: DependencyContainer): void
    {
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
        this.logger = container.resolve<ILogger>("WinstonLogger");

        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`,
            [{
                url: "/LateToTheParty/GetConfig",
                action: async () => 
                {
                    return JSON.stringify(modConfig);
                }
            }], "GetConfig"
        );

        if (!modConfig.enabled)
        {
            return;
        }

        // Game start
        // Needed to initialize loot ranking generator after any other mods have potentially changed config settings
        staticRouterModService.registerStaticRouter(`StaticAkiGameStart${modName}`,
            [{
                url: "/client/game/start",
                // biome-ignore lint/suspicious/noExplicitAny: <explanation>
                action: async (url: string, info: any, sessionId: string, output: string) => 
                {
                    this.generateLootRankingData(sessionId);

                    return output;
                }
            }], "aki"
        );
        
        // Get lootRanking.json for loot ranking
        staticRouterModService.registerStaticRouter(`StaticGetLootRankingData${modName}`,
            [{
                url: "/LateToTheParty/GetLootRankingData",
                action: async () => 
                {
                    return JSON.stringify(this.lootRankingGenerator.getLootRankingDataFromFile());
                }
            }], "GetLootRankingData"
        );

        // Get an array of all car extract names
        staticRouterModService.registerStaticRouter(`StaticGetCarExtractNames${modName}`,
            [{
                url: "/LateToTheParty/GetCarExtractNames",
                action: async () => 
                {
                    return JSON.stringify(this.inRaidConfig.carExtracts);
                }
            }], "GetCarExtractNames"
        );

        // Adjust the static and loose loot multipliers
        dynamicRouterModService.registerDynamicRouter(`DynamicSetLootMultipliers${modName}`,
            [{
                url: "/LateToTheParty/SetLootMultiplier/",
                action: async (url: string) => 
                {
                    const urlParts = url.split("/");
                    const factor = Number(urlParts[urlParts.length - 1]);

                    this.setLootMultipliers(factor);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "SetLootMultiplier"
        );
    }

    public postDBLoad(container: DependencyContainer): void
    {
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.fileSystem = container.resolve<FileSystemSync>("FileSystemSync");
        this.localeService = container.resolve<LocaleService>("LocaleService");
        this.botWeaponGenerator = container.resolve<BotWeaponGenerator>("BotWeaponGenerator");
        this.hashUtil = container.resolve<HashUtil>("HashUtil");

        this.locationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);
        this.inRaidConfig = this.configServer.getConfig(ConfigTypes.IN_RAID);

        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);
        
        if (!modConfig.enabled)
        {
            this.commonUtils.logInfo("Mod disabled in config.json.");
            return;
        }

        if (!this.doesFileIntegrityCheckPass())
        {
            modConfig.enabled = false;
            return;
        }

        this.adjustSPTScavRaidChanges();
    }

    public postSptLoad(): void
    {
        if (!modConfig.enabled)
        {
            return;
        }

        // Store the original static and loose loot multipliers
        this.getLootMultipliers();
    }

    private adjustSPTScavRaidChanges(): void
    {
        this.commonUtils.logInfo("Adjusting SPT Scav-raid changes...");

        for (const map in this.locationConfig.scavRaidTimeSettings.maps)
        {
            if (modConfig.scav_raid_adjustments.always_spawn_late)
            {
                this.locationConfig.scavRaidTimeSettings.maps[map].reducedChancePercent = 100;
            }

            if (modConfig.destroy_loot_during_raid.enabled)
            {
                this.locationConfig.scavRaidTimeSettings.maps[map].reduceLootByPercent = false;
            }
        }
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
            woods: this.locationConfig.looseLootMultiplier.woods,
            sandbox: this.locationConfig.looseLootMultiplier.sandbox
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
            woods: this.locationConfig.staticLootMultiplier.woods,
            sandbox: this.locationConfig.staticLootMultiplier.sandbox
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
        this.locationConfig.looseLootMultiplier.sandbox = this.originalLooseLootMultipliers.sandbox * factor;

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
        this.locationConfig.staticLootMultiplier.sandbox = this.originalStaticLootMultipliers.sandbox * factor;
    }

    private generateLootRankingData(sessionId: string): void
    {
        this.lootRankingGenerator = new LootRankingGenerator(this.commonUtils, this.databaseTables, this.fileSystem, this.botWeaponGenerator, this.hashUtil);
        this.lootRankingGenerator.generateLootRankingData(sessionId);
    }

    private doesFileIntegrityCheckPass(): boolean
    {
        const path = `${__dirname}/..`;

        if (this.fileSystem.exists(`${path}/log/`))
        {
            this.commonUtils.logWarning("Found obsolete log folder 'user\\mods\\DanW-LateToTheParty\\log'. Logs are now saved in 'BepInEx\\plugins\\DanW-LateToTheParty\\log'.");
        }

        if (this.fileSystem.exists(`${path}/../../../BepInEx/plugins/LateToTheParty.dll`))
        {
            this.commonUtils.logError("Please remove BepInEx/plugins/LateToTheParty.dll from the previous version of this mod and restart the server, or it will NOT work correctly.");
        
            return false;
        }

        return true;
    }
}
module.exports = {mod: new LateToTheParty()}