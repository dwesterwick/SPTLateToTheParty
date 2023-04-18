﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Models
{
    public static class LootManager
    {
        public static bool IsFindingAndDestroyingLoot { get; private set; } = false;

        private static List<LootableContainer> AllLootableContainers = new List<LootableContainer>();
        private static Dictionary<Item, LootInfo> LootInfo = new Dictionary<Item, LootInfo>();
        private static List<Item> ItemsDroppedByMainPlayer = new List<Item>();
        private static string[] secureContainerIDs = new string[0];

        public static int LootableContainerCount
        {
            get { return AllLootableContainers.Count; }
        }

        public static int TotalLootItemsCount
        {
            get { return LootInfo.Count; }
        }

        public static int RemainingLootItemsCount
        {
            get { return LootInfo.Where(l => !l.Value.IsDestroyed).Count(); }
        }

        public static void Clear()
        {
            AllLootableContainers.Clear();
            LootInfo.Clear();
            ItemsDroppedByMainPlayer.Clear();
        }

        public static int FindAllLootableContainers()
        {
            LoggingController.LogInfo("Searching for lootable containers in the map...");
            AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>().ToList();
            LoggingController.LogInfo("Searching for lootable containers in the map...found " + LootableContainerCount + " lootable containers.");

            return LootableContainerCount;
        }

        public static void RegisterItemDroppedByPlayer(Item item)
        {
            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in item.FindAllItemsInContainer(true))
            {
                if (!ItemsDroppedByMainPlayer.Contains(relevantItem))
                {
                    LoggingController.LogInfo("Preventing dropped item from despawning: " + relevantItem.LocalizedName());
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
                    LoggingController.LogInfo("Removing picked-up item from eligible loot: " + relevantItem.LocalizedName());
                    LootInfo.Remove(relevantItem);
                }
            }
        }

        public static IEnumerator FindAndDestroyLoot(Vector3 yourPosition, float timeRemainingFraction, double raidET)
        {
            try
            {
                IsFindingAndDestroyingLoot = true;

                // Check if this is the first time looking for loot in the map
                bool firstLootSearch = LootInfo.Count == 0;

                // Spread the work across multiple frames based on a maximum calculation time per frame
                EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);

                // Find all loose loot
                LootItem[] allLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
                yield return enumeratorWithTimeLimit.Run(allLootItems, ProcessFoundLooseLootItem, firstLootSearch ? 0 : raidET);

                // Search all lootable containers for loot
                yield return enumeratorWithTimeLimit.Run(AllLootableContainers, ProcessStaticLootContainer, firstLootSearch ? 0 : raidET);

                // Ensure there is still loot on the map
                if ((LootInfo.Count == 0) || LootInfo.All(l => l.Value.IsDestroyed))
                {
                    IsFindingAndDestroyingLoot = false;
                    yield break;
                }

                // Destroy loot based on target fraction remaining
                double targetLootRemainingFraction = LocationSettingsController.GetLootRemainingFactor(timeRemainingFraction);
                Item[] itemsToDestroy = FindLootToDestroy(yourPosition, targetLootRemainingFraction, raidET).ToArray();
                yield return enumeratorWithTimeLimit.Run(itemsToDestroy, DestroyLoot, raidET);
            }
            finally
            {
                IsFindingAndDestroyingLoot = false;
            }
        }

        private static void ProcessFoundLooseLootItem(LootItem lootItem, double raidET)
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
                    LootInfo newLoot = new LootInfo(
                            ELootType.Loose,
                            lootItem.ItemOwner,
                            lootItem.transform,
                            ItemHelpers.GetDistanceToNearestSpawnPoint(lootItem.transform.position),
                            GetLootFoundTime(raidET)
                    );
                    LootInfo.Add(item, newLoot);
                }
            }
        }

        private static void ProcessStaticLootContainer(LootableContainer lootableContainer, double raidET)
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
                        LootInfo newLoot = new LootInfo(
                            ELootType.Static,
                            lootableContainer.ItemOwner,
                            lootableContainer.transform,
                            ItemHelpers.GetDistanceToNearestSpawnPoint(lootableContainer.transform.position),
                            GetLootFoundTime(raidET)
                        );
                        LootInfo.Add(item, newLoot);
                    }
                }
            }
        }

        private static double GetLootFoundTime(double raidET)
        {
            return raidET == 0 ? -1.0 * ConfigController.Config.DestroyLootDuringRaid.MinLootAge : raidET;
        }

        private static IEnumerable<Item> FindLootToDestroy(Vector3 yourPosition, double targetLootRemainingFraction, double raidET)
        {
            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = (double)LootInfo.Values.Where(v => v.IsDestroyed == false).Count() / LootInfo.Count;
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //LoggingController.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + currentLootRemainingFraction);
            if (lootFractionToDestroy <= 0)
            {
                return Enumerable.Empty<Item>();
            }

            // Calculate the number of loot items to destroy
            int lootItemsToDestroy = (int)Math.Floor(lootFractionToDestroy * LootInfo.Count);
            if (lootItemsToDestroy == 0)
            {
                return Enumerable.Empty<Item>();
            }

            // Find all loot items eligible for destruction and randomly sort them
            System.Random randomGen = new System.Random();
            IEnumerable<KeyValuePair<Item, LootInfo>> eligibleItems = LootInfo.Where(l => CanDestroyItem(l.Key, yourPosition, raidET));
            IEnumerable<KeyValuePair<Item, LootInfo>> randomlySortedLoot = eligibleItems.OrderBy(e => randomGen.NextDouble());
            
            // Generate a list of loot to be destroyed. This needs to be iterated because each item in the loot dictionaries has an unknown number of child items in it. 
            int actualLootBeingDestroyed = 0;
            IEnumerable<KeyValuePair<Item, LootInfo>> lootToDestroy = Enumerable.Empty<KeyValuePair<Item, LootInfo>>();
            foreach (KeyValuePair<Item, LootInfo> lootInfo in randomlySortedLoot)
            {
                if (actualLootBeingDestroyed >= lootItemsToDestroy)
                {
                    break;
                }

                lootToDestroy = lootToDestroy.Append(lootInfo);
                actualLootBeingDestroyed += lootInfo.Key.ToEnumerable().FindAllRelatedItems().Count();
            }

            //LoggingController.LogInfo("Target loot to destroy: " + lootItemsToDestroy + ", Loot Being Destroyed: " + actualLootBeingDestroyed);

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

            // Ensure enough time has elapsed since the loot was first placed on the map (to prevent loot on dead bots from being destroyed too soon)
            double lootAge = raidET - LootInfo[item].RaidETWhenFound;
            if (lootAge < ConfigController.Config.DestroyLootDuringRaid.MinLootAge)
            {
                //LoggingController.LogInfo("Ignoring " + item.LocalizedName() + " (Loot age: " + lootAge + ")");
                return false;
            }

            // Ignore loot that's too close to you
            float lootDist = Vector3.Distance(yourPosition, LootInfo[item].Transform.position);
            if (lootDist < ConfigController.Config.DestroyLootDuringRaid.ExclusionRadius)
            {
                return false;
            }

            // Ignore loot that players couldn't have possibly reached yet
            double maxBotRunDistance = raidET * ConfigController.Config.DestroyLootDuringRaid.MapTraversalSpeed;
            if (maxBotRunDistance < LootInfo[item].DistanceToNearestSpawnPoint)
            {
                //LoggingController.LogInfo("Ignoring " + item.LocalizedName() + " (Loot Distance: " + LootInfo[item].DistanceToNearestSpawnPoint + ", Current Distance: " + maxBotRunDistance + ")");
                return false;
            }

            return true;
        }

        private static void DestroyLoot(Item item, double raidET)
        {
            // Find all parents of the item. Need to do this in case the item is (for example) a gun. If only the gun item is destroyed,
            // all of the mods, magazines, etc. on it will be orphaned and cause errors
            IEnumerable<Item> parentItems = item.ToEnumerable();
            try
            {
                IEnumerable<Item> _parentItems = item.GetAllParentItems();
                parentItems = parentItems.Concat(_parentItems);
            }
            catch (Exception)
            {
                LoggingController.LogError("Could not get parents of " + item.LocalizedName() + " (" + item.TemplateId + ")");
                throw;
            }

            // Remove all invalid items from the parent list (secure containers, fixed loot containers, etc.)
            try
            {
                parentItems = parentItems.RemoveExcludedItems();
            }
            catch (Exception)
            {
                LoggingController.LogError("Could not removed excluded items from " + string.Join(",", parentItems.Select(i => i.LocalizedName())));
                throw;
            }

            // Check if there aren't any items remaining after filtering
            if (parentItems.Count() == 0)
            {
                return;
            }

            // Get all child items of the parent item. The array needs to be reversed to prevent any of the items from becoming orphaned. 
            Item parentItem = parentItems.Last();
            Item[] allItems = parentItem.GetAllItems().Reverse().ToArray();
            foreach (Item containedItem in allItems)
            {
                if (!LootInfo.ContainsKey(containedItem))
                {
                    LoggingController.LogWarning("Could not find entry for " + containedItem.LocalizedName());
                    continue;
                }

                if (containedItem.CurrentAddress == null)
                {
                    LoggingController.LogWarning("Invalid parent for " + containedItem.LocalizedName());
                    continue;
                }

                LoggingController.LogInfo("Destroying " + LootInfo[item].LootType + " loot" + ((item.Id != containedItem.Id) ? " in " + parentItem.LocalizedName() + " (" + parentItem.TemplateId + ")" : "") + ": " + containedItem.LocalizedName());
                try
                {
                    LootInfo[containedItem].TraderController.DestroyItem(containedItem);
                    LootInfo[containedItem].IsDestroyed = true;
                }
                catch (Exception ex)
                {
                    LoggingController.LogError("Could not destroy " + containedItem);
                    LoggingController.LogError(ex.ToString());
                    LootInfo.Remove(containedItem);
                }
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

            IEnumerable<Item> filteredItems = items
                .Where(i => i.Template.Parent == null || !ConfigController.Config.DestroyLootDuringRaid.ExcludedParents.Any(p => i.Template.IsChildOf(p)))
                .Where(i => !ConfigController.Config.DestroyLootDuringRaid.ExcludedParents.Any(p => p == i.TemplateId))
                .Where(i => !secureContainerIDs.Contains(i.TemplateId));

            return filteredItems;
        }
    }
}
