import modConfig from "../config/config.json";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";
import { LocaleService } from "@spt-aki/services/LocaleService";

export class CommonUtils
{
    private debugMessagePrefix = "[Late to the Party] ";
    private translations: Record<string, string>;
	
    constructor (private logger: ILogger, private databaseTables: IDatabaseTables, private localeService: LocaleService)
    {
        // Get all translations for the current locale
        this.translations = this.localeService.getLocaleDb();
    }
	
    public logInfo(message: string, alwaysShow = false): void
    {
        if (modConfig.debug.enabled || alwaysShow)
            this.logger.info(this.debugMessagePrefix + message);
    }

    public logWarning(message: string): void
    {
        this.logger.warning(this.debugMessagePrefix + message);
    }

    public logError(message: string): void
    {
        this.logger.error(this.debugMessagePrefix + message);
    }

    public getItemName(itemID: string): string
    {
        const translationKey = itemID + " Name";
        if (translationKey in this.translations)
            return this.translations[translationKey];
		
        // If a key can't be found in the translations dictionary, fall back to the template data if possible
        if (!(itemID in this.databaseTables.templates.items))
        {
            return undefined;
        }

        const item = this.databaseTables.templates.items[itemID];
        return item._name;
    }

    /**
     * Check if @param item is a child of the item with ID @param parentID
     */
    public static hasParent(item: ITemplateItem, parentID: string, databaseTables: IDatabaseTables): boolean
    {
        const allParents = CommonUtils.getAllParents(item, databaseTables);
        return allParents.includes(parentID);
    }

    public static getAllParents(item: ITemplateItem, databaseTables: IDatabaseTables): string[]
    {
        if ((item._parent === null) || (item._parent === undefined) || (item._parent == ""))
            return [];
		
        const allParents = CommonUtils.getAllParents(databaseTables.templates.items[item._parent], databaseTables);
        allParents.push(item._parent);
		
        return allParents;
    }

    public static interpolateForFirstCol(array: number[][], value: number): number
    {
        if (array.length == 1)
        {
            return array[array.length][1];
        }

        if (value <= array[0][0])
        {
            return array[0][1];
        }

        for (let i = 1; i < array.length; i++)
        {
            if (array[i][0] >= value)
            {
                if (array[i][0] - array[i - 1][0] == 0)
                {
                    return array[i][1];
                }

                return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
            }
        }

        return array[array.length][1];
    }
}