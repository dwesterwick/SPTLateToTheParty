using System;
using Comfort.Common;
using LateToTheParty.Utils;

namespace LateToTheParty.Controllers.LoadedModInfo
{
    public class LockableDoorsModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "Jehree.LockableDoors";

        public override Version MinCompatibleVersion => new Version("2.0.0");
        public override Version MaxCompatibleVersion => new Version("2.0.99");

        public override string IncompatibilityMessage => $"Installed version of Lockable Doors ({PluginInfo.Metadata.Version}) is not compatible with Late to the Party. Please upgrade Lockable Doors to a version between {MinCompatibleVersion} and {MaxCompatibleVersion}.";

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
