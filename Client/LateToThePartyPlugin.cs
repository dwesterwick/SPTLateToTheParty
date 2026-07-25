using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using LateToTheParty.Helpers;
using LateToTheParty.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty
{
    [BepInDependency("xyz.drakia.waypoints", "1.8.2")]
    [BepInPlugin(ModInfo.GUID, ModInfo.MODNAME, ModInfo.MOD_VERSION)]
    internal class LateToThePartyPlugin : BaseUnityPlugin
    {
        private static List<ModulePatch> hostOnlyPatches = new List<ModulePatch>();

        public static void Enable() => enableHostOnlyPatches();
        public static void Disable() => disableHostOnlyPatches();

        protected void Awake()
        {
            Logger.LogInfo("Loading LateToTheParty...");

            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));

            Logger.LogInfo("Loading LateToTheParty...getting configuration data...");
            Singleton<ConfigUtil>.Create(new ConfigUtil());
            if (Singleton<ConfigUtil>.Instance.CurrentConfig == null)
            {
                Chainloader.DependencyErrors.Add("Could not load " + ModInfo.MODNAME + " because it cannot communicate with the server. Please ensure the mod has been installed correctly.");
                return;
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.IsModEnabled())
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Loading LateToTheParty...enabled");

                createHostOnlyPatches();
                enableAllClientPathes();
                enableHostOnlyPatches();
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading LateToTheParty...done.");
        }

        private static void createHostOnlyPatches()
        {
            hostOnlyPatches.Add(new Patches.StartLocalGamePatch());
            hostOnlyPatches.Add(new Patches.OnGameStartedPatch());

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.Enabled)
            {
                hostOnlyPatches.Add(new Patches.OnItemAddedOrRemovedPatch());
                hostOnlyPatches.Add(new Patches.OnBeenKilledByAggressorPatch());
                hostOnlyPatches.Add(new Patches.OnBoxLandPatch());
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.ToggleSwitchesDuringRaid.Enabled)
            {
                hostOnlyPatches.Add(new Patches.WorldInteractiveObjectPlaySoundPatch());
            }
        }

        private static void enableAllClientPathes()
        {
            Singleton<LoggingUtil>.Instance.LogInfo("Enabling patches used by all client machines...");

            new Patches.MenuShowPatch().Enable();
            new Patches.TarkovInitPatch().Enable();
            new Patches.ReadyToPlayPatch().Enable();
            new Patches.GameWorldOnDestroyPatch().Enable();
            new Patches.WorldInteractiveObjectSkipEmitterPatch().Enable();
        }

        private static void enableHostOnlyPatches()
        {
            Singleton<LoggingUtil>.Instance.LogInfo("Enabling patches only used for the host machine...");

            foreach (ModulePatch patch in hostOnlyPatches)
            {
                patch.Enable();
            }
        }

        private static void disableHostOnlyPatches()
        {
            Singleton<LoggingUtil>.Instance.LogWarning("Disabling patches only used for the host machine...");

            foreach (ModulePatch patch in hostOnlyPatches)
            {
                patch.Disable();
            }
        }
    }
}
