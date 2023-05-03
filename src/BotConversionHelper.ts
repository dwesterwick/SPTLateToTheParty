import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import { MinMax } from "@spt-aki/models/common/MinMax";
import { IBotConfig } from "@spt-aki/models/spt/config/IBotConfig";

export class BotConversionHelper
{
    private static escapeTime: number
    private static simulatedTimeRemaining: number

    private static commonUtils: CommonUtils
    private static iBotConfig: IBotConfig

    private static timerHandle: NodeJS.Timer
    private static timerRunning: boolean
    private static convertIntoPmcChanceOrig: Record<string, MinMax> = {};

    constructor(commonUtils: CommonUtils, iBotConfig: IBotConfig)
    {
        BotConversionHelper.commonUtils = commonUtils;
        BotConversionHelper.iBotConfig = iBotConfig;

        this.setOriginalData();
    }

    public setEscapeTime(escapeTime: number, timeRemaining: number): void
    {
        if (!this.checkIfInitialized())
        {
            BotConversionHelper.commonUtils.logError("BotConversionHelper object not initialized!");
            return;
        }

        BotConversionHelper.escapeTime = escapeTime;
        BotConversionHelper.simulatedTimeRemaining = timeRemaining;

        if (!BotConversionHelper.timerRunning)
        {
            BotConversionHelper.timerHandle = setInterval(BotConversionHelper.simulateRaidTime, 1000 * modConfig.adjust_pmc_spawn_chances.update_rate);
        }
        
        BotConversionHelper.commonUtils.logInfo("Updated escape time");
    }

    public static stopRaidTimer(): void
    {
        clearInterval(BotConversionHelper.timerHandle);
        BotConversionHelper.timerRunning = false;

        BotConversionHelper.adjustPmcConversionChance(1);

        BotConversionHelper.commonUtils.logInfo("Raid ended");
    }

    public static adjustPmcConversionChance(timeRemainingFactor: number): void
    {
        const adjFactor = CommonUtils.interpolateForFirstCol(modConfig.pmc_spawn_chance_multipliers, timeRemainingFactor);

        let logMessage = "";
        for (const pmcType in BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance)
        {
            BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance[pmcType].min = Math.min(100, BotConversionHelper.convertIntoPmcChanceOrig[pmcType].min * adjFactor);
            BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance[pmcType].max = Math.min(100, BotConversionHelper.convertIntoPmcChanceOrig[pmcType].max * adjFactor);
            
            const min = Math.round(BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance[pmcType].min);
            const max = Math.round(BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance[pmcType].max);
            logMessage += `${pmcType}: ${min}-${max}%, `;
        }

        BotConversionHelper.commonUtils.logInfo(`Adjusting PMC spawn chances (${adjFactor}): ${logMessage}`);
    }

    private checkIfInitialized(): boolean
    {
        if (BotConversionHelper.commonUtils === undefined)
        {
            return false;
        }

        if (BotConversionHelper.iBotConfig === undefined)
        {
            return false;
        }

        return true;
    }

    private setOriginalData(): void
    {
        let logMessage = "";
        for (const pmcType in BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance)
        {
            const chances: MinMax = {
                min: BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance[pmcType].min,
                max: BotConversionHelper.iBotConfig.pmc.convertIntoPmcChance[pmcType].max
            }
            BotConversionHelper.convertIntoPmcChanceOrig[pmcType] = chances;

            logMessage += `${pmcType}: ${chances.min}-${chances.max}%, `;
        }

        BotConversionHelper.commonUtils.logInfo(`Reading default PMC spawn chances: ${logMessage}`);
    }

    private static simulateRaidTime(): void
    {
        BotConversionHelper.timerRunning = true;

        const timeFactor = BotConversionHelper.simulatedTimeRemaining / BotConversionHelper.escapeTime;
        //BotConversionHelper.commonUtils.logInfo(`Time remaining: ${BotConversionHelper.simulatedTimeRemaining}, Factor: ${timeFactor}`);

        BotConversionHelper.adjustPmcConversionChance(timeFactor);

        BotConversionHelper.simulatedTimeRemaining--;
    }
}