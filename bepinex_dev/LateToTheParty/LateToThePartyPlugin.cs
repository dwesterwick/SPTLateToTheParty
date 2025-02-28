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
    [BepInDependency("xyz.drakia.waypoints", "1.6.2")]
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "2.8.4.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";

        private static List<ModulePatch> hostOnlyPatches = new List<ModulePatch>();

        public static void Enable() => enableHostOnlyPatches();
        public static void Disable() => disableHostOnlyPatches();

        protected void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Patches.TarkovInitPatch.MinSPTVersion = "3.10.0.0";
            Patches.TarkovInitPatch.MaxSPTVersion = "3.10.99.0";

            Helpers.VersionCheckHelper.MinFikaSyncPluginVersion = "1.1.0.0";

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

                createHostOnlyPatches();
                enableAllClientPathes();
                enableHostOnlyPatches();
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

        private static void createHostOnlyPatches()
        {
            hostOnlyPatches.Add(new Patches.StartLocalGamePatch());
            hostOnlyPatches.Add(new Patches.OnGameStartedPatch());

            if (ConfigController.Config.DestroyLootDuringRaid.Enabled)
            {
                hostOnlyPatches.Add(new Patches.OnItemAddedOrRemovedPatch());
                hostOnlyPatches.Add(new Patches.OnBeenKilledByAggressorPatch());
                hostOnlyPatches.Add(new Patches.OnBoxLandPatch());
            }

            if (ConfigController.Config.ToggleSwitchesDuringRaid.Enabled)
            {
                hostOnlyPatches.Add(new Patches.WorldInteractiveObjectPlaySoundPatch());
            }
        }

        private static void enableAllClientPathes()
        {
            LoggingController.LogInfo("Enabling patches used by all client machines...");

            new Patches.MenuShowPatch().Enable();
            new Patches.TarkovInitPatch().Enable();
            new Patches.ReadyToPlayPatch().Enable();
            new Patches.GameWorldOnDestroyPatch().Enable();
        }

        private static void enableHostOnlyPatches()
        {
            LoggingController.LogInfo("Enabling patches only used for the host machine...");

            foreach (ModulePatch patch in hostOnlyPatches)
            {
                patch.Enable();
            }
        }

        private static void disableHostOnlyPatches()
        {
            LoggingController.LogWarning("Disabling patches only used for the host machine...");

            foreach (ModulePatch patch in hostOnlyPatches)
            {
                patch.Disable();
            }
        }
    }
}
