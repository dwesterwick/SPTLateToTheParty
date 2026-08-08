using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using Comfort.Common;
using LateToTheParty.Controllers.LoadedModInfo;
using LateToTheParty.Utils;

namespace LateToTheParty.Controllers
{
    public static class ExternalModHandler
    {
        public static FikaModInfo FikaModInfo { get; private set; } = new FikaModInfo();
        public static LTTPFikaSyncModInfo LTTPFikaSyncModInfo { get; private set; } = new LTTPFikaSyncModInfo();
        public static LockableDoorsModInfo LockableDoorsModInfo { get; private set; } = new LockableDoorsModInfo();

        private static List<AbstractExternalModInfo> externalMods = new List<AbstractExternalModInfo>
        {
            FikaModInfo,
            LTTPFikaSyncModInfo,
            LockableDoorsModInfo
        };

        public static void CheckForExternalMods()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Enabled)
            {
                return;
            }

            foreach (AbstractExternalModInfo modInfo in externalMods)
            {
                if (!modInfo.CheckIfInstalled())
                {
                    continue;
                }

                Singleton<LoggingUtil>.Instance.LogInfo($"Found external mod {modInfo.GetName()} (version {modInfo.GetVersion()})");

                if (!modInfo.IsCompatible())
                {
                    Chainloader.DependencyErrors.Add(modInfo.IncompatibilityMessage);
                    continue;
                }

                if (!modInfo.CheckInteropAvailability())
                {
                    Singleton<LoggingUtil>.Instance.LogWarning($"Interoperability for external mod {modInfo.GUID} could not be initialized");
                }
            }
        }
    }
}
