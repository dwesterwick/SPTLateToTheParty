using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
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
        private static Stopwatch lastLootDestroyedTimer = Stopwatch.StartNew();

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

            lastLootDestroyedTimer.Restart();
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

                // Find all loose loot
                LootItem[] allLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
                EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
                yield return enumeratorWithTimeLimit.Run(allLootItems, ProcessFoundLooseLootItem, firstLootSearch ? 0 : raidET);

                // Search all lootable containers for loot
                enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
                yield return enumeratorWithTimeLimit.Run(AllLootableContainers, ProcessStaticLootContainer, firstLootSearch ? 0 : raidET);

                // Ensure there is still loot on the map
                if ((LootInfo.Count == 0) || LootInfo.All(l => l.Value.IsDestroyed))
                {
                    IsFindingAndDestroyingLoot = false;
                    yield break;
                }

                // Find amount of loot to destroy
                double targetLootRemainingFraction = LocationSettingsController.GetLootRemainingFactor(timeRemainingFraction);
                int lootItemsToDestroy = GetNumberOfLootItemsToDestroy(targetLootRemainingFraction);
                if ((lootItemsToDestroy == 0) && (lastLootDestroyedTimer.ElapsedMilliseconds >= ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot * 1000.0))
                {
                    LoggingController.LogInfo("Max time of " + ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot + "s elapsed since destroying loot. Forcing at least 1 item to be removed...");
                    lootItemsToDestroy = 1;
                }
                if (lootItemsToDestroy == 0)
                {
                    yield break;
                }

                // Determine which loot is eligible to destroy
                enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
                yield return enumeratorWithTimeLimit.Run(LootInfo.Keys.ToArray(), UpdateLootEligibility, yourPosition, raidET);

                // Sort eligible loot
                IEnumerable<KeyValuePair<Item, LootInfo>> eligibleItems = LootInfo.Where(l => l.Value.CanDestroy);
                Item[] sortedLoot = SortLoot(eligibleItems).Select(i => i.Key).ToArray();

                // Identify items to destroy
                List<Item> itemsToDestroy = new List<Item>();
                enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
                yield return enumeratorWithTimeLimit.Run(sortedLoot, FindItemsToDestroy, lootItemsToDestroy, itemsToDestroy);

                // Destroy items
                enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
                yield return enumeratorWithTimeLimit.Run(itemsToDestroy, DestroyLoot);

                itemsToDestroy.Clear();
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

        private static void UpdateLootEligibility(Item item, Vector3 yourPosition, double raidET)
        {
            LootInfo[item].CanDestroy = CanDestroyItem(item, yourPosition, raidET);
        }

        private static int GetNumberOfLootItemsToDestroy(double targetLootRemainingFraction)
        {
            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = (double)LootInfo.Values.Where(v => v.IsDestroyed == false).Count() / LootInfo.Count;
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //LoggingController.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + currentLootRemainingFraction);

            // Calculate the number of loot items to destroy
            int lootItemsToDestroy = (int)Math.Floor(Math.Max(0, lootFractionToDestroy) * LootInfo.Count);

            return lootItemsToDestroy;
        }

        private static IEnumerable<KeyValuePair<Item, LootInfo>> SortLoot(IEnumerable<KeyValuePair<Item, LootInfo>> loot)
        {
            System.Random randomGen = new System.Random();

            // Get the loot ranking data from the server, but this only needs to be done once
            if (ConfigController.LootRanking == null)
            {
                ConfigController.GetLootRankingData();
            }
            if (ConfigController.LootRanking == null)
            {
                LoggingController.LogError("Cannot read loot ranking data from the server.");
            }

            // If loot ranking is disabled, simply sort the loot randomly
            if ((!ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled) || (ConfigController.LootRanking == null))
            {
                return loot.OrderBy(i => randomGen.NextDouble());
            }

            // Determine how much randomness to apply to loot sorting
            double lootValueRange = ConfigController.LootRanking.Items.Max(i => i.Value.Value) - ConfigController.LootRanking.Items.Min(i => i.Value.Value);
            double lootValueRandomFactor = lootValueRange * ConfigController.Config.DestroyLootDuringRaid.LootRanking.Randomness / 100.0;

            // Return loot sorted by value but with randomness applied
            return loot.OrderByDescending(i => ConfigController.LootRanking.Items[i.Key.TemplateId].Value + randomGen.Range(-1, 1) * lootValueRandomFactor);
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

            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (LootInfo[item].Transform == null))
            {
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

        private static void FindItemsToDestroy(Item item, int totalItemsToDestroy, List<Item> allItemsToDestroy)
        {
            // Do not search for more items if enough have already been identified
            if (allItemsToDestroy.Count >= totalItemsToDestroy)
            {
                return;
            }

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
                if (allItemsToDestroy.Contains(containedItem))
                {
                    continue;
                }

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

                // Ensure child items are destroyed before parent items
                LootInfo[item].parentItem = parentItem;
                if (allItemsToDestroy.Contains(parentItem))
                {
                    allItemsToDestroy.Insert(allItemsToDestroy.IndexOf(parentItem) - 1, containedItem);
                }
                else
                {
                    allItemsToDestroy.Add(containedItem);
                }
            }
        }

        private static void DestroyLoot(Item item)
        {
            LoggingController.LogInfo(
                    "Destroying " + LootInfo[item].LootType + " loot"
                    + (((LootInfo[item].parentItem != null) && (LootInfo[item].parentItem.TemplateId != item.TemplateId)) ? " in " + LootInfo[item].parentItem.LocalizedName() : "")
                    + (ConfigController.LootRanking.Items.ContainsKey(item.TemplateId) ? " (Value=" + ConfigController.LootRanking.Items[item.TemplateId].Value + ")" : "")
                    + ": " + item.LocalizedName()
                );

            try
            {
                LootInfo[item].TraderController.DestroyItem(item);
                LootInfo[item].IsDestroyed = true;
                lastLootDestroyedTimer.Restart();
            }
            catch (Exception ex)
            {
                LoggingController.LogError("Could not destroy " + item);
                LoggingController.LogError(ex.ToString());
                LootInfo.Remove(item);
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