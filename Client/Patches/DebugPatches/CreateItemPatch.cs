using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using LateToTheParty.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LateToTheParty.Patches.DebugPatches
{
    internal class CreateItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemFactory).GetMethod(nameof(ItemFactory.CreateItem), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix(ItemFactory __instance, string templateId)
        {
            if (__instance.ItemTemplates.TryGetValue(templateId, out ItemTemplate template))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Creating " + template.Name + " (" + templateId + ")...");
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not find template " + templateId);
            }
        }
    }
}
