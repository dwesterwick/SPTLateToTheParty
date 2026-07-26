using LateToTheParty.Helpers;
using LateToTheParty.Routers.Internal;
using LateToTheParty.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace LateToTheParty.Routers
{
    [Injectable]
    internal class LootRankingDataRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetLootRankingData"];

        private LootRankingUtil _lootRankingUtil;

        public LootRankingDataRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, LootRankingUtil lootRankingUtil) : base(_routeNames, logger, config, jsonUtil)
        {
            _lootRankingUtil = lootRankingUtil;
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            string json = ConfigHelpers.Serialize(_lootRankingUtil.GetLootRankingData());
            return new ValueTask<string?>(json);
        }
    }
}
