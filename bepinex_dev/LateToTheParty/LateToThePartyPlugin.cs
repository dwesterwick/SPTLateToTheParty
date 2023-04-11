using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.1.1.0")]
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
                new Patches.ShowScreenPatch().Enable();
                new Patches.OnItemAddedOrRemovedPatch().Enable();

                Logger.LogInfo("Loading LateToThePartyPlugin...enabling controllers...");
                Controllers.LootDestroyerController lootDestroyerController = this.GetOrAddComponent<Controllers.LootDestroyerController>();
                Controllers.LootDestroyerController.Logger = Logger;
                Controllers.LootDestroyerController.ModConfig = ModConfig;

                Logger.LogInfo("Loading LateToThePartyPlugin...getting car extract names...");
                CarExtractNames = Controllers.ConfigController.GetCarExtractNames();
            }
            
            Logger.LogInfo("Loading LateToThePartyPlugin...done.");
        }
    }
}
