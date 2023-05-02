import { CommonUtils } from "./CommonUtils";

export class BotConversionHelper
{
    constructor(private commonUtils: CommonUtils)
    { }

    public setEscapeTime(escapeTime: number, timeRemaining: number): void
    {
        this.commonUtils.logInfo("Updated escape time");
    }

    public stopRaidTimer(): void
    {
        this.commonUtils.logInfo("Raid ended");
    }
}