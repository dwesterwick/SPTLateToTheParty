import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { FenceService } from "@spt-aki/services/FenceService";
import { FenceBaseAssortGenerator } from "@spt-aki/generators/FenceBaseAssortGenerator";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { RandomUtil } from "@spt-aki/utils/RandomUtil";
import { TimeUtil } from "@spt-aki/utils/TimeUtil";
import { ITraderConfig } from "@spt-aki/models/spt/config/ITraderConfig";
import { Traders } from "@spt-aki/models/enums/Traders";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";

export class TraderAssortGenerator
{
    private lastAssortUpdate: Record<string, number> = {};
    private lastAssort: Record<string, ITraderAssort> = {};
    private originalFenceBaseAssortData: ITraderAssort;
    private fenceAssortWarehouse: ITraderAssort;

    constructor
    (
        private commonUtils: CommonUtils,
        private databaseTables: IDatabaseTables,
        private jsonUtil: JsonUtil,
        private fenceService: FenceService,
        private fenceBaseAssortGenerator: FenceBaseAssortGenerator,
        private iTraderConfig: ITraderConfig,
        private randomUtil: RandomUtil,
        private timeUtil: TimeUtil
    )
    {
        this.modifyFenceConfig();
        this.originalFenceBaseAssortData = this.jsonUtil.clone(this.databaseTables.traders[Traders.FENCE].assort);
    }

    public clearLastAssortData(): void
    {
        this.lastAssort = {};
        this.lastAssortUpdate = {};
    }

    public updateTraderStock(traderID: string, assort: ITraderAssort, deleteDepletedItems: boolean): ITraderAssort
    {
        const now = this.timeUtil.getTimestamp();

        // Initialize the timestamp for when the last update occurred
        if (this.lastAssortUpdate[traderID] === undefined)
        {
            this.lastAssortUpdate[traderID] = -99;
        }
        
        for (let i = 0; i < assort.items.length; i++)
        {
            // Ensure the stock can actually be reduced
            if ((assort.items[i].upd === undefined) || (assort.items[i].upd.StackObjectsCount === undefined) || (assort.items[i].upd.UnlimitedCount))
            {
                continue;
            }

            // Find the corresponding item template
            const itemTpl = this.databaseTables.templates.items[assort.items[i]._tpl];
            if (itemTpl === undefined)
            {
                this.commonUtils.logWarning(`Could not find template for ID ${assort.items[i]._tpl}`);
                continue;
            }

            // Determine the rate at which the item stock can be reduced
            let maxBuyRate = modConfig.fence_assort_changes.max_item_buy_rate * ((traderID == Traders.FENCE) ? 0.01 : 1);
            if (itemTpl._parent == modConfig.fence_assort_changes.ammo_parent_id)
            {
                maxBuyRate = modConfig.fence_assort_changes.max_ammo_buy_rate;

                // Set max ammo stack size
                if (traderID == Traders.FENCE)
                {
                    assort.items[i].upd.StackObjectsCount = this.randomUtil.randInt(0, modConfig.fence_assort_changes.max_ammo_stack);
                }

                // Remove duplicate ammo stacks
                for (let j = i + 1; j < assort.items.length; j++)
                {
                    if (assort.items[j]._tpl == assort.items[i]._tpl)
                    {
                        //this.commonUtils.logInfo(`Removing duplicate of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from assort...`);
                        this.removeIndexFromTraderAssort(assort, j);
                    }
                }
            }
            
            // Update the stack size
            if ((this.lastAssort[traderID] !== undefined) && (this.lastAssort[traderID].nextResupply == assort.nextResupply))
            {
                const lastAssortItem = this.lastAssort[traderID].items.find((item) => (item._id == assort.items[i]._id));
                if (lastAssortItem !== undefined)
                {
                    const stackSizeReduction = this.randomUtil.randInt(0, maxBuyRate * (now - this.lastAssortUpdate[traderID]));
                    if (stackSizeReduction > 0)
                    {
                        const newStackSize = lastAssortItem.upd.StackObjectsCount - stackSizeReduction;

                        if (newStackSize <= 0)
                        {
                            this.commonUtils.logInfo(`Reducing stock of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from ${lastAssortItem.upd.StackObjectsCount} to ${newStackSize}...`);
                        }

                        assort.items[i].upd.StackObjectsCount = newStackSize;
                    }
                }
                else
                {
                    // If the item wasn't in the previous assort, the stock was depleted
                    //this.commonUtils.logInfo(`Stock of ${this.commonUtils.getItemName(assort.items[i]._tpl)} is depleted.`);
                    assort.items[i].upd.StackObjectsCount = 0;
                }
            }

            // Check if the stock has been depleted
            if (assort.items[i].upd.StackObjectsCount <= 0)
            {
                // Remove ammo that is sold out
                if (deleteDepletedItems)
                {
                    this.removeIndexFromTraderAssort(assort, i);
                    i--;
                }
                else
                {
                    assort.items[i].upd.StackObjectsCount = 0;
                }
            }
        }

        // Update the resupply time and stock
        this.lastAssort[traderID] = this.jsonUtil.clone(assort);
        this.lastAssortUpdate[traderID] = now;

        return assort;
    } 

