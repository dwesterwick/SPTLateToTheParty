import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";

const lootFilePath = __dirname + "/../db/lootRanking.json";

// Overall file structure
export interface LootRankingContainer
{
    costPerSlot: number,
    weight: number,
    netSize: number,
    maxDim: number,
    armorClass: number,
    parentWeighting: Record<string, LootRankingForParent>,
    items: Record<string, LootRankingData>
}

// Store the parameters for parent weighting
export interface LootRankingForParent
{
    name: string,
    weighting: number
}

// Object for each item
export interface LootRankingData
{
    id: string,
    name: string,
    value: number,
    costPerSlot: number,
    weight: number,
    netSize: number,
    maxDim: number,
    armorClass: number,
    parentWeighting: number
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

        // Create ranking data for each item found in the server database
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

        // Generate the file contents
        const rankingData: LootRankingContainer = {
            costPerSlot: modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot,
            weight: modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight,
            netSize: modConfig.destroy_loot_during_raid.loot_ranking.weighting.net_size,
            maxDim: modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim,
            armorClass: modConfig.destroy_loot_during_raid.loot_ranking.weighting.armor_class,
            parentWeighting: modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents,
            items: items
        };
        const rankingDataStr = JSON.stringify(rankingData);

        this.vfs.writeFile(lootFilePath, rankingDataStr);
        this.commonUtils.logInfo("Creating loot ranking data...done.");
    }

    private generateLookRankingForItem(item: ITemplateItem): LootRankingData
    {
        // Get the handbook.json price, if any exists
        const matchingHandbookItems = this.databaseTables.templates.handbook.Items.filter((item) => item.Id == item.Id);
        let handbookPrice = 0;
        if (matchingHandbookItems.length == 1)
        {
            handbookPrice = matchingHandbookItems[0].Price;
        }

        // Get the prices.json price, if any exists
        let price = 0;
        if (item._id in this.databaseTables.templates.prices)
        {
            price = this.databaseTables.templates.prices[item._id];
        }
        
        // Get required item properties from the server database
        const cost = Math.max(handbookPrice, price);
        const weight = item._props.Weight;
        const size = item._props.Width * item._props.Height;
        const maxDim = Math.max(item._props.Width, item._props.Height);

        // Check if the item has a grid in which other items can be placed (i.e. a backpack)
        let gridSize = 0;
        if (item._props.Grids !== undefined)
        {
            for (const grid in item._props.Grids)
            {
                gridSize += item._props.Grids[grid]._props.cellsH * item._props.Grids[grid]._props.cellsV;
            }
        }
        const netSize =  gridSize - size;

        let armorClass = 0;
        if (item._props.armorClass !== undefined)
        {
            armorClass = Number(item._props.armorClass);
        }

        // Generate the loot-ranking value based on the item properties and weighting in config.json
        let value = (cost / size) * modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot;
        value += weight * modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight;
        value += netSize * modConfig.destroy_loot_during_raid.loot_ranking.weighting.net_size;
        value += maxDim * modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim;
        value += armorClass * modConfig.destroy_loot_during_raid.loot_ranking.weighting.armor_class;

        // Determine how much additional weighting to apply if the item is a parent of any defined in config.json
        let parentWeighting = 0;
        for (const parentID in modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents)
        {
            if (CommonUtils.hasParent(item, parentID, this.databaseTables))
            {
                parentWeighting += modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents[parentID].weighting;
            }
        }
        value += parentWeighting;

        // Create the object to store in lootRanking.json 
        const data: LootRankingData = {
            id: item._id,
            name: this.commonUtils.getItemName(item._id),
            value: value,
            costPerSlot: cost / size,
            weight: weight,
            netSize: netSize,
            maxDim: maxDim,
            armorClass: armorClass,
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

        // Get the current file data
        const rankingData: LootRankingContainer = this.getLootRankingDataFromFile();

        // Check if the parent weighting in config.json matches the file data
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

        // Check if the general weighting parameters in config.json match the file data
        if (
            rankingData.costPerSlot != modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot ||
            rankingData.maxDim != modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim ||
            rankingData.netSize != modConfig.destroy_loot_during_raid.loot_ranking.weighting.net_size ||
            rankingData.weight != modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight ||
            rankingData.armorClass != modConfig.destroy_loot_during_raid.loot_ranking.weighting.armor_class ||
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