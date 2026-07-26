using LateToTheParty.Helpers;
using LateToTheParty.Routers.Internal;
using LateToTheParty.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace LateToTheParty.Routers
{
    [Injectable]
    internal class CarExtractNamesRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetCarExtractNames"];

        private InRaidConfig _inRaidConfig;

        public CarExtractNamesRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, ConfigServer configServer) : base(_routeNames, logger, config, jsonUtil)
        {
            _inRaidConfig = configServer.GetConfig<InRaidConfig>();
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            string json = ConfigHelpers.Serialize(_inRaidConfig.CarExtracts);
            return new ValueTask<string?>(json);
        }
    }
}
