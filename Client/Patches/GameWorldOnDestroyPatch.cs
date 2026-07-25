using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using LateToTheParty.Components;
using LateToTheParty.Utils;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnDestroy), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            // Don't do anything if this is for the hideout
            if (!Controllers.LocationSettingsController.HasRaidStarted)
            {
                return;
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.DestroyLootDuringRaid.Enabled && Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.Enabled)
            {
                Singleton<LootDestroyerComponent>.Instance.LootManager.WriteLootLogFile(Controllers.LocationSettingsController.CurrentLocation.Name);
            }

            // Needed for compatibility with Refringe's CustomRaidTimes mod
            Controllers.LocationSettingsController.ClearOriginalSettings();
        }
    }
}
