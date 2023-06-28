import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { TraderController } from "@spt-aki/controllers/TraderController";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { FenceService } from "@spt-aki/services/FenceService";
import { IGetBodyResponseData } from "@spt-aki/models/eft/httpResponse/IGetBodyResponseData";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { Traders } from "@spt-aki/models/enums/Traders";
import { IPmcData } from "@spt-aki/models/eft/common/IPmcData";

export class FenceAssortGenerator
{
    private originalAssortIDs: Record<string, number> = {};

    constructor
    (
        private commonUtils: CommonUtils,
        private databaseTables: IDatabaseTables,
        private fenceService: FenceService,
        private traderController: TraderController,
        private httpResponseUtil: HttpResponseUtil
    )
    {
        // Store original Fence assort ID's
        const assortIDs = this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items;
        this.commonUtils.logInfo(`Fence assorts currently has ${Object.keys(assortIDs).length} items`);
        for (const itemID in assortIDs)
        {
            this.originalAssortIDs[itemID] = assortIDs[itemID];
        }
    }

    public getFenceAssort(pmcProfile: IPmcData): IGetBodyResponseData<ITraderAssort>
    {
        this.updateFenceAssortIDs();
        this.fenceService.generateFenceAssorts();
        
        return this.httpResponseUtil.getBody(this.fenceService.getFenceAssorts(pmcProfile));
    }

    public updateFenceAssortIDs(): void
    {
        let assortIDs = this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items;
        this.commonUtils.logInfo(`Updating Fence assorts... currently has ${Object.keys(assortIDs).length} items...`);

        assortIDs = {};
        for (const itemID in this.originalAssortIDs)
        {
            if (this.commonUtils.getMaxItemPrice(itemID) < 50000)
            {
                assortIDs[itemID] = this.originalAssortIDs[itemID];
            }
        }

        this.commonUtils.logInfo(`Updating Fence assorts... updated to have ${Object.keys(assortIDs).length} items`);
    }
}