    public updateFenceAssort(pmcProfile: IPmcData): void
    {
        this.updateFenceAssortIDs();
        
        // Ensure the new assorts are generated at least once
        if ((this.lastAssort[Traders.FENCE] === undefined) || modConfig.fence_assort_changes.always_regenerate)
        {
            this.fenceService.generateFenceAssorts();

            this.fenceAssortWarehouse = {
                // eslint-disable-next-line @typescript-eslint/naming-convention
                barter_scheme: {},
                items: [],
                // eslint-disable-next-line @typescript-eslint/naming-convention
                loyal_level_items: {},
                nextResupply: 0
            };

            this.lastAssort[Traders.FENCE] = this.fenceService.getFenceAssorts(pmcProfile);
            this.moveItemsToWarehouse(this.lastAssort[Traders.FENCE], 1, modConfig.fence_assort_changes.assort_size);
            this.moveItemsToWarehouse(this.lastAssort[Traders.FENCE], 2, modConfig.fence_assort_changes.assort_size_discount);
            this.lastAssortUpdate[Traders.FENCE] = this.timeUtil.getTimestamp();
        }
    }

    public updateFenceAssortIDs(): void
    {
        const assort = this.jsonUtil.clone(this.originalFenceBaseAssortData);
        for (const itemID in this.originalFenceBaseAssortData.loyal_level_items)
        {
            const itemPrice = this.commonUtils.getMaxItemPrice(itemID);
            const permittedChance = CommonUtils.interpolateForFirstCol(modConfig.fence_item_value_permitted_chance, itemPrice);
            const randNum = this.randomUtil.getFloat(0, 100);

            // Determine if the item should be allowed in Fence's assorts
            if ((itemPrice >= modConfig.fence_assort_changes.min_allowed_item_value) && (permittedChance <= randNum))
            {
                // Ensure the index is valid
                const itemIndex = assort.items.findIndex((i) => i._id == itemID);
                if (itemIndex < 0)
                {
                    this.commonUtils.logError(`Invalid item: ${itemID}`);
                    continue;
                }

                this.removeIndexFromTraderAssort(assort, itemIndex);
            }
        }

        this.databaseTables.traders[Traders.FENCE].assort = assort;

        const originalAssortCount = Object.keys(this.originalFenceBaseAssortData.loyal_level_items).length;
        const newAssortCount = Object.keys(this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items).length;
        this.commonUtils.logInfo(`Updated Fence assort data: ${newAssortCount}/${originalAssortCount} items are available for sale.`);
    }

    private removeIDfromTraderAssort(assort: ITraderAssort, id: string): void
    {
        const index = assort.items.findIndex((i) => i._id == id);
        if (index > -1)
        {
            this.removeIndexFromTraderAssort(assort, index);
        }
        else
        {
            this.commonUtils.logError(`Could not find index ${id} in trader assort items`);
        }
    }

    private removeIndexFromTraderAssort(assort: ITraderAssort, index: number): void
    {
        const itemID = assort.items[index]._id;

        delete assort.loyal_level_items[itemID];
        delete assort.barter_scheme[itemID];
        assort.items.splice(index, 1);
    }

