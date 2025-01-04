using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;
using SPT.Reflection.Patching;
using LateToTheParty.Controllers;
using BepInEx.Bootstrap;

namespace LateToTheParty.Patches
{
    public class MenuShowPatch : ModulePatch
    {
        private static string _lockableDoorsGUID = "Jehree.LockableDoors";
        private static string _fikaGUID = "com.fika.core";
        private static string _fikaSyncGUID = "com.DanW.LateToThePartyFikaSync";
        private static bool _displayedLockableDoorsWarning = false;
        private static bool _displayedFikaWarning = false;

        protected override MethodBase GetTargetMethod()
        {
            // Same as SPT method to display plugin errors
            return typeof(MenuScreen).GetMethods().First(m => m.Name == nameof(MenuScreen.Show));
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            if (!_displayedLockableDoorsWarning && couldLockableDoorsCauseIssues())
            {
                string message = "Using " + _lockableDoorsGUID + " may result in loot being despawned behind locked doors even with loot-accessibility checks enabled!";
                LoggingController.LogWarningToServerConsole(message);

                message = "Please see the console for known limitation with " + _lockableDoorsGUID;
                NotificationManagerClass.DisplayWarningNotification(message, EFT.Communications.ENotificationDurationType.Long);

                _displayedLockableDoorsWarning = true;
            }

            if (!_displayedFikaWarning && fikaInstalledWithoutSyncPlugin())
            {
                string message = "You must use " + _fikaSyncGUID + " when using " + _fikaGUID + " or the states of doors and switches will not sync between clients!";
                LoggingController.LogErrorToServerConsole(message);

                message = "Missing LateToTheParty Fika sync plugin";
                NotificationManagerClass.DisplayWarningNotification(message, EFT.Communications.ENotificationDurationType.Long);

                _displayedFikaWarning = true;
            }
        }

        private static bool couldLockableDoorsCauseIssues()
        {
            if (!ConfigController.Config.DestroyLootDuringRaid.Enabled || !ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.Enabled)
            {
                return false;
            }

            if (!Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID == _lockableDoorsGUID))
            {
                return false;
            }

            return true;
        }

        private static bool fikaInstalledWithoutSyncPlugin()
        {
            if (!Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID == _fikaGUID))
            {
                return false;
            }

            if (Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID == _fikaSyncGUID))
            {
                return false;
            }

            return true;
        }
    }
}
