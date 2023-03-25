using Aki.Reflection.Patching;
using EFT.UI.Matchmaker;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;

namespace LateToTheParty.Patches
{
    public class ReadyToPlayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MainMenuController).GetMethod("method_45", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(MainMenuController __instance, bool __result, ISession ___ginterface128_0, RaidSettings ___raidSettings_0)
        {
            if (!__result)
            {
                return;
            }

            Logger.LogInfo("Location: " + ___raidSettings_0.SelectedLocation.Name + ". EscapeTimeLimit=" + ___raidSettings_0.SelectedLocation.EscapeTimeLimit);
            ___raidSettings_0.SelectedLocation.EscapeTimeLimit = 12;
        }
    }
}
