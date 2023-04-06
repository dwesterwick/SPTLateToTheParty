using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace LateToTheParty.Patches
{
    public class OnItemRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, Item item, ItemAddress location, bool added)
        {
            if (added)
            {
                return;
            }

            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if (__instance != Singleton<GameWorld>.Instance.MainPlayer)
            {
                return;
            }

            if (!LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.IgnoreItemsDroppedByPlayer.Enabled)
            {
                Logger.LogInfo("Main player removed item: " + item.LocalizedName() + " (ignored)");
                return;
            }

            if (LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.IgnoreItemsDroppedByPlayer.OnlyItemsBroughtIntoRaid && item.SpawnedInSession)
            {
                Logger.LogInfo("Main player removed item: " + item.LocalizedName() + " (ignored because not FIR)");
                return;
            }

            if (!Controllers.LootDestroyerController.ItemsDroppedByMainPlayer.Contains(item))
            {
                foreach (Item relevantItem in Controllers.LootDestroyerController.FindAllItemsInContainer(item).Append(item))
                {
                    Logger.LogInfo("Main player removed item: " + item.LocalizedName() + " (Spawned in session: " + item.SpawnedInSession + ")");
                    Controllers.LootDestroyerController.ItemsDroppedByMainPlayer.Add(relevantItem);
                }
            }
        }
    }
}
