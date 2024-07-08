using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using EFT.UI.Matchmaker;

namespace LateToTheParty.Patches
{
    public class ReadyToPlayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchmakerTimeHasCome).GetMethod(
                "Show",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(ISession), typeof(RaidSettings) },
                null);
        }

        [PatchPostfix]
        private static void PatchPostfix(ISession session, RaidSettings raidSettings)
        {
            Controllers.LocationSettingsController.CacheLocationSettings(raidSettings.SelectedLocation);
        }
    }
}
