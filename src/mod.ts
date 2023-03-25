import modConfig from "../config/config.json";

import { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type {StaticRouterModService} from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil"

const modName = "LateToTheParty";

class LateToTheParty implements IPreAkiLoadMod
{
    private httpResponseUtil: HttpResponseUtil;
	
    public preAkiLoad(container: DependencyContainer): void
    {
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        
        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`,
            [{
                url: "/LateToTheParty/GetConfig",
                action: () => 
                {
                    return JSON.stringify(modConfig);
                }
            }], "GetConfig"
        );  
    }
}
module.exports = {mod: new LateToTheParty()}