using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    public class DoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1766).GetMethod("smethod_9", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(GamePlayerOwner owner, Door door)
        {
            // Do not log when the door controller is looking for valid doors
            if (DoorController.IsFindingDoors)
            {
                return;
            }

            // Ignore interactions from bots
            if ((owner.Player == null) || (owner.Player.Id != Singleton<GameWorld>.Instance.MainPlayer.Id))
            {
                return;
            }

            Controllers.LoggingController.LogInfo("Checking interaction options for door: " + door.Id + "...");
        }
    }
}
