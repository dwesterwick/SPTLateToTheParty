import { CommonUtils } from "./CommonUtils";

export class BotConversionHelper
{
    private timerHandle: NodeJS.Timer
    private static escapeTime: number
    private static simulatedTimeRemaining: number
    private static commonUtils: CommonUtils

    constructor(commonUtils: CommonUtils)
    {
        BotConversionHelper.commonUtils = commonUtils;
    }

    public setEscapeTime(escapeTime: number, timeRemaining: number): void
    {
        BotConversionHelper.escapeTime = escapeTime;
        BotConversionHelper.simulatedTimeRemaining = timeRemaining;

        this.timerHandle = setInterval(this.simulateRaidTime, 1000);
        
        BotConversionHelper.commonUtils.logInfo("Updated escape time");
    }

    public stopRaidTimer(): void
    {
        clearInterval(this.timerHandle);
        BotConversionHelper.commonUtils.logInfo("Raid ended");
    }

    private simulateRaidTime(): void
    {
        const timeFactor = BotConversionHelper.simulatedTimeRemaining / BotConversionHelper.escapeTime;
        BotConversionHelper.commonUtils.logInfo(`Time remaining: ${BotConversionHelper.simulatedTimeRemaining}, Factor: ${timeFactor}`);

        BotConversionHelper.simulatedTimeRemaining--;
    }
}