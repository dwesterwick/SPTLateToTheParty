import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
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

        const items: Record<string, LootRankingData> = {};
        for (const itemID in this.databaseTables.templates.items)
        {
            if (this.databaseTables.templates.items[itemID]._type == "Node")
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
            items: items
        };
        const rankingDataStr = JSON.stringify(rankingData);

        this.vfs.writeFile(lootFilePath, rankingDataStr);
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

        let value = (cost / maxDim) * modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot;
        value += weight * modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight;
        value += size * modConfig.destroy_loot_during_raid.loot_ranking.weighting.size;
        value += maxDim * modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim;

        const data: LootRankingData = {
            id: item._id,
            name: this.commonUtils.getItemName(item._id),
            value: value,
            costPerSlot: cost / size,
            weight: weight,
            size: size,
            maxDim: maxDim
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