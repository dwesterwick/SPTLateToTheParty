using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using LateToTheParty.Controllers;
using SPT.Reflection.Patching;

namespace LateToTheParty
{
    [BepInDependency("xyz.drakia.waypoints", "1.6.0")]
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "2.8.0.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";

        private static List<ModulePatch> patches = new List<ModulePatch>();

        public static void Disable()
        {
            if (!ConfigController.Config.Enabled)
            {
                return;
            }

            disablePatches();

            ConfigController.Config.Enabled = false;
        }

        protected void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Patches.TarkovInitPatch.MinVersion = "3.10.0.0";
            Patches.TarkovInitPatch.MaxVersion = "3.10.99.0";

            Logger.LogInfo("Loading LateToThePartyPlugin...getting configuration data...");
            ConfigController.GetConfig();
            LoggingController.Logger = Logger;
            ModName = Info.Metadata.Name;

            if (!confirmNoPreviousVersionExists())
            {
                Chainloader.DependencyErrors.Add("An older version of " + ModName + " still exists in '/BepInEx/plugins'. Please remove LateToTheParty.dll from that directory, or this mod will not work correctly.");
                return;
            }

            if (ConfigController.Config.Enabled)
            {
                string loggingPath = ConfigController.GetLoggingPath();
                LoggingController.SetLoggingPath(loggingPath);

                enablePatches();
            }

            Logger.LogInfo("Loading LateToThePartyPlugin...done.");
        }

        private bool confirmNoPreviousVersionExists()
        {
            string oldPath = AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/LateToTheParty.dll";
            if (File.Exists(oldPath))
            {
                return false;
            }

            return true;
        }

        private static void enablePatches()
        {
            LoggingController.LogInfo("Loading LateToThePartyPlugin...enabling patches...");

            patches.Add(new Patches.ReadyToPlayPatch());
            patches.Add(new Patches.StartLocalGamePatch());
            patches.Add(new Patches.GameWorldOnDestroyPatch());
            patches.Add(new Patches.OnGameStartedPatch());
            patches.Add(new Patches.TarkovInitPatch());
            patches.Add(new Patches.MenuShowPatch());

            if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
            {
                patches.Add(new Patches.OnItemAddedOrRemovedPatch());
                patches.Add(new Patches.OnBeenKilledByAggressorPatch());
                patches.Add(new Patches.OnBoxLandPatch());
            }

            if (ConfigController.Config.ToggleSwitchesDuringRaid.Enabled)
            {
                patches.Add(new Patches.WorldInteractiveObjectPlaySoundPatch());
            }

            foreach (ModulePatch patch in patches)
            {
                patch.Enable();
            }
        }

        private static void disablePatches()
        {
            LoggingController.LogWarning("Disabling all patches...");

            foreach (ModulePatch patch in patches)
            {
                patch.Disable();
            }
        }
    }
}
