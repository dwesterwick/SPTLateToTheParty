using Comfort.Common;
using EFT;
using LateToTheParty.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LateToTheParty.Controllers.LoadedModInfo
{
    public class SainModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "me.sol.sain";

        public override Version MinCompatibleVersion => new Version("4.3.0");
        public override Version MaxCompatibleVersion => new Version("4.4.3");

        public override string IncompatibilityMessage => $"Installed SAIN ({PluginInfo.Metadata.Version}) is not compatible with Late to the Party. Please upgrade SAIN to a version between {MinCompatibleVersion} and {MaxCompatibleVersion}.";

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

        public override bool CheckInteropAvailability()
        {
            if (SAIN.Interop.SAINInterop.Init())
            {
                CanUseInterop = true;
            }

            return CanUseInterop;
        }

        public bool AnySainBotsWithinRange(Vector3 sourcePosition, double range)
        {
            return GetAllSainBotsWithinRange(sourcePosition, range).Any();
        }

        public IEnumerable<BotOwner> GetAllSainBotsWithinRange(Vector3 sourcePosition, double range)
        {
            if (!CanUseInterop)
            {
                return Enumerable.Empty<BotOwner>();
            }

            return Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(bot => bot?.IsDead == false)
                .Where(bot => Vector3.Distance(sourcePosition, bot.Position) < range)
                .Where(bot => IsSainBot(bot));
        }

        public BotOwner? GetNearestSainBot(Vector3 sourcePosition)
        {
            if (!CanUseInterop)
            {
                return null;
            }

            return Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(bot => bot?.IsDead == false)
                .Where(bot => IsSainBot(bot))
                .OrderBy(bot => Vector3.Distance(sourcePosition, bot.Position))
                .FirstOrDefault();
        }

        public bool IsSainBot(BotOwner bot)
        {
            if (!CanUseInterop)
            {
                return false;
            }

            return SAIN.Interop.SAINInterop.GetPersonality(bot) != string.Empty;
        }
    }
}
