using Aki.Reflection.Patching;
using EFT;
using JetBrains.Annotations;
using LateToTheParty.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Patches
{
    public class GetAvailableActionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1767).GetMethod("GetAvailableActions", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GClass2644 __result, GamePlayerOwner owner, [CanBeNull] GInterface85 interactive)
        {
            LoggingController.LogInfo("Available actions: " + string.Join(",", __result.Actions.Select(a => a.Name)));
        }
    }
}
