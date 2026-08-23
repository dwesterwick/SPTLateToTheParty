using LateToTheParty.Services.Internal;
using LateToTheParty.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace LateToTheParty.Services
{
    [Injectable(TypePriority = OnLoadOrder.Preload + LateToTheParty_Server.LOAD_ORDER_OFFSET)]
    public class ScavRaidSettingsAdjustmentService : AbstractService
    {
        private LocationConfig _locationConfig;

        public ScavRaidSettingsAdjustmentService(LoggingUtil logger, ConfigUtil config, LocationConfig locationConfig) : base(logger, config)
        {
            _locationConfig = locationConfig;
        }

        protected override void OnLoadIfModIsEnabled()
        {
            if (!Config.CurrentConfig.ScavRaidAdjustments.AlwaysSpawnLate && !Config.CurrentConfig.DestroyLootDuringRaid.Enabled)
            {
                return;
            }

            Logger.Info("Adjusting SPT Scav raid changes...");

            foreach (ScavRaidTimeLocationSettings? settings in _locationConfig.ScavRaidTimeSettings.Maps.Values)
            {
                if (settings == null)
                {
                    continue;
                }

                if (Config.CurrentConfig.ScavRaidAdjustments.AlwaysSpawnLate)
                {
                    settings.ReducedChancePercent = 100;
                }

                if (Config.CurrentConfig.DestroyLootDuringRaid.Enabled)
                {
                    settings.ReduceLootByPercent = false;
                }
            }
        }
    }
}
