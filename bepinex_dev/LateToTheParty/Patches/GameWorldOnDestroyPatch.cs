using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using LateToTheParty.Components;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnDestroy", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            // Don't do anything if this is for the hideout
            if (!Controllers.LocationSettingsController.HasRaidStarted)
            {
                return;
            }

            if (Controllers.ConfigController.Config.DestroyLootDuringRaid.Enabled && Controllers.ConfigController.Config.Debug.Enabled)
            {
                Singleton<LootDestroyerComponent>.Instance.LootManager.WriteLootLogFile(Controllers.LocationSettingsController.CurrentLocation.Name);
            }

            // Needed for compatibility with Refringe's CustomRaidTimes mod
            Controllers.LocationSettingsController.ClearOriginalSettings();
        }
    }
}
