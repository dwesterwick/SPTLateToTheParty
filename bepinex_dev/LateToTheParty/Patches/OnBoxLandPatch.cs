using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Custom.Airdrops;
using SPT.Reflection.Patching;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    internal class OnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(AirdropBox __instance)
        {
            LootManager.AddLootableContainer(__instance.container);
        }
    }
}
