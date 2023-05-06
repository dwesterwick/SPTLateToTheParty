import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";

const lootFilePath = __dirname + "/../db/lootRanking.json";

export interface LootRankingData
{
    generated: boolean;
}

export class LootRankingGenerator
{
    constructor(private commonUtils: CommonUtils, private databaseTables: IDatabaseTables, private vfs: VFS)
    { }

    public generateLootRankingData(): void
    {
        if (this.validLootRankingDataExists())
        {
            this.commonUtils.logInfo("Using existing loot ranking data.");
            return;
        }

        this.commonUtils.logInfo("Creating loot ranking data...");

        const rankingData: LootRankingData = {
            generated: true
        };
        const rankingDataStr = JSON.stringify(rankingData);

        this.vfs.writeFile(lootFilePath, rankingDataStr);
    }

    private validLootRankingDataExists(): boolean
    {
        if (!this.vfs.exists(lootFilePath))
        {
            this.commonUtils.logInfo("Loot ranking data not found.");
            return false;
        }

        const rankingDataStr = this.vfs.readFile(lootFilePath);
        const rankingData: LootRankingData = JSON.parse(rankingDataStr);

        if (!rankingData.generated)
        {
            this.commonUtils.logInfo("Loot ranking data not valid.");
            return false;
        }

        this.commonUtils.logInfo(`${lootFilePath}: ${rankingDataStr}`);

        return true;
    }
}