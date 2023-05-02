import modConfig from "../config/config.json";
import { ILogger } from "@spt-aki/models/spt/utils/ILogger";

export class CommonUtils
{
    private debugMessagePrefix = "[Late to the Party] ";
	
    constructor (private logger: ILogger)
    { }
	
    public logInfo(message: string): void
    {
        if (modConfig.debug)
            this.logger.info(this.debugMessagePrefix + message);
    }
}