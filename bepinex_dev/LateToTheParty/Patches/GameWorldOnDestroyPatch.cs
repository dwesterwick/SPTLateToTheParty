﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using EFT.UI;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            // Needed for compatibility with Refringe's CustomRaidTimes mod
            Controllers.LocationSettingsController.ClearOriginalSettings();
        }
    }
}
