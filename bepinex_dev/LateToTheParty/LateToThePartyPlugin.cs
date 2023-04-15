using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.1.3.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; } = null;
        public static Configuration.ModConfig ModConfig { get; private set; } = null;
        public static string[] CarExtractNames { get; set; } = new string[0];

        private void Awake()
        {
            Log = Logger;

            Log.LogInfo("Loading LateToThePartyPlugin...");

            Log.LogInfo("Loading LateToThePartyPlugin...getting configuration data...");
            ModConfig = Controllers.ConfigController.GetConfig();

            if (ModConfig.Enabled)
            {
                Log.LogInfo("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();
                new Patches.ShowScreenPatch().Enable();
                new Patches.OnItemAddedOrRemovedPatch().Enable();

                Log.LogInfo("Loading LateToThePartyPlugin...enabling controllers...");
                Controllers.LootDestroyerController lootDestroyerController = this.GetOrAddComponent<Controllers.LootDestroyerController>();
                Controllers.LootDestroyerController.Logger = Log;
                Controllers.LootDestroyerController.ModConfig = ModConfig;

                Log.LogInfo("Loading LateToThePartyPlugin...getting car extract names...");
                CarExtractNames = Controllers.ConfigController.GetCarExtractNames();
            }

            Log.LogInfo("Loading LateToThePartyPlugin...done.");
        }
    }
}
