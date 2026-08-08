using LateToTheParty.Helpers;
using LateToTheParty.Services.Internal;
using LateToTheParty.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace LateToTheParty.Services
{
    [Injectable(TypePriority = OnLoadOrder.PostSptModLoader + LateToTheParty_Server.LOAD_ORDER_OFFSET)]
    public class DebugService : AbstractService
    {
        private LootRankingUtil _lootRankingUtil;

        public DebugService(LoggingUtil logger, ConfigUtil config, LootRankingUtil lootRankingUtil) : base(logger, config)
        {
            _lootRankingUtil = lootRankingUtil;
        }

        protected override void OnLoadIfModIsEnabled()
        {
            if (!Config.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            ForceGenerateLootRankingData();
        }

        private void ForceGenerateLootRankingData()
        {
            Logger.Warning("DEBUG: Generate loot ranking data...");
            _lootRankingUtil.GenerateLootRankingData();
        }
    }
}
