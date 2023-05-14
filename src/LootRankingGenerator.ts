import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { VFS } from "@spt-aki/utils/VFS";
import { BotWeaponGenerator } from "@spt-aki/generators/BotWeaponGenerator";
import { EquipmentSlots } from "@spt-aki/models/enums/EquipmentSlots";
import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import { Item } from "@spt-aki/models/eft/common/tables/IItem";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { GenerateWeaponResult } from "@spt-aki/models/spt/bots/GenerateWeaponResult";

const verboseLogging = true;
const lootFilePath = __dirname + "/../db/lootRanking.json";

// Overall file structure
export interface LootRankingContainer
{
    costPerSlot: number,
    weight: number,
    size: number,
    gridSize: number,
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
    size: number,
    gridSize: number,
    maxDim: number,
    armorClass: number,
    parentWeighting: number
}

export class LootRankingGenerator
{
    constructor(private commonUtils: CommonUtils, private databaseTables: IDatabaseTables, private vfs: VFS, private botWeaponGenerator: BotWeaponGenerator)
    { }

    public getLootRankingDataFromFile(): LootRankingContainer
    {
        const rankingDataStr = this.vfs.readFile(lootFilePath);
        return JSON.parse(rankingDataStr);
    }

    public generateLootRankingData(sessionId: string): void
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

        this.commonUtils.logInfo("Creating loot ranking data...", true);

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

