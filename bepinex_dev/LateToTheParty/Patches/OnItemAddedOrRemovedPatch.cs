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
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Patches
{
    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, Item item, ItemAddress location, bool added)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if (__instance != Singleton<GameWorld>.Instance.MainPlayer)
            {
                return;
            }

            // If you pick up an item, it needs to be removed from the loot lists to prevent it from being randomly despawned while in your inventory
            if (added)
            {
                Logger.LogInfo("Main player picked up item: " + item.LocalizedName());
                RemoveAllRelatedLootItems(item, Controllers.LootDestroyerController.LooseLootInfo);
                RemoveAllRelatedLootItems(item, Controllers.LootDestroyerController.StaticLootInfo);
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
                // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
                foreach (Item relevantItem in Controllers.LootDestroyerController.FindAllItemsInContainer(item).Append(item))
                {
                    Logger.LogInfo("Main player removed item: " + item.LocalizedName() + " (Spawned in session: " + item.SpawnedInSession + ")");
                    Controllers.LootDestroyerController.ItemsDroppedByMainPlayer.Add(relevantItem);
                }
            }
        }

        private static void RemoveAllRelatedLootItems(Item item, Dictionary<Item, LootInfo> lootDict)
        {
            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in Controllers.LootDestroyerController.FindAllItemsInContainer(item).Append(item))
            {
                if (lootDict.Any(i => i.Key.Id == relevantItem.Id))
                {
                    Logger.LogInfo("Removing item from loot list: " + relevantItem.LocalizedName());
                    lootDict.Remove(relevantItem);
                }
            }
        }
    }
}
