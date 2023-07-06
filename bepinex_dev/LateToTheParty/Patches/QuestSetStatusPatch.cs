using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Quests;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    public class QuestSetStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(QuestClass).GetMethod("SetStatus", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(QuestClass __instance, EQuestStatus status)
        {
            if ((status != EQuestStatus.Success) || (__instance.QuestStatus == EQuestStatus.Success))
            {
                return;
            }

            LoggingController.LogInfo("Quest " + __instance.Id + " was completed. Current status: " + __instance.QuestStatus.ToString());
        }
    }
}
