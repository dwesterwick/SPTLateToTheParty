using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Models
{
    public static class LootManager
    {
        public static bool IsFindingAndDestroyingLoot { get; private set; } = false;

        public static List<LootableContainer> AllLootableContainers = new List<LootableContainer>();
        public static Dictionary<Item, LootInfo> LootInfo = new Dictionary<Item, LootInfo>();
        public static List<Item> ItemsDroppedByMainPlayer { get; set; } = new List<Item>();

        private static string[] secureContainerIDs = new string[0];

        public static void RegisterItemDroppedByPlayer(Item item)
        {
            if (!ItemsDroppedByMainPlayer.Contains(item))
            {
                // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
                foreach (Item relevantItem in item.FindAllItemsInContainer(true))
                {
                    LateToThePartyPlugin.Log.LogInfo("Main player removed item: " + item.LocalizedName() + " (Spawned in session: " + item.SpawnedInSession + ")");
                    ItemsDroppedByMainPlayer.Add(relevantItem);
                }
            }
        }

        public static void RegisterItemPickedUpByPlayer(Item item)
        {
            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in item.FindAllItemsInContainer(true))
            {
                if (LootInfo.Any(i => i.Key.Id == relevantItem.Id))
                {
                    LateToThePartyPlugin.Log.LogInfo("Removing item from loot list: " + relevantItem.LocalizedName());
                    LootInfo.Remove(relevantItem);
                }
            }
        }

        public static IEnumerator FindAndDestroyLoot(Vector3 yourPosition, float timeRemainingFraction, double raidET)
        {
            IsFindingAndDestroyingLoot = true;

            EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.MaxCalcTimePerFrame);

            LootItem[] allLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
            
            yield return enumeratorWithTimeLimit.Run(allLootItems, ProcessFoundLooseLootItem);
            yield return enumeratorWithTimeLimit.Run(AllLootableContainers, ProcessStaticLootContainer);

            if ((LootInfo.Count == 0) || LootInfo.All(l => l.Value.IsDestroyed))
            {
                IsFindingAndDestroyingLoot = false;
                yield break;
            }

            double targetLootRemainingFraction = Patches.ReadyToPlayPatch.GetLootRemainingFactor(timeRemainingFraction);
            Item[] itemsToDestroy = FindLootToDestroy(yourPosition, targetLootRemainingFraction, raidET).ToArray();
            yield return enumeratorWithTimeLimit.Run(itemsToDestroy, DestroyLoot);

            IsFindingAndDestroyingLoot = false;
        }

        private static void ProcessFoundLooseLootItem(LootItem lootItem)
        {
            // Ignore quest items like the bronze pocket watch for "Checking"
            if (lootItem.Item.QuestItem)
            {
                return;
            }

            // Find all items associated with lootItem that are eligible for despawning
            IEnumerable<Item> allItems = lootItem.Item.FindAllItemsInContainer(true).RemoveExcludedItems().RemoveItemsDroppedByPlayer();
            foreach (Item item in allItems)
            {
                if (!LootInfo.ContainsKey(item))
                {
                    LootInfo.Add(item, new LootInfo(ELootType.Loose, lootItem.ItemOwner, lootItem.transform, ItemHelpers.GetDistanceToNearestSpawnPoint(lootItem.transform.position)));
                }
            }
        }

        private static void ProcessStaticLootContainer(LootableContainer lootableContainer)
        {
            if (lootableContainer.ItemOwner == null)
            {
                return;
            }

            // NOTE: This level is for containers like weapon boxes, not like backpacks
            foreach (Item containerItem in lootableContainer.ItemOwner.Items)
            {
                foreach (Item item in containerItem.FindAllItemsInContainer().RemoveItemsDroppedByPlayer())
                {
                    if (!LootInfo.ContainsKey(item))
                    {
                        LootInfo.Add(item, new LootInfo(ELootType.Static, lootableContainer.ItemOwner, lootableContainer.transform, ItemHelpers.GetDistanceToNearestSpawnPoint(lootableContainer.transform.position)));
                    }
                }
            }
        }

        private static IEnumerable<Item> FindLootToDestroy(Vector3 yourPosition, double targetLootRemainingFraction, double raidET)
        {
            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = (double)LootInfo.Values.Where(v => v.IsDestroyed == false).Count() / LootInfo.Count;
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //Logger.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + currentLootRemainingFraction);
            if (lootFractionToDestroy <= 0)
            {
                return Enumerable.Empty<Item>();
            }

            // Calculate the number of loot items to destroy
            System.Random randomGen = new System.Random();
            int lootItemsToDestroy = (int)Math.Floor(lootFractionToDestroy * LootInfo.Count);
            
            // Find all loot items eligible for destruction and randomly sort them
            IEnumerable<KeyValuePair<Item, LootInfo>> eligibleItems = LootInfo.Where(l => CanDestroyItem(l.Key, yourPosition, raidET));
            IEnumerable<KeyValuePair<Item, LootInfo>> randomlySortedLoot = eligibleItems.OrderBy(e => randomGen.NextDouble());
            
            // Generate a list of loot to be destroyed. This needs to be iterated because each item in the loot dictionaries has an unknown number of child items in it. 
            int actualLootBeingDestroyed = 0;
            IEnumerable<KeyValuePair<Item, LootInfo>> lootToDestroy = Enumerable.Empty<KeyValuePair<Item, LootInfo>>();
            foreach (KeyValuePair<Item, LootInfo> lootInfo in randomlySortedLoot)
            {
                lootToDestroy.AddItem(lootInfo);
                actualLootBeingDestroyed += lootInfo.Key.ToEnumerable().FindAllRelatedItems().Count();

                if (actualLootBeingDestroyed >= lootItemsToDestroy)
                {
                    break;
                }
            }

            //Logger.LogInfo("Target loot to destroy: " + lootItemsToDestroy + ", Loot Being Destroyed: " + actualLootBeingDestroyed + ", Iterations: " + (lootItemsToDestroy - targetLootIndex));

            return lootToDestroy.Select(l => l.Key);
        }

        private static bool CanDestroyItem(this Item item, Vector3 yourPosition, double raidET)
        {
            if (!LootInfo.ContainsKey(item))
            {
                return false;
            }

            if (LootInfo[item].IsDestroyed)
            {
                return false;
            }

            // Ignore loot that's too close to you
            float lootDist = Vector3.Distance(yourPosition, LootInfo[item].Transform.position);
            if (lootDist < LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.ExclusionRadius)
            {
                return false;
            }

            // Ignore loot that players couldn't have possibly reached yet
            double maxBotRunDistance = raidET * LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.MapTraversalSpeed;
            if (maxBotRunDistance < LootInfo[item].DistanceToNearestSpawnPoint)
            {
                LateToThePartyPlugin.Log.LogInfo("Ignoring " + item.LocalizedName() + " (Loot Distance: " + LootInfo[item].DistanceToNearestSpawnPoint + ", Current Distance: " + maxBotRunDistance + ")");
                return false;
            }

            return true;
        }

        private static void DestroyLoot(Item item)
        {
            // Find all parents of the item. Need to do this in case the item is (for example) a gun. If only the gun item is destroyed,
            // all of the mods, magazines, etc. on it will be orphaned and cause errors
            IEnumerable<Item> parentItems = item.GetAllParentItemsAndSelf().RemoveExcludedItems();
            if (parentItems.Count() == 0)
            {
                LateToThePartyPlugin.Log.LogWarning("Could not find valid parent for " + item.LocalizedName());
                return;
            }

            // Get all child items of the parent item. The array needs to be reversed to prevent any of the items from becoming orphaned. 
            Item parentItem = parentItems.Last();
            Item[] allItems = parentItem.GetAllItems().Reverse().ToArray();
            foreach (Item containedItem in allItems)
            {
                if (!LootInfo.ContainsKey(containedItem))
                {
                    LateToThePartyPlugin.Log.LogWarning("Could not find entry for " + containedItem.LocalizedName());
                    continue;
                }

                LateToThePartyPlugin.Log.LogInfo("Destroying " + LootInfo[item].LootType + " loot" + ((item.Id != containedItem.Id) ? " in " + parentItem.LocalizedName() + " (" + parentItem.TemplateId + ")" : "") + ": " + containedItem.LocalizedName());
                LootInfo[item].TraderController.DestroyItem(containedItem);
                LootInfo[containedItem].IsDestroyed = true;
            }
        }

        private static IEnumerable<Item> RemoveItemsDroppedByPlayer(this IEnumerable<Item> items)
        {
            return items.Where(i => !ItemsDroppedByMainPlayer.Contains(i));
        }

        private static IEnumerable<Item> RemoveExcludedItems(this IEnumerable<Item> items)
        {
            // This should only be run once to generate the array of secure container ID's
            if (secureContainerIDs.Length == 0)
            {
                secureContainerIDs = ItemHelpers.GetSecureContainerIDs().ToArray();
            }

            return items
                .Where(i => !LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.ExcludedParents.Any(p => i.Template.IsChildOf(p)))
                .Where(i => !LateToThePartyPlugin.ModConfig.DestroyLootDuringRaid.ExcludedParents.Any(p => p == i.TemplateId))
                .Where(i => !secureContainerIDs.Contains(i.TemplateId));
        }
    }
}
