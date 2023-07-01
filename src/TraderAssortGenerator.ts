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
    private originalFenceBaseAssortData: ITraderAssort

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
            let maxBuyRate = modConfig.fence_assort_changes.max_item_buy_rate;
            let maxStock = assort.items[i].upd.StackObjectsCount;
            if (itemTpl._parent == modConfig.fence_assort_changes.ammo_parent_id)
            {
                maxBuyRate = modConfig.fence_assort_changes.max_ammo_buy_rate;
                maxStock = this.randomUtil.randInt(0, modConfig.fence_assort_changes.max_ammo_stack);

                // Remove duplicate ammo stacks
                for (let j = i + 1; j < assort.items.length; j++)
                {
                    if (assort.items[j]._tpl == assort.items[i]._tpl)
                    {
                        this.commonUtils.logInfo(`Removing duplicate of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from assort...`);

                        delete assort.loyal_level_items[assort.items[j]._id];
                        delete assort.barter_scheme[assort.items[j]._id];
                        assort.items.splice(j, 1);
                    }
                }
            }

            // Update the stack size
            assort.items[i].upd.StackObjectsCount = maxStock;
            if ((this.lastAssort[traderID] !== undefined) && (this.lastAssort[traderID].nextResupply == assort.nextResupply))
            {
                const lastAssortItem = this.lastAssort[traderID].items.find((item) => (item._id == assort.items[i]._id));
                if (lastAssortItem !== undefined)
                {
                    const maxStackSizeReduction = this.randomUtil.randInt(0, maxBuyRate * (now - this.lastAssortUpdate[traderID]));
                    const newStackSize = lastAssortItem.upd.StackObjectsCount - maxStackSizeReduction;

                    this.commonUtils.logInfo(`Reducing stock of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from ${lastAssortItem.upd.StackObjectsCount} to ${newStackSize}...`);
                    assort.items[i].upd.StackObjectsCount = newStackSize;
                }
                else
                {
                    // If the item wasn't in the previous assort, the stock was depleted
                    this.commonUtils.logInfo(`Stock of ${this.commonUtils.getItemName(assort.items[i]._tpl)} is depleted.`);
                    assort.items[i].upd.StackObjectsCount = 0;
                }
            }

            // Check if the stock has been depleted
            if (assort.items[i].upd.StackObjectsCount <= 0)
            {
                // Remove ammo that is sold out
                if (deleteDepletedItems)
                {
                    delete assort.loyal_level_items[assort.items[i]._id];
                    delete assort.barter_scheme[assort.items[i]._id];
                    assort.items.splice(i, 1);
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

    public updateFenceAssort(pmcProfile: IPmcData): ITraderAssort
    {
        this.updateFenceAssortIDs();
        
        // Ensure the new assorts are generated at least once
        if ((this.lastAssort[Traders.FENCE] === undefined) || modConfig.fence_assort_changes.always_regenerate)
        {
            this.fenceService.generateFenceAssorts();
        }

        return this.fenceService.getFenceAssorts(pmcProfile);
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

                delete assort.loyal_level_items[itemID];
                delete assort.barter_scheme[itemID];
                assort.items.splice(itemIndex, 1);
            }
        }

        this.databaseTables.traders[Traders.FENCE].assort = assort;

        const originalAssortCount = Object.keys(this.originalFenceBaseAssortData.loyal_level_items).length;
        const newAssortCount = Object.keys(this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items).length;
        this.commonUtils.logInfo(`Updated Fence assort data: ${newAssortCount}/${originalAssortCount} items are available for sale.`);
    }

    private modifyFenceConfig(): void
    {
        // Adjust assort size and variety
        this.iTraderConfig.fence.assortSize = modConfig.fence_assort_changes.assort_size;
        this.iTraderConfig.fence.discountOptions.assortSize = modConfig.fence_assort_changes.assort_size_discount;

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
}