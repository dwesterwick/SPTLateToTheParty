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
            ForceLateScavSpawns();
            DisableSptLootReductionForScavRaids();
        }

        public void ForceLateScavSpawns()
        {
            if (!Config.CurrentConfig.ScavRaidAdjustments.AlwaysSpawnLate)
            {
                return;
            }

            Logger.Info("Forcing Scav raids to never start at the beginnning of the raid...");

            foreach (ScavRaidTimeLocationSettings? settings in _locationConfig.ScavRaidTimeSettings.Maps.Values)
            {
                if (settings == null)
                {
                    continue;
                }

                settings.ReducedChancePercent = 100;
            }
        }

        private void DisableSptLootReductionForScavRaids()
        {
            if (!Config.CurrentConfig.DestroyLootDuringRaid.Enabled)
            {
                return;
            }

            Logger.Info("Disabling SPT's loot reduction for Scav raids...");

            foreach (ScavRaidTimeLocationSettings? settings in _locationConfig.ScavRaidTimeSettings.Maps.Values)
            {
                if (settings == null)
                {
                    continue;
                }

                settings.ReduceLootByPercent = false;
            }
        }
    }
}
