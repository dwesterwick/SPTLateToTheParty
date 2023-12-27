﻿using Aki.Reflection.Patching;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Patches
{
    public class StartLocalGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            return localGameType.GetMethod("smethod_6", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPrefix]
        private static void PatchPrefix(ref LocationSettingsClass.Location location)
        {
            Controllers.LocationSettingsController.SetCurrentLocation(location);

            float raidTimeRemainingFraction = (float)location.EscapeTimeLimit / Controllers.LocationSettingsController.GetOriginalEscapeTime(location);
            Controllers.LoggingController.LogInfo("Time remaining fraction: " + raidTimeRemainingFraction);

            Controllers.LocationSettingsController.AdjustBossSpawnChances(location, raidTimeRemainingFraction);
            Controllers.LocationSettingsController.AdjustVExChance(location, raidTimeRemainingFraction);
        }
    }
}