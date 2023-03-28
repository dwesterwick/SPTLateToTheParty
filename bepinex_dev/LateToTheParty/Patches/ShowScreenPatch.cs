using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

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
                Logger.LogInfo("Discarding original raid settings...");
                Patches.ReadyToPlayPatch.OriginalSettings.Clear();
            }
        }
    }
}
