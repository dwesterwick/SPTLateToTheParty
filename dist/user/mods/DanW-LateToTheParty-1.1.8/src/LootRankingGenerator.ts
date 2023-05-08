import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";

const lootFilePath = __dirname + "/../db/lootRanking.json";

export interface LootRankingContainer
{
    costPerSlot: number,
    weight: number,
    size: number,
    maxDim: number,
    parentWeighting: Record<string, LootRankingForParent>,
    items: Record<string, LootRankingData>
}

export interface LootRankingData
{
    id: string,
    name: string,
    value: number,
    costPerSlot: number,
    weight: number,
    size: number,
    maxDim: number,
    parentWeighting: number
}

export interface LootRankingForParent
{
    name: string,
    weighting: number
}

export class LootRankingGenerator
{
    constructor(private commonUtils: CommonUtils, private databaseTables: IDatabaseTables, private vfs: VFS)
    { }

    public getLootRankingDataFromFile(): LootRankingContainer
    {
        const rankingDataStr = this.vfs.readFile(lootFilePath);
        return JSON.parse(rankingDataStr);
    }

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

        const items: Record<string, LootRankingData> = {};
        for (const itemID in this.databaseTables.templates.items)
        {
            if (this.databaseTables.templates.items[itemID]._type == "Node")
            {
                continue;
            }

            if (this.databaseTables.templates.items[itemID]._props.QuestItem)
            {
                continue;
            }

            items[this.databaseTables.templates.items[itemID]._id] = this.generateLookRankingForItem(this.databaseTables.templates.items[itemID]);
        }

        const rankingData: LootRankingContainer = {
            costPerSlot: modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot,
            weight: modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight,
            size: modConfig.destroy_loot_during_raid.loot_ranking.weighting.size,
            maxDim: modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim,
            parentWeighting: modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents,
            items: items
        };
        const rankingDataStr = JSON.stringify(rankingData);

        this.vfs.writeFile(lootFilePath, rankingDataStr);
        this.commonUtils.logInfo("Creating loot ranking data...done.");
    }

    private generateLookRankingForItem(item: ITemplateItem): LootRankingData
    {
        const matchingHandbookItems = this.databaseTables.templates.handbook.Items.filter((item) => item.Id == item.Id);
        let handbookPrice = 0;
        if (matchingHandbookItems.length == 1)
        {
            handbookPrice = matchingHandbookItems[0].Price;
        }

        let price = 0;
        if (item._id in this.databaseTables.templates.prices)
        {
            price = this.databaseTables.templates.prices[item._id];
        }
        
        const cost = Math.max(handbookPrice, price);
        const weight = item._props.Weight;
        const size = item._props.Width * item._props.Height;
        const maxDim = Math.max(item._props.Width, item._props.Height);

        let value = (cost / size) * modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot;
        value += weight * modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight;
        value += size * modConfig.destroy_loot_during_raid.loot_ranking.weighting.size;
        value += maxDim * modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim;

        let parentWeighting = 0;
        for (const parentID in modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents)
        {
            if (CommonUtils.hasParent(item, parentID, this.databaseTables))
            {
                parentWeighting += modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents[parentID].weighting;
            }
        }
        value += parentWeighting;

        const data: LootRankingData = {
            id: item._id,
            name: this.commonUtils.getItemName(item._id),
            value: value,
            costPerSlot: cost / size,
            weight: weight,
            size: size,
            maxDim: maxDim,
            parentWeighting: parentWeighting
        }

        return data;
    }

    private validLootRankingDataExists(): boolean
    {
        if (!this.vfs.exists(lootFilePath))
        {
            this.commonUtils.logInfo("Loot ranking data not found.");
            return false;
        }

        if (modConfig.destroy_loot_during_raid.loot_ranking.alwaysRegenerate)
        {
            this.commonUtils.logInfo("Loot ranking data forced to regenerate.");
            this.vfs.removeFile(lootFilePath);
            return false;
        }

        const rankingData: LootRankingContainer = this.getLootRankingDataFromFile();

        let parentParametersMatch = true;
        for (const parentID in modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents)
        {
            if (!(parentID in rankingData.parentWeighting))
            {
                parentParametersMatch = false;
                break;
            }

            if (rankingData.parentWeighting[parentID].weighting != modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents[parentID].weighting)
            {
                parentParametersMatch = false;
                break;
            }
        }

        if (
            rankingData.costPerSlot != modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot ||
            rankingData.maxDim != modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim ||
            rankingData.size != modConfig.destroy_loot_during_raid.loot_ranking.weighting.size ||
            rankingData.weight != modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight ||
            !parentParametersMatch
        )
        {
            this.commonUtils.logInfo("Loot ranking parameters have changed; deleting cached data.");
            this.vfs.removeFile(lootFilePath);
            return false;
        }

        return true;
    }
}