using LateToTheParty.Server.Internal;
using LateToTheParty.Services;
using LateToTheParty.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace LateToTheParty.Server
{
    public class RaidAdjustmentsTests
    {
        private ISptLogger<LateToTheParty_Server> _logger;
        private LoggingUtil _loggingUtil;
        private MockConfigUtil _configUtil;

        private ModHelper _modHelper = null!;
        private LocationConfig _locationConfig = null!;

        private ScavRaidSettingsAdjustmentService _scavRaidSettingsAdjustmentService;

        [SetUp]
        public void Setup()
        {
            RunFromSptInstallDirectoryService.RunFromSptInstallDirectory(LoadSptDependencies);

            _logger = new MockLogger<LateToTheParty_Server>();
            _configUtil = new MockConfigUtil(_modHelper);
            _loggingUtil = new LoggingUtil(_logger);

            _scavRaidSettingsAdjustmentService = new ScavRaidSettingsAdjustmentService(_loggingUtil, _configUtil, _locationConfig);
        }

        [Test]
        public void EnsureScavRaidsCanBeForcedToAlwaysSpawnLate()
        {
            _configUtil.CurrentConfig.ScavRaidAdjustments.AlwaysSpawnLate = true;

            _scavRaidSettingsAdjustmentService.ForceLateScavSpawns();

            foreach (ScavRaidTimeLocationSettings? settings in _locationConfig.ScavRaidTimeSettings.Maps.Values)
            {
                if (settings == null)
                {
                    continue;
                }

                Assert.AreEqual(settings.ReducedChancePercent, 100);
            }
        }

        private void LoadSptDependencies()
        {
            _modHelper = DI.GetInstance().GetService<ModHelper>();
            _locationConfig = DI.GetInstance().GetService<LocationConfig>();
        }
    }
}
