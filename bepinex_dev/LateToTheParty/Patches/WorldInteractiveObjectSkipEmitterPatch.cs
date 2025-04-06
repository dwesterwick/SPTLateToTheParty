using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using LateToTheParty.Helpers;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class WorldInteractiveObjectSkipEmitterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            MethodInfo methodInfo = typeof(WorldInteractiveObject)
                .GetMethods()
                .First(m => m.IsUnmapped() && m.HasAllParameterTypesInOrder(new Type[] { typeof(EDoorState) }));

            Controllers.LoggingController.LogInfo("Found method for WorldInteractiveObjectSkipEmitterPatch: " + methodInfo.Name);

            return methodInfo;
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