            items[this.databaseTables.templates.items[itemID]._id] = this.generateLookRankingForItem(this.databaseTables.templates.items[itemID], sessionId);
        }

        // Generate the file contents
        const rankingData: LootRankingContainer = {
            costPerSlot: modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot,
            weight: modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight,
            size: modConfig.destroy_loot_during_raid.loot_ranking.weighting.size,
            gridSize: modConfig.destroy_loot_during_raid.loot_ranking.weighting.gridSize,
            maxDim: modConfig.destroy_loot_during_raid.loot_ranking.weighting.max_dim,
            armorClass: modConfig.destroy_loot_during_raid.loot_ranking.weighting.armor_class,
            parentWeighting: modConfig.destroy_loot_during_raid.loot_ranking.weighting.parents,
            items: items
        };
        const rankingDataStr = JSON.stringify(rankingData);

        this.vfs.writeFile(lootFilePath, rankingDataStr);
        this.commonUtils.logInfo("Creating loot ranking data...done.", true);
    }

    private generateLookRankingForItem(item: ITemplateItem, sessionId: string): LootRankingData
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
        let weight = item._props.Weight;
        let size = item._props.Width * item._props.Height;
        let maxDim = Math.max(item._props.Width, item._props.Height);

        // If the item is a weapon, find a suitable assembled version of it
        if (item._props.weapClass !== undefined)
        {
            // First try to find the most desirable weapon from the traders
            let bestWeaponMatch: Item[] = this.findBestWeaponMatchfromTraders(item);

            // If the weapon isn't offered by any traders, generate a random version of it
            if (bestWeaponMatch.length == 0)
            {
                //this.commonUtils.logInfo(`Could not find ${this.commonUtils.getItemName(item._id)} in trader assorts.`);
                bestWeaponMatch = this.generateRandomWeapon(item, sessionId);
            }

            // Ensure a weapon has been generated
            if (bestWeaponMatch.length == 0)
            {
                this.commonUtils.logError(`Could not generate a weapon for ${this.commonUtils.getItemName(item._id)}`);
            }
            else
            {
                const [weaponWidth, weaponHeight, weaponWeight]: number[] = this.getWeaponProperties(item, bestWeaponMatch);
                if (verboseLogging) this.commonUtils.logInfo(`Found weapon ${this.commonUtils.getItemName(item._id)}: Width=${weaponWidth},Height=${weaponHeight},Weight=${weaponWeight}`);

                weight = weaponWeight;
                size = weaponWidth * weaponHeight;
                maxDim = Math.max(weaponWidth, weaponHeight);
            }
        }

        // Check if the item has a grid in which other items can be placed (i.e. a backpack)
        let gridSize = 0;
        if (item._props.Grids !== undefined)
        {
            for (const grid in item._props.Grids)
            {
                gridSize += item._props.Grids[grid]._props.cellsH * item._props.Grids[grid]._props.cellsV;
            }
        }

        // Get the armor class for the item if applicable
        let armorClass = 0;
        if (item._props.armorClass !== undefined)
        {
            armorClass = Number(item._props.armorClass);
        }

        // Calculate the cost per slot
        // If the item can be equipped (backpacks, weapons, etc.), use the inventory slot size (1) instead of the item's total size
        let costPerSlot = cost;
        if (!this.canEquipItem(item))
        {
            costPerSlot /= size;
        }

        // Generate the loot-ranking value based on the item properties and weighting in config.json
        let value = costPerSlot * modConfig.destroy_loot_during_raid.loot_ranking.weighting.cost_per_slot;
        value += weight * modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight;
        value += size * modConfig.destroy_loot_during_raid.loot_ranking.weighting.size;
        value += gridSize * modConfig.destroy_loot_during_raid.loot_ranking.weighting.gridSize;
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
            costPerSlot: costPerSlot,
            weight: weight,
            size: size,
            gridSize: gridSize,
            maxDim: maxDim,
            armorClass: armorClass,
            parentWeighting: parentWeighting
        }

        return data;
    }

    private generateRandomWeapon(item: ITemplateItem, sessionId: string): Item[]
    {
        const iterations = 5;
        const botType = "assault";
        const possibleSlots: string[] = [
            EquipmentSlots.FIRST_PRIMARY_WEAPON,
            EquipmentSlots.SECOND_PRIMARY_WEAPON,
            EquipmentSlots.HOLSTER
        ];
        
        let weapon: Item[] = [];
        let randomWeapon: GenerateWeaponResult;
        for (const possibleSlot in possibleSlots)
        {
            for (let iteration = 0; iteration < iterations; iteration++)
            {
                randomWeapon = this.botWeaponGenerator.generateWeaponByTpl(
                    sessionId,
                    item._id,
                    possibleSlots[possibleSlot],
                    this.databaseTables.bots.types[botType].inventory,
                    item._parent,
                    this.databaseTables.bots.types[botType].chances.mods,
                    botType,
                    false,
                    1
                );
                
                // If the random weapon is invalid, don't bother running more iterations; the weapon slot is wrong
                if ((randomWeapon.weapon === undefined) && (randomWeapon.weapon.length == 0))
                {
                    break;
                }

                // Store the initial weapon selection
                if (weapon.length == 0)
                {
                    weapon = randomWeapon.weapon;
                    continue;
                }

                // Determine if the weapon is better than the previous one found
                if (this.weaponBaseValue(item, randomWeapon.weapon) > this.weaponBaseValue(item, weapon))
                {
                    weapon = randomWeapon.weapon;
                }
            }

            // Check if a valid weapon was generated
            if (weapon.length > 0)
            {
                break;
            }
        }

        return weapon;
    }

    private findBestWeaponMatchfromTraders(item: ITemplateItem): Item[]
    {
        let weapon: Item[] = [];

        // Search all traders to see if they sell the weapon
        for (const traderID in this.databaseTables.traders)
        {
            const assort = this.databaseTables.traders[traderID].assort;

            // Ignore traders who don't sell anything (i.e. Lightkeeper)
            if ((assort === null) || (assort === undefined))
                continue;

            //this.commonUtils.logInfo(`Searching ${this.databaseTables.traders[traderID].base.nickname}...`);
            for (const assortID in assort.items)
            {
                const weaponCandidate: Item[] = [];
                if (assort.items[assortID]._tpl == item._id)
                {
                    // Get all parts attached to the weapon
                    const matchingSlots = this.findChildSlotIndexesInTraderAssort(assort, assortID);
                    for (const matchingSlot in matchingSlots)
                    {
                        weaponCandidate.push(assort.items[matchingSlots[matchingSlot]]);
                    }

                    // Store the initial weapon selection
                    if (weapon.length == 0)
                    {
                        weapon = weaponCandidate;
                        continue;
                    }

                    // Determine if the weapon is better than the previous one found
                    if (this.weaponBaseValue(item, weaponCandidate) > this.weaponBaseValue(item, weapon))
                    {
                        weapon = weaponCandidate;
                    }
                }
            }
        }

        return weapon;
    }

    private weaponBaseValue(baseWeaponItem: ITemplateItem, weaponParts: Item[]): number
    {
        const [width, height, weight]: number[] = this.getWeaponProperties(baseWeaponItem, weaponParts);

        let value = weight * modConfig.destroy_loot_during_raid.loot_ranking.weighting.weight;
        value += width * height * modConfig.destroy_loot_during_raid.loot_ranking.weighting.size;

        return value;
    }

    /**
     * Gets relevant weapon properties
     * @param baseWeaponItem The base weapon template (namely the receiver)
     * @param weaponParts All parts attached to the weapon (which may include the base weapon item itself)
     * @returns [length, width, weight] of the assembled weapon
     */
    private getWeaponProperties(baseWeaponItem: ITemplateItem, weaponParts: Item[]): [number, number, number]
    {
        let width = baseWeaponItem._props.Width;
        let height = baseWeaponItem._props.Height;
        let weight = baseWeaponItem._props.Weight;
        if (verboseLogging) this.commonUtils.logInfo(`Generating random weapon ${this.commonUtils.getItemName(baseWeaponItem._id)}... => Width=${width},Height=${height},Weight=${weight}`);
        for (const weaponPart in weaponParts)
        {
            const templateID = weaponParts[weaponPart]._tpl;
            const slotID = weaponParts[weaponPart].slotId;

            if (baseWeaponItem._id == templateID)
            {
                continue;
            }

            weight += this.databaseTables.templates.items[templateID]._props.Weight ?? 0;

            // Fold the weapon if possible
            if (baseWeaponItem._props.FoldedSlot !== undefined)
            {
                if (baseWeaponItem._props.FoldedSlot == slotID)
                {
                    if (verboseLogging) this.commonUtils.logInfo(`Generating random weapon ${this.commonUtils.getItemName(baseWeaponItem._id)}...folds with ${this.commonUtils.getItemName(templateID)} => Width=${width},Height=${height},Weight=${weight}`);
                    continue;
                }
            }

            width += this.databaseTables.templates.items[templateID]._props.ExtraSizeLeft ?? 0;
            width += this.databaseTables.templates.items[templateID]._props.ExtraSizeRight ?? 0;
            height += this.databaseTables.templates.items[templateID]._props.ExtraSizeUp ?? 0;
            height += this.databaseTables.templates.items[templateID]._props.ExtraSizeDown ?? 0;

            if (verboseLogging) this.commonUtils.logInfo(`Generating random weapon ${this.commonUtils.getItemName(baseWeaponItem._id)}...found ${this.commonUtils.getItemName(templateID)} => Width=${width},Height=${height},Weight=${weight}`);
        }

        if (verboseLogging) this.commonUtils.logInfo(`Generated random weapon ${this.commonUtils.getItemName(baseWeaponItem._id)}: Width=${width},Height=${height},Weight=${weight}`);
        return [width, height, weight];
    }

    private findChildSlotIndexesInTraderAssort(assort: ITraderAssort, parentIndex: number | string): string[]
    {
        let matchingSlots: string[] = [];

        const parentID = assort.items[parentIndex]._id;
        for (const assortID in assort.items)
        {
            if (assort.items[assortID].parentId == parentID)
            {
                matchingSlots.push(assortID);
                matchingSlots = matchingSlots.concat(this.findChildSlotIndexesInTraderAssort(assort, assortID));
            }
        }

        return matchingSlots;
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
            rankingData.size != modConfig.destroy_loot_during_raid.loot_ranking.weighting.size ||
            rankingData.gridSize != modConfig.destroy_loot_during_raid.loot_ranking.weighting.gridSize ||
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

    private canEquipItem(item: ITemplateItem): boolean
    {
        const defaultInventory: ITemplateItem = this.databaseTables.templates.items[modConfig.destroy_loot_during_raid.loot_ranking.weighting.default_inventory_id];

        if (defaultInventory === undefined)
        {
            return false;
        }

        for (const slot in defaultInventory._props.Slots)
        {
            const filters = defaultInventory._props.Slots[slot]._props.filters[0].Filter;
            for (const filter in filters)
            {
                if (CommonUtils.hasParent(item, filters[filter], this.databaseTables))
                {
                    //this.commonUtils.logInfo(`${this.commonUtils.getItemName(item._id)} can be equipped in ${defaultInventory._props.Slots[slot]._name}`);
                    return true;
                }
            }
        }

        return false;
    }
}