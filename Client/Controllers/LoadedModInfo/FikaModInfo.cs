using System;
using Comfort.Common;
using LateToTheParty.Utils;

namespace LateToTheParty.Controllers.LoadedModInfo
{
    public class FikaModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.core";

        public override Version MinCompatibleVersion => new Version("2.1.1");
        public override Version MaxCompatibleVersion => new Version("2.99.99");

        public override string IncompatibilityMessage => $"Installed Fika ({PluginInfo.Metadata.Version}) is not compatible with Late to the Party. Please upgrade Fika to a version between {MinCompatibleVersion} and {MaxCompatibleVersion}.";

        public override bool IsCompatible()
        {
            if (base.IsCompatible())
            {
                return true;
            }

            NotificationManagerClass.DisplayWarningNotification(IncompatibilityMessage, EFT.Communications.ENotificationDurationType.Infinite);
            Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(IncompatibilityMessage);
            return false;
        }

        public override bool CheckInteropAvailability() => true;
    }
}