    private modifyFenceConfig(): void
    {
        // Adjust assort size and variety
        const assortGenFactor = 3;
        this.iTraderConfig.fence.assortSize = modConfig.fence_assort_changes.assort_size * assortGenFactor;
        this.iTraderConfig.fence.discountOptions.assortSize = modConfig.fence_assort_changes.assort_size_discount * assortGenFactor;
        
        for (const itemID in modConfig.fence_assort_changes.itemTypeLimits_Override)
        {
            this.iTraderConfig.fence.itemTypeLimits[itemID] = modConfig.fence_assort_changes.itemTypeLimits_Override[itemID];
        }

        // Add or remove ID's from the blacklist
        this.iTraderConfig.fence.blacklist = this.iTraderConfig.fence.blacklist.concat(modConfig.fence_assort_changes.blacklist_append);
        const removeID = modConfig.fence_assort_changes.blacklist_remove;
        for (const i in removeID)
        {
            if (this.iTraderConfig.fence.blacklist.includes(removeID[i]))
            {
                this.iTraderConfig.fence.blacklist.splice(this.iTraderConfig.fence.blacklist.indexOf(removeID[i]), 1);
            }
        }

        // Exclude high-tier ammo from Fence's assorts
        const allItems = this.databaseTables.templates.items;
        for (const itemID in allItems)
        {
            if (allItems[itemID]._parent != modConfig.fence_assort_changes.ammo_parent_id)
            {
                continue;
            }

            if ((allItems[itemID]._props.PenetrationPower === undefined) || (allItems[itemID]._props.Damage === undefined))
            {
                continue;
            }

            if (allItems[itemID]._props.PenetrationPower > modConfig.fence_assort_changes.blacklist_ammo_penetration_limit)
            {
                this.iTraderConfig.fence.blacklist.push(itemID);
                continue;
            }

            if (allItems[itemID]._props.Damage > modConfig.fence_assort_changes.blacklist_ammo_damage_limit)
            {
                this.iTraderConfig.fence.blacklist.push(itemID);
                continue;
            }
        }

        // Update Fence's base assorts
        this.commonUtils.logInfo(`Original Fence assort data: ${this.databaseTables.traders[Traders.FENCE].assort.items.length} items are available for sale.`);
        this.databaseTables.traders[Traders.FENCE].assort.barter_scheme = {};
        this.databaseTables.traders[Traders.FENCE].assort.items = [];
        this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items = {};
        this.databaseTables.traders[Traders.FENCE].assort.nextResupply = this.fenceService.getNextFenceUpdateTimestamp();
        this.fenceBaseAssortGenerator.generateFenceBaseAssorts();
        this.commonUtils.logInfo(`Updated Fence assort data: ${this.databaseTables.traders[Traders.FENCE].assort.items.length} items are available for sale.`);
    }

    private static getTraderAssortIDsforLL(assort: ITraderAssort, ll: number): string[]
    {
        const ids: string[] = [];

        for (const id in assort.loyal_level_items)
        {
            if (assort.loyal_level_items[id] == ll)
            {
                ids.push(id);
            }
        }

        return ids;
    }

    private moveItemsToWarehouse(assort: ITraderAssort, ll: number, maxCount: number): void
    {
        const ids = TraderAssortGenerator.getTraderAssortIDsforLL(assort, ll);
        let itemsMoved = 0;
        while (ids.length > maxCount)
        {
            const randIDindex = this.randomUtil.getInt(0, ids.length - 1);
            const item = assort.items.find((i) => i._id == ids[randIDindex]);

            this.fenceAssortWarehouse.loyal_level_items[ids[randIDindex]] = assort.loyal_level_items[ids[randIDindex]];
            this.fenceAssortWarehouse.barter_scheme[ids[randIDindex]] = assort.barter_scheme[ids[randIDindex]];
            this.fenceAssortWarehouse.items.push(item);

            this.removeIDfromTraderAssort(assort, ids[randIDindex]);

            ids.splice(randIDindex, 1);

            itemsMoved++;
        }
        this.commonUtils.logInfo(`Moved ${itemsMoved} items to the warehouse for LL${ll}.`);
    }
}