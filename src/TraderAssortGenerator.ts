import modConfig from "../config/config.json";
import hotItems from "../config/hotItems.json";
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
import { Item } from "@spt-aki/models/eft/common/tables/IItem";

export class TraderAssortGenerator
{
    private lastLL: Record<string, number> = {};
    private lastAssortUpdate: Record<string, number> = {};
    private lastAssort: Record<string, ITraderAssort> = {};
    private originalFenceBaseAssortData: ITraderAssort;
    private modifiedFenceItems: string[];

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
        this.lastLL = {};
        this.modifiedFenceItems = [];
    }

    public updateTraderStock(traderID: string, assort: ITraderAssort, ll: number, deleteDepletedItems: boolean): ITraderAssort
    {
        const now = this.timeUtil.getTimestamp();

        // Initialize data for when the last assort update 
        if (this.lastLL[traderID] == undefined)
        {
            this.lastLL[traderID] = ll;
        }
        if (this.lastAssortUpdate[traderID] === undefined)
        {
            const resupplyTime = this.iTraderConfig.updateTime.find((t) => t.traderId == traderID).seconds
            const timeRemaining = assort.nextResupply - now;
            this.lastAssortUpdate[traderID] = now - (resupplyTime - timeRemaining);
        }
        if ((this.lastLL[traderID] != ll) || (this.lastAssort[traderID] === undefined) || (this.lastAssort[traderID].items.length == 0))
        {
            this.lastAssort[traderID] = this.jsonUtil.clone(assort);
        }

        for (let i = 0; i < assort.items.length; i++)
        {
            // Ensure the stock can actually be reduced
            if ((assort.items[i].upd === undefined) || (assort.items[i].upd.StackObjectsCount === undefined) || (assort.items[i].upd.UnlimitedCount))
            {
                continue;
            }

            // Skip item attachments
            if ((assort.items[i].parentId === undefined) || (assort.items[i].parentId != "hideout"))
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

            // For Fence, combine duplicate items if possible
            if ((traderID == Traders.FENCE) && !CommonUtils.canItemDegrade(assort.items[i], this.databaseTables))
            {
                for (let j = i + 1; j < assort.items.length; j++)
                {
                    if (assort.items[j]._tpl == assort.items[i]._tpl)
                    {
                        //this.commonUtils.logInfo(`Combining ${this.commonUtils.getItemName(assort.items[i]._tpl)} in assort...`);
                        this.removeIndexFromTraderAssort(assort, j);
                        assort.items[i].upd.StackObjectsCount += 1;
                    }
                }
            }

            // Set max ammo stack size
            if ((traderID == Traders.FENCE) && (itemTpl._parent == modConfig.trader_stock_changes.ammo_parent_id))
            {
                assort.items[i].upd.StackObjectsCount = this.randomUtil.randInt(0, modConfig.trader_stock_changes.fence_stock_changes.max_ammo_stack);
            }

            // Update the stack size
            if (this.lastAssort[traderID].nextResupply == assort.nextResupply)
            {
                const lastAssortItem = this.lastAssort[traderID].items.find((item) => (item._id == assort.items[i]._id));
                if (lastAssortItem !== undefined)
                {
                    const isBarter = this.isBarterTrade(assort, assort.items[i]._id);
                    const stackSizeReduction = this.getStackSizeReduction(
                        assort.items[i],
                        isBarter,
                        assort.nextResupply,
                        assort.items[i].upd.StackObjectsCount,
                        lastAssortItem.upd.StackObjectsCount,
                        traderID
                    );

                    if (stackSizeReduction > 0)
                    {
                        const newStackSize = lastAssortItem.upd.StackObjectsCount - stackSizeReduction;
                        if (newStackSize <= 0)
                        {
                            //this.commonUtils.logInfo(`Reducing stock of ${this.commonUtils.getItemName(assort.items[i]._tpl)} from ${lastAssortItem.upd.StackObjectsCount} to ${newStackSize}...`);
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

    public updateFenceAssort(): void
    {
        this.updateFenceAssortIDs();
        
        // Ensure the new assorts are generated at least once
        if ((this.lastAssort[Traders.FENCE] === undefined) || modConfig.trader_stock_changes.fence_stock_changes.always_regenerate)
        {
            this.generateNewFenceAssorts();
        }
    }

    public generateNewFenceAssorts(): void
    {
        this.fenceService.generateFenceAssorts();
        this.modifiedFenceItems = [];
    }

    public replenishFenceStockIfNeeded(currentAssort: ITraderAssort, maxLL: number): boolean
    {
        const ll1ItemIDs = TraderAssortGenerator.getTraderAssortIDsforLL(currentAssort, 1);
        const ll2ItemIDs = TraderAssortGenerator.getTraderAssortIDsforLL(currentAssort, 2);

        if (
            (ll1ItemIDs.length < modConfig.trader_stock_changes.fence_stock_changes.assort_size * modConfig.trader_stock_changes.fence_stock_changes.assort_restock_threshold / 100)
            || ((maxLL > 1) && (ll2ItemIDs.length < modConfig.trader_stock_changes.fence_stock_changes.assort_size_discount * modConfig.trader_stock_changes.fence_stock_changes.assort_restock_threshold / 100))
        )
        {
            //this.commonUtils.logInfo(`Replenishing Fence's assorts. Current LL1 items: ${ll1ItemIDs.length}, LL2 items: ${ll2ItemIDs.length}`);
            this.generateNewFenceAssorts();
            return true;
        }

        return false;
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
            if ((itemPrice >= modConfig.trader_stock_changes.fence_stock_changes.min_allowed_item_value) && (permittedChance <= randNum))
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
    
    public removeExpensivePresets(assort: ITraderAssort, maxCost: number): void
    {
        for (let i = 0; i < assort.items.length; i++)
        {
            if ((assort.items[i].upd === undefined) || (assort.items[i].upd.sptPresetId === undefined) || (assort.items[i].upd.sptPresetId.length == 0))
            {
                continue;
            }

            const id = assort.items[i]._id;
            const cost = assort.barter_scheme[id][0][0].count;

            if (cost > maxCost)
            {
                //this.commonUtils.logInfo(`Removing preset for ${this.commonUtils.getItemName(assort.items[i]._tpl)}...`);
                this.removeIndexFromTraderAssort(assort, i);
                i--;
            }
        }
    }

    public adjustFenceAssortItemPrices(assort: ITraderAssort): void
    {
        for (const i in assort.items)
        {
            if (!CommonUtils.canItemDegrade(assort.items[i], this.databaseTables))
            {
                continue;
            }

            // Find the corresponding item template
            const itemTpl = this.databaseTables.templates.items[assort.items[i]._tpl];
            if (itemTpl === undefined)
            {
                this.commonUtils.logError(`Could not find template for ID ${assort.items[i]._tpl}`);
                continue;
            }

            if (assort.items[i].upd.MedKit !== undefined)
            {
                const durabilityFraction = assort.items[i].upd.MedKit.HpResource / itemTpl._props.MaxHpResource;
                this.adjustFenceItemPrice(assort, assort.items[i], durabilityFraction);
                continue;
            }

            if (assort.items[i].upd.Resource !== undefined)
            {
                const durabilityFraction = assort.items[i].upd.Resource.Value / itemTpl._props.MaxResource;
                this.adjustFenceItemPrice(assort, assort.items[i], durabilityFraction);
                continue;
            }

            if (assort.items[i].upd.Repairable !== undefined)
            {
                const durabilityFraction = assort.items[i].upd.Repairable.Durability / itemTpl._props.MaxDurability;
                this.adjustFenceItemPrice(assort, assort.items[i], durabilityFraction);
                continue;
            }
        }
    }

    private getStackSizeReduction(item: Item, isbarter: boolean, nextResupply: number, originalStock: number, currentStock: number, traderID: string): number
    {
        const now = this.timeUtil.getTimestamp();

        // Find the corresponding item template
        const itemTpl = this.databaseTables.templates.items[item._tpl];
        if (itemTpl === undefined)
        {
            this.commonUtils.logError(`Could not find template for ID ${item._tpl}`);
            return 0;
        }

        const fenceMult = (traderID == Traders.FENCE ? modConfig.trader_stock_changes.fence_stock_changes.sell_chance_multiplier : 1);
        let selloutMult = isbarter ? modConfig.trader_stock_changes.barter_trade_sellout_factor : 1;
        if (itemTpl._id in hotItems)
        {
            selloutMult *= hotItems[itemTpl._id].value * modConfig.trader_stock_changes.hot_item_sell_chance_global_multiplier;
        }
        
        if (itemTpl._parent == modConfig.trader_stock_changes.ammo_parent_id)
        {
            return Math.round(this.randomUtil.getFloat(0, 1) * modConfig.trader_stock_changes.max_ammo_buy_rate * selloutMult / fenceMult * (now - this.lastAssortUpdate[traderID]));
        }

        const refreshFractionElapsed = 1 - ((nextResupply - now) / this.iTraderConfig.updateTime.find((t) => t.traderId == traderID).seconds);
        const maxItemsSold = modConfig.trader_stock_changes.item_sellout_chance / 100 * originalStock * refreshFractionElapsed * selloutMult * fenceMult;
        const itemsSold = originalStock - currentStock;
        const maxReduction = this.randomUtil.getFloat(0, 1) * modConfig.trader_stock_changes.max_item_buy_rate * selloutMult * (now - this.lastAssortUpdate[traderID]);
        const itemsToSell = Math.round(Math.max(0, Math.min(maxItemsSold - itemsSold, maxReduction)));

        //this.commonUtils.logInfo(`Refresh fraction: ${refreshFractionElapsed}, Max items sold: ${maxItemsSold}, Items to sell; ${itemsToSell}`);

        return itemsToSell;
    }
    
    private isBarterTrade(assort: ITraderAssort, itemID: string): boolean
    {
        if (assort.barter_scheme[itemID] === undefined)
        {
            if (assort.items.find((i) => i._id == itemID).parentId != "hideout")
            {
                return false;
            }

            this.commonUtils.logError(`Could not find barter template for ID ${itemID}`);
            return false;
        }

        for (const i in assort.barter_scheme[itemID][0])
        {
            const barterItemTpl = assort.barter_scheme[itemID][0][i]._tpl;

            // Find the corresponding item template
            const itemTpl = this.databaseTables.templates.items[barterItemTpl];
            if (itemTpl === undefined)
            {
                this.commonUtils.logError(`Could not find template for ID ${barterItemTpl}`);
                return false;
            }
            
            if (CommonUtils.hasParent(itemTpl, modConfig.trader_stock_changes.money_parent_id, this.databaseTables))
            {
                return false;
            }
        }

        //this.commonUtils.logInfo(`Item ${this.commonUtils.getItemName(assort.items.find((i) => i._id == itemID)._tpl)} is a barter trade.`);
        return true;
    }

    private adjustFenceItemPrice(assort: ITraderAssort, item: Item, durabilityFraction: number): void
    {
        // Ensure the item hasn't already been modified
        const id = item._id;
        if (this.modifiedFenceItems.includes(id))
        {
            return;
        }

        const costFactor = CommonUtils.interpolateForFirstCol(modConfig.item_cost_fraction_vs_durability, durabilityFraction);

        //this.commonUtils.logInfo(`Modifying value of  ${this.commonUtils.getItemName(item._tpl)} by ${costFactor}...`);
        assort.barter_scheme[id][0][0].count *= costFactor;
        this.modifiedFenceItems.push(id);
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
        this.iTraderConfig.fence.assortSize = modConfig.trader_stock_changes.fence_stock_changes.assort_size;
        this.iTraderConfig.fence.discountOptions.assortSize = modConfig.trader_stock_changes.fence_stock_changes.assort_size_discount;
        this.iTraderConfig.fence.maxPresetsPercent = modConfig.trader_stock_changes.fence_stock_changes.maxPresetsPercent;
        
        for (const itemID in modConfig.trader_stock_changes.fence_stock_changes.itemTypeLimits_Override)
        {
            this.iTraderConfig.fence.itemTypeLimits[itemID] = modConfig.trader_stock_changes.fence_stock_changes.itemTypeLimits_Override[itemID];
        }

        // Add or remove ID's from the blacklist
        this.iTraderConfig.fence.blacklist = this.iTraderConfig.fence.blacklist.concat(modConfig.trader_stock_changes.fence_stock_changes.blacklist_append);
        const removeID = modConfig.trader_stock_changes.fence_stock_changes.blacklist_remove;
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
            if (allItems[itemID]._parent != modConfig.trader_stock_changes.ammo_parent_id)
            {
                continue;
            }

            if ((allItems[itemID]._props.PenetrationPower === undefined) || (allItems[itemID]._props.Damage === undefined))
            {
                continue;
            }

            if (allItems[itemID]._props.PenetrationPower > modConfig.trader_stock_changes.fence_stock_changes.blacklist_ammo_penetration_limit)
            {
                this.iTraderConfig.fence.blacklist.push(itemID);
                continue;
            }

            if (allItems[itemID]._props.Damage > modConfig.trader_stock_changes.fence_stock_changes.blacklist_ammo_damage_limit)
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
}