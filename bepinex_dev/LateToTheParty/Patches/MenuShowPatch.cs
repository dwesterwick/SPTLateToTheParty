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
        private static string lockableDoorsGUID = "Jehree.LockableDoors";
        private static bool _displayedLockableDoorsWarning = false;

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
                string message = "Using " + lockableDoorsGUID + " may result in loot being despawned behind locked doors even with loot-accessibility checks enabled!";
                LoggingController.LogWarningToServerConsole(message);

                message = "Please see the console for known limitation with " + lockableDoorsGUID;
                NotificationManagerClass.DisplayWarningNotification(message, EFT.Communications.ENotificationDurationType.Long);

                _displayedLockableDoorsWarning = true;
            }
        }

        private static bool couldLockableDoorsCauseIssues()
        {
            if (!ConfigController.Config.DestroyLootDuringRaid.Enabled || !ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.Enabled)
            {
                return false;
            }

            if (!Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID == lockableDoorsGUID))
            {
                return false;
            }

            return true;
        }
    }
}
