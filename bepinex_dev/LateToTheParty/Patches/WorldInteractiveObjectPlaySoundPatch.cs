using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using LateToTheParty.Components;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class WorldInteractiveObjectPlaySoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WorldInteractiveObject).GetMethod(nameof(WorldInteractiveObject.PlaySound), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(WorldInteractiveObject __instance)
        {
            if (Helpers.RaidHelpers.IsInHideout())
            {
                return true;
            }

            if (!Singleton<SwitchTogglingComponent>.Instance.HasToggledInitialSwitches)
            {
                Controllers.LoggingController.LogWarning("Suppressing sound for " + __instance.Id + " until all initial switches have been toggled", true);

                return false;
            }

            return true;
        }
    }
}
