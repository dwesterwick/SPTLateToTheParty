﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT.Quests;

namespace LateToTheParty.Patches
{
    public class QuestSetStatusPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1260).GetMethod("SetStatus", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(GClass1260 __instance, EQuestStatus status, bool notify, bool fromServer)
        {
            QuestSetStatusPatch.SetQuestStatus(__instance, status, notify, fromServer);
        }
    }
}
