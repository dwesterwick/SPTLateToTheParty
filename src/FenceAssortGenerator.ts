import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { FenceService } from "@spt-aki/services/FenceService";
import { IGetBodyResponseData } from "@spt-aki/models/eft/httpResponse/IGetBodyResponseData";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
import { RandomUtil } from "@spt-aki/utils/RandomUtil";
import { Traders } from "@spt-aki/models/enums/Traders";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";

export class FenceAssortGenerator
{
    private originalAssortData: ITraderAssort

    constructor
    (
        private commonUtils: CommonUtils,
        private databaseTables: IDatabaseTables,
        private jsonUtil: JsonUtil,
        private fenceService: FenceService,
        private httpResponseUtil: HttpResponseUtil,
        private randomUtil: RandomUtil
    )
    {
        this.originalAssortData = this.jsonUtil.clone(this.databaseTables.traders[Traders.FENCE].assort);
    }

    public getFenceAssort(pmcProfile: IPmcData): IGetBodyResponseData<ITraderAssort>
    {
        this.updateFenceAssortIDs();
        this.fenceService.generateFenceAssorts();
        
        return this.httpResponseUtil.getBody(this.fenceService.getFenceAssorts(pmcProfile));
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
}