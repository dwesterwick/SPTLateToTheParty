using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using EFT;
using EFT.UI;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.0.0.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        public static Configuration.ModConfig ModConfig { get; set; } = null;

        private void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Logger.LogDebug("Loading LateToThePartyPlugin...getting configuration data...");
            ModConfig = Controllers.ConfigController.GetConfig();

            if (ModConfig.Enabled)
            {
                Logger.LogDebug("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();
            }
            
            Logger.LogDebug("Loading LateToThePartyPlugin...done.");
        }
    }
}
