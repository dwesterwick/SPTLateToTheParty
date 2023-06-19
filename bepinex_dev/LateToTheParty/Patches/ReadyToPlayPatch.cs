using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;

namespace LateToTheParty.Patches
{
    public class ReadyToPlayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Method 45 always runs, but sometimes twice. Method 42 runs before pressing "Ready", but won't work if you press "Ready" early.
            string methodName = "method_45";
            if (Controllers.ConfigController.Config.Debug.Enabled)
            {
                methodName = "method_42";
            }

            return typeof(MainMenuController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(bool __result, RaidSettings ___raidSettings_0)
        {
            // Don't bother running the code if the game wouldn't allow you into a raid anyway
            if (!__result)
            {
                return;
            }

            Controllers.LocationSettingsController.ModifyLocationSettings(___raidSettings_0.SelectedLocation, ___raidSettings_0.IsScav);
        }
    }
}
