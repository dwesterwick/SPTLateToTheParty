import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { TraderController } from "@spt-aki/controllers/TraderController";
import { ITraderAssort } from "@spt-aki/models/eft/common/tables/ITrader";
import { IGetBodyResponseData } from "@spt-aki/models/eft/httpResponse/IGetBodyResponseData";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import { Traders } from "@spt-aki/models/enums/Traders";

export class FenceAssortGenerator
{
    private originalAssortIDs: Record<string, number> = {};

    constructor
    (
        private commonUtils: CommonUtils,
        private databaseTables: IDatabaseTables,
        private traderController: TraderController,
        private httpResponseUtil: HttpResponseUtil
    )
    {
        // Store original Fence assort ID's
        const assortIDs = this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items;
        for (const itemID in assortIDs)
        {
            this.originalAssortIDs[itemID] = assortIDs[itemID];
        }
    }

    public getFenceAssort(sessionID: string): IGetBodyResponseData<ITraderAssort>
    {
        this.commonUtils.logInfo("Updating Fence assort...");

        return this.httpResponseUtil.getBody(this.traderController.getAssort(sessionID, Traders.FENCE));
    }

    private updateFenceAssortIDs(): void
    {
        this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items = {};
        for (const itemID in this.originalAssortIDs)
        {
            if (this.commonUtils.getMaxItemPrice(itemID) < 50000)
            {
                this.databaseTables.traders[Traders.FENCE].assort.loyal_level_items[itemID] = this.originalAssortIDs[itemID];
            }
        }
    }
}