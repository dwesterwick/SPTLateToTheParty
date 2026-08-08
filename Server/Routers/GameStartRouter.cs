using LateToTheParty.Routers.Internal;
using LateToTheParty.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace LateToTheParty.Routers
{
    [Injectable]
    internal class GameStartRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["/client/game/start"];

        private LootRankingUtil _lootRankingUtil;

        public GameStartRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, LootRankingUtil lootRankingUtil) : base(_routeNames, logger, config, jsonUtil)
        {
            _lootRankingUtil = lootRankingUtil;
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            _lootRankingUtil.GenerateLootRankingData();

            return new ValueTask<string?>(routerData.Output);
        }
    }
}
