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
        public static string[] CarExtractNames { get; set; } = new string[0];

        private void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Logger.LogInfo("Loading LateToThePartyPlugin...getting configuration data...");
            ModConfig = Controllers.ConfigController.GetConfig();

            if (ModConfig.Enabled)
            {
                Logger.LogInfo("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();

                Logger.LogInfo("Loading LateToThePartyPlugin...getting car extract names...");
                CarExtractNames = Controllers.ConfigController.GetCarExtractNames();
            }
            
            Logger.LogInfo("Loading LateToThePartyPlugin...done.");
        }
    }
}
