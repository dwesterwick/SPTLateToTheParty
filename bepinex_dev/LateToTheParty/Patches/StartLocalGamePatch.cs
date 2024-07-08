using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;

namespace LateToTheParty.Patches
{
    public class StartLocalGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = SPT.Reflection.Utils.PatchConstants.LocalGameType;
            return localGameType.GetMethod("smethod_6", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        private static void PatchPrefix(ref LocationSettingsClass.Location location)
        {
            Controllers.LocationSettingsController.SetCurrentLocation(location);

            float raidTimeRemainingFraction = (float)location.EscapeTimeLimit / Controllers.LocationSettingsController.GetOriginalEscapeTime(location);
            Controllers.LoggingController.LogInfo("Time remaining fraction: " + raidTimeRemainingFraction);

            Controllers.LocationSettingsController.AdjustBossSpawnChances(location, raidTimeRemainingFraction);

            // Only used to test car-extract departures
            //Controllers.LocationSettingsController.AdjustVExChance(location, 100);
        }
    }
}
