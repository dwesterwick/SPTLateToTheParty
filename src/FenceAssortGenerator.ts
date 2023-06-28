import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { FenceService } from "@spt-aki/services/FenceService";
import { IGetBodyResponseData } from "@spt-aki/models/eft/httpResponse/IGetBodyResponseData";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { JsonUtil } from "@spt-aki/utils/JsonUtil";
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
        private httpResponseUtil: HttpResponseUtil
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
        const assort = this.databaseTables.traders[Traders.FENCE].assort;
        for (const itemID in this.originalAssortData.loyal_level_items)
        {
            if (this.commonUtils.getMaxItemPrice(itemID) > 20000)
            {
                const itemIndex = assort.items.findIndex((i) => i._id == itemID);
                if (itemIndex < 0)
                {
                    continue;
                }

                delete assort.loyal_level_items[itemID];
                delete assort.barter_scheme[itemID];
                assort.items.splice(itemIndex, 1);
            }
        }

        this.commonUtils.logInfo(`Updating Fence assorts... updated to have ${Object.keys(assort.loyal_level_items).length} items`);
    }
}