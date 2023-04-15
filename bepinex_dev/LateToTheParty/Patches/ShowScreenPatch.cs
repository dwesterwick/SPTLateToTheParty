using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.UI;

namespace LateToTheParty.Patches
{
    public class ShowScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MainMenuController).GetMethod("ShowScreen", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EMenuType screen)
        {
            // Needed for compatibility with Refringe's CustomRaidTimes mod
            if (screen == EMenuType.Play)
            {
                Controllers.LocationSettingsController.ClearOriginalSettings();
            }
        }
    }
}
