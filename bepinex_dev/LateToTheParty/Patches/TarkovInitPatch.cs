using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using BepInEx.Bootstrap;
using EFT;
using EFT.InputSystem;

namespace LateToTheParty.Patches
{
    public class TarkovInitPatch : ModulePatch
    {
        public static string MinSPTVersion { get; set; } = "0.0.0.0";
        public static string MaxSPTVersion { get; set; } = "999999.999999.999999.999999";

        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.Init), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(IAssetsManager assetsManager, InputTree inputTree)
        {
            if (!Helpers.VersionCheckHelper.IsSPTWithinVersionRange(MinSPTVersion, MaxSPTVersion, out string currentVersion))
            {
                string errorMessage = createSPTVersionErrorMessage(currentVersion);
                Chainloader.DependencyErrors.Add(errorMessage);
                return;
            }

            if (!Helpers.VersionCheckHelper.IsFikaSyncPluginCompatible())
            {
                Chainloader.DependencyErrors.Add("Please update your LTTP Fika Sync Plugin to the latest version.");
                return;
            }
        }

        private static string createSPTVersionErrorMessage(string currentVersion)
        {
            string errorMessage = "Could not load " + LateToThePartyPlugin.ModName + " because it requires SPT ";

            if (MinSPTVersion == MaxSPTVersion)
            {
                errorMessage += MinSPTVersion;
            }
            else if (MaxSPTVersion == "999999.999999.999999.999999")
            {
                errorMessage += MinSPTVersion + " or later";
            }
            else if (MinSPTVersion == "0.0.0.0")
            {
                errorMessage += MaxSPTVersion + " or older";
            }
            else
            {
                errorMessage += "between versions " + MinSPTVersion + " and " + MaxSPTVersion;
            }

            errorMessage += ". The current version is " + currentVersion + ".";

            return errorMessage;
        }
    }
}
