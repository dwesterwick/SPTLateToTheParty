using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT.Quests;
using LateToTheParty.Controllers;

namespace LateToTheParty.Patches
{
    public class QuestSetStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1259).GetMethod("SetStatus", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(GClass1259 __instance, EQuestStatus status, bool notify, bool fromServer)
        {
            SetQuestStatus(__instance, status, notify, fromServer);
        }

        public static void SetQuestStatus<T>(AbstractQuestClass<T> __instance, EQuestStatus status, bool notify, bool fromServer) where T: IConditionCounter
        {
            // Ignore quests that already have this status
            if (__instance.QuestStatus == status)
            {
                return;
            }

            // Ignore status changes that won't result in trader assort unlocks
            if ((status != EQuestStatus.Success) && (status != EQuestStatus.Started))
            {
                return;
            }

            LoggingController.LogInfo("Quest status for " + __instance.Id + " changed from " + __instance.QuestStatus.ToString() + " to " + status.ToString());
            ConfigController.ShareQuestStatusChange(__instance.Id, status.ToString());
        }
    }
}
