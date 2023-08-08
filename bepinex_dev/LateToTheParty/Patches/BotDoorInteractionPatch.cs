using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Interactive;
using EFT;

namespace LateToTheParty.Patches
{
    public class BotDoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass465).GetMethod("Interact", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Door door, EInteractionType Etype, BotOwner ___botOwner_0)
        {
            string brainLayerName = ___botOwner_0.Brain.ActiveLayerName();
            Controllers.LoggingController.LogInfo("Bot " + ___botOwner_0.Profile.Nickname + " is interacting with door " + door.Id + " in layer " + brainLayerName + "...");
        }
    }
}
