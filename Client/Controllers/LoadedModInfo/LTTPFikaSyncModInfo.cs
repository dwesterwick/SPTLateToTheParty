using System;
using Comfort.Common;
using LateToTheParty.Helpers;
using LateToTheParty.Utils;

namespace LateToTheParty.Controllers.LoadedModInfo
{
    public class LTTPFikaSyncModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = ModInfo.GUID + "fikasync";

        private Version? _currentBuildVersion;
        public Version CurrentBuildVersion
        {
            get
            {
                if (_currentBuildVersion == null)
                {
                    _currentBuildVersion = new Version(ModInfo.MOD_VERSION);
                }

                return _currentBuildVersion;
            }
        }

        public override Version MinCompatibleVersion => CurrentBuildVersion.MinBuild();
        public override Version MaxCompatibleVersion => CurrentBuildVersion.MaxBuild();

        public string Name => ModInfo.MODNAME + "FikaSync";

        public override string IncompatibilityMessage => $"Current version {PluginInfo.Metadata.Version} of {Name} is not compatible with Late to the Party. Please install a version between {MinCompatibleVersion} and {MaxCompatibleVersion} or synchronization of doors with Fika clients may not work correctly.";

        public override bool IsCompatible()
        {
            if (base.IsCompatible())
            {
                return true;
            }

            NotificationManagerClass.DisplayWarningNotification(IncompatibilityMessage, EFT.Communications.ENotificationDurationType.Long);
            Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(IncompatibilityMessage);
            return false;
        }

        public override bool CheckInteropAvailability() => true;
    }
}
