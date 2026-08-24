using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class WorldInteractiveObjectSkipEmitterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WorldInteractiveObject).GetMethod(nameof(WorldInteractiveObject.PushTriggers), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(WorldInteractiveObject __instance)
        {
            if (__instance.InteractingPlayer != null)
            {
                return true;
            }

            return false;
        }
    }
}
