import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";

const lootFilePath = __dirname + "/../db/lootRanking.json";

export interface LootRankingData
{
    id: string,
    name: string,
    value: number,
    costPerSlot: number,
    weight: number,
    size: number,
    maxDim: number
}

export interface LootRankingContainer
{
    costPerSlot: number,
    weight: number,
    size: number,
    maxDim: number,
    items: Record<string, LootRankingData>
}

export class LootRankingGenerator
{
    constructor(private commonUtils: CommonUtils, private databaseTables: IDatabaseTables, private vfs: VFS)
    { }

    public generateLootRankingData(): void
    {
        if (!modConfig.destroy_loot_during_raid.loot_ranking)
        {
            this.commonUtils.logInfo("Loot ranking is disabled in config.json.");
            return;
        }

        if (this.validLootRankingDataExists())
        {
            this.commonUtils.logInfo("Using existing loot ranking data.");
            return;
        }

        this.commonUtils.logInfo("Creating loot ranking data...");

        const rankingData: LootRankingContainer = {
            costPerSlot: modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot,
            weight: modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight,
            size: modConfig.destroy_loot_during_raid.loot_ranking.weighting.size,
            maxDim: modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim,
            items: undefined
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
        const rankingData: LootRankingContainer = JSON.parse(rankingDataStr);

        if (
            rankingData.costPerSlot != modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot ||
            rankingData.maxDim != modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim ||
            rankingData.size != modConfig.destroy_loot_during_raid.loot_ranking.weighting.size ||
            rankingData.weight != modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight
        )
        {
            this.commonUtils.logInfo("Loot ranking data not valid.");
            this.vfs.removeFile(lootFilePath);
            return false;
        }

        this.commonUtils.logInfo(`${lootFilePath}: ${rankingDataStr}`);

        return true;
    }
}