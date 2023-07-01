import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { FenceService } from "@spt-aki/services/FenceService";
import { FenceBaseAssortGenerator } from "@spt-aki/generators/FenceBaseAssortGenerator";
import { IGetBodyResponseData } from "@spt-aki/models/eft/httpResponse/IGetBodyResponseData";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { RandomUtil } from "@spt-aki/utils/RandomUtil";
import { TimeUtil } from "@spt-aki/utils/TimeUtil";
import { ITraderConfig } from "@spt-aki/models/spt/config/ITraderConfig";
import { Traders } from "@spt-aki/models/enums/Traders";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";

export class FenceAssortGenerator
{
    private lastAssortUpdate = -99;
    private lastAssort: ITraderAssort
    private originalAssortData: ITraderAssort

    constructor
    (
        private commonUtils: CommonUtils,
        private databaseTables: IDatabaseTables,
        private jsonUtil: JsonUtil,
        private fenceService: FenceService,
        private fenceBaseAssortGenerator: FenceBaseAssortGenerator,
        private iTraderConfig: ITraderConfig,
        private httpResponseUtil: HttpResponseUtil,
        private randomUtil: RandomUtil,
        private timeUtil: TimeUtil
    )
    {
        this.modifyFenceConfig();
        this.originalAssortData = this.jsonUtil.clone(this.databaseTables.traders[Traders.FENCE].assort);
    }

    public 

    public getFenceAssort(pmcProfile: IPmcData): IGetBodyResponseData<ITraderAssort>
    {
        this.updateFenceAssortIDs();
        
        // Ensure the new assorts are generated at least once
        if ((this.lastAssort === undefined) || modConfig.fence_assort_changes.always_regenerate)
        {
            this.fenceService.generateFenceAssorts();
        }

        const assort = this.fenceService.getFenceAssorts(pmcProfile);
        this.updateAmmoStacks(assort);
        
        return this.httpResponseUtil.getBody(assort);
    }

    public updateFenceAssortIDs(): void
    {
        const assort = this.jsonUtil.clone(this.originalAssortData);
        for (const itemID in this.originalAssortData.loyal_level_items)
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

        const originalAssortCount = Object.keys(this.originalAssortData.loyal_level_items).length;
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
            if (allItems[itemID]._parent != modConfig.fence_assort_changes.blacklist_ammo_parent_id)
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

    private updateAmmoStacks(assort: ITraderAssort): void
    {
        const now = this.timeUtil.getTimestamp();
        
        for (let i = 0; i < assort.items.length; i++)
        {
            const itemTpl = this.databaseTables.templates.items[assort.items[i]._tpl];

            if (itemTpl === undefined)
            {
                this.commonUtils.logWarning(`Could not find template for ID ${assort.items[i]._tpl}`);
                continue;
            }

            // Check if the item is ammo
            if (itemTpl._parent != modConfig.fence_assort_changes.blacklist_ammo_parent_id)
            {
                continue;
            }

            // Remove duplicate ammo stacks
            for (let j = i + 1; j < assort.items.length; j++)
            {
                if (assort.items[j]._tpl == assort.items[i]._tpl)
                {
                    this.commonUtils.logInfo(`Removing duplicate of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from Fence assort...`);

                    delete assort.loyal_level_items[assort.items[j]._id];
                    delete assort.barter_scheme[assort.items[j]._id];
                    assort.items.splice(j, 1);
                }
            }

            // Update the stack size
            if ((this.lastAssort === undefined) || (this.lastAssort.nextResupply != assort.nextResupply))
            {
                const maxAmmoStackSize = this.randomUtil.randInt(0, modConfig.fence_assort_changes.max_ammo_stack);
                assort.items[i].upd.StackObjectsCount = maxAmmoStackSize;
            }
            else
            {
                const maxStackSizeReduction = this.randomUtil.randInt(0, modConfig.fence_assort_changes.max_ammo_buy_rate * (now - this.lastAssortUpdate));
                const newStackSize = this.lastAssort.items[i].upd.StackObjectsCount - maxStackSizeReduction;

                this.commonUtils.logInfo(`Reducing stock of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from ${this.lastAssort.items[i].upd.StackObjectsCount} to ${newStackSize}...`);
                assort.items[i].upd.StackObjectsCount = newStackSize;
            }

            // Remove ammo that is sold out
            if (assort.items[i].upd.StackObjectsCount <= 1)
            {
                if ((this.lastAssort !== undefined) && (this.lastAssort.items[i].upd.StackObjectsCount > 1))
                {
                    this.commonUtils.logInfo(`Removing ${this.commonUtils.getItemName(assort.items[i]._tpl)} from Fence assort...`);
                }

                delete assort.loyal_level_items[assort.items[i]._id];
                delete assort.barter_scheme[assort.items[i]._id];
                assort.items.splice(i, 1);
            }
        }

        // Update the resupply time and stock
        this.lastAssort = this.jsonUtil.clone(assort);
        this.lastAssortUpdate = now;
    }
}