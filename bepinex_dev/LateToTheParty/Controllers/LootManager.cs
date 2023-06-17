using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using LateToTheParty.CoroutineExtensions;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.Controllers
{
    public static class LootManager
    {
        public static bool IsFindingAndDestroyingLoot { get; private set; } = false;

        private static List<LootableContainer> AllLootableContainers = new List<LootableContainer>();
        private static object lootableContainerLock = new object();

        private static Dictionary<Item, Models.LootInfo> LootInfo = new Dictionary<Item, Models.LootInfo>();
        private static List<Item> ItemsDroppedByMainPlayer = new List<Item>();
        private static string[] secureContainerIDs = new string[0];
        private static Stopwatch lastLootDestroyedTimer = Stopwatch.StartNew();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
        private static string currentLocationName = "";

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
            if (IsFindingAndDestroyingLoot)
            {
                enumeratorWithTimeLimit.Abort();
                TaskWithTimeLimit.WaitForCondition(() => !IsFindingAndDestroyingLoot);
            }

            if (ConfigController.Config.Debug && (LootInfo.Count > 0))
            {
                WriteLootLogFile();
            }

            PathRender.Clear();

            lock (lootableContainerLock)
            {
                AllLootableContainers.Clear();
            }

            LootInfo.Clear();
            ItemsDroppedByMainPlayer.Clear();

            currentLocationName = "";

            lastLootDestroyedTimer.Restart();
        }

        public static int FindAllLootableContainers(string _currentMapName)
        {
            LoggingController.LogInfo("Searching for lootable containers in the map...");
            AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>().ToList();
            LoggingController.LogInfo("Searching for lootable containers in the map...found " + LootableContainerCount + " lootable containers.");

            currentLocationName = _currentMapName;

            return LootableContainerCount;
        }

        public static void AddLootableContainer(LootableContainer container)
        {
            lock (lootableContainerLock)
            {
                LoggingController.LogInfo("Including container " + container.name + " when searching for loot.");
                AllLootableContainers.Add(container);
            }
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

                    NavMeshController.RemoveAccessibilityPaths(GetLootPathName(relevantItem));
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
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allLootItems, ProcessFoundLooseLootItem, firstLootSearch ? 0 : raidET);

                // Search all lootable containers for loot
                enumeratorWithTimeLimit.Reset();
                lock (lootableContainerLock)
                {
                    yield return enumeratorWithTimeLimit.Run(AllLootableContainers, ProcessStaticLootContainer, firstLootSearch ? 0 : raidET);
                }

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
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(LootInfo.Keys.ToArray(), UpdateLootEligibility, yourPosition, raidET);

                // Check which items are accessible
                IEnumerable<KeyValuePair<Item,Models.LootInfo>> remainingItems = LootInfo.Where(l => !l.Value.IsDestroyed);
                Item[] inaccessibleItems = remainingItems.Where(l => !l.Value.NavData.IsAccessible).Select(l => l.Key).ToArray();
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(inaccessibleItems, UpdateLootAccessibility);

                double percentAccessible = Math.Round(100.0 * remainingItems.Where(i => i.Value.NavData.IsAccessible).Count() / remainingItems.Count(), 1);
                LoggingController.LogInfo(percentAccessible + "% of " + remainingItems.Count() + " items are accessible.");

                // Sort eligible loot
                IEnumerable <KeyValuePair<Item, Models.LootInfo>> eligibleItems = LootInfo.Where(l => l.Value.CanDestroy);
                Item[] sortedLoot = SortLoot(eligibleItems).Select(i => i.Key).ToArray();

                // Identify items to destroy
                List<Item> itemsToDestroy = new List<Item>();
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(sortedLoot, FindItemsToDestroy, lootItemsToDestroy, itemsToDestroy);

                // Destroy items
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(itemsToDestroy, DestroyLoot, raidET);

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
                    Models.LootInfo newLoot = new Models.LootInfo(
                            Models.ELootType.Loose,
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
                        Models.LootInfo newLoot = new Models.LootInfo(
                            Models.ELootType.Static,
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

        private static IEnumerable<KeyValuePair<Item, Models.LootInfo>> SortLoot(IEnumerable<KeyValuePair<Item, Models.LootInfo>> loot)
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

        private static void UpdateLootAccessibility(Item item)
        {
            Vector3 itemPosition = LootInfo[item].Transform.position;

            float distanceToNearestLockedDoor = NavMeshController.GetDistanceToNearestLockedDoor(itemPosition);
            if ((distanceToNearestLockedDoor < float.MaxValue) && (distanceToNearestLockedDoor > 25))
            {
                LootInfo[item].NavData.IsAccessible = true;
                return;
            }

            Player nearestPlayer = NavMeshController.GetNearestPlayer(itemPosition);
            LootInfo[item].NavData.AccessibleFromPosition = nearestPlayer.Transform.position;
            bool isAccessible = NavMeshController.IsPositionAccessible(nearestPlayer.Transform.position, itemPosition, GetLootPathName(item));
            LootInfo[item].NavData.IsAccessible = isAccessible;
        }

        private static string GetLootPathName(Item item)
        {
            return item.LocalizedName() + "_" + item.Id;
        }

        private static void FindItemsToDestroy(Item item, int totalItemsToDestroy, List<Item> allItemsToDestroy)
        {
            // Do not search for more items if enough have already been identified
            if (allItemsToDestroy.Count >= totalItemsToDestroy)
            {
                return;
            }

            // Make sure the item isn't already in the queue to be destroyed
            if (allItemsToDestroy.Contains(item))
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

            // Check if the item cannot be removed from its parent
            Item[] allItems;
            if (CanRemoveItemFromParent(item, parentItem))
            {
                allItems = item.GetAllItems().Reverse().ToArray();                
                if (allItems.Length > ConfigController.Config.DestroyLootDuringRaid.LootRanking.ChildItemLimits.Count)
                {
                    LoggingController.LogInfo(item.LocalizedName() + " has too many child items to destroy.");
                    return;
                }

                double allItemsWeight = allItems.Select(i => i.Weight).Sum();
                if ((allItems.Length > 1) && (allItemsWeight > ConfigController.Config.DestroyLootDuringRaid.LootRanking.ChildItemLimits.TotalWeight))
                {
                    LoggingController.LogInfo(item.LocalizedName() + " and its child items are too heavy to destroy.");
                    return;
                }

                AddItemsToDespawnList(allItems, item, allItemsToDestroy);
                return;
            }
            LoggingController.LogInfo(item.LocalizedName() + " cannot be removed from " + parentItem.LocalizedName() + ". Destroying parent item and all children.");

            // Get all children of the parent item and add them to the despawn list
            allItems = parentItem.GetAllItems().Reverse().ToArray();
            AddItemsToDespawnList(allItems, parentItem, allItemsToDestroy);
        }

        private static bool CanRemoveItemFromParent(Item item, Item parentItem)
        {
            if (item.TemplateId == parentItem.TemplateId)
            {
                return true;
            }

            LootItemClass lootItemClass;
            if ((lootItemClass = (parentItem as LootItemClass)) == null)
            {
                return true;
            }

            foreach(Slot slot in lootItemClass.Slots)
            {
                /*if (!slot.Required)
                {
                    continue;
                }

                if (slot.Items.Contains(item))
                {
                    return false;
                }*/

                if (slot.RemoveItem(true).Failed)
                {
                    return false;
                }
            }

            return true;
        }

        private static int AddItemsToDespawnList(Item[] items, Item parentItem, List<Item> allItemsToDestroy)
        {
            int despawnCount = 0;
            foreach (Item item in items)
            {
                despawnCount += AddItemToDespawnList(item, parentItem, allItemsToDestroy) ? 1: 0;
            }
            return despawnCount;
        }

        private static bool AddItemToDespawnList(Item item, Item parentItem, List<Item> allItemsToDestroy)
        {
            if (allItemsToDestroy.Contains(item))
            {
                return false;
            }

            if (!LootInfo.ContainsKey(item))
            {
                LoggingController.LogWarning("Could not find entry for " + item.LocalizedName());
                return false;
            }

            if (item.CurrentAddress == null)
            {
                LoggingController.LogWarning("Invalid parent for " + item.LocalizedName());
                return false;
            }

            // Ensure child items are destroyed before parent items
            LootInfo[item].parentItem = parentItem;
            if ((item.Parent.Item != null) && allItemsToDestroy.Contains(item.Parent.Item))
            {
                allItemsToDestroy.Insert(allItemsToDestroy.IndexOf(item.Parent.Item), item);
            }
            else
            {
                allItemsToDestroy.Add(item);
            }

            return true;
        }

        private static void DestroyLoot(Item item, double raidET)
        {
            try
            {
                LootInfo[item].TraderController.DestroyItem(item);
                LootInfo[item].IsDestroyed = true;
                LootInfo[item].RaidETWhenDestroyed = raidET;
                lastLootDestroyedTimer.Restart();

                LoggingController.LogInfo(
                    "Destroyed " + LootInfo[item].LootType + " loot"
                    + (((LootInfo[item].parentItem != null) && (LootInfo[item].parentItem.TemplateId != item.TemplateId)) ? " in " + LootInfo[item].parentItem.LocalizedName() : "")
                    + (ConfigController.LootRanking.Items.ContainsKey(item.TemplateId) ? " (Value=" + ConfigController.LootRanking.Items[item.TemplateId].Value + ")" : "")
                    + ": " + item.LocalizedName()
                );

                NavMeshController.RemoveAccessibilityPaths(GetLootPathName(item));
            }
            catch (Exception ex)
            {
                LoggingController.LogError("Could not destroy " + item.LocalizedName());
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

        private static void WriteLootLogFile()
        {
            LoggingController.LogInfo("Writing loot log file...");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Item,Template ID,Value,Raid ET When Found,Raid ET When Destroyed");
            foreach(Item item in LootInfo.Keys)
            {
                sb.Append(item.LocalizedName() + ",");
                sb.Append(item.TemplateId + ",");
                sb.Append(ConfigController.LootRanking.Items[item.TemplateId].Value + ",");
                sb.Append(LootInfo[item].RaidETWhenFound + ",");
                sb.AppendLine(LootInfo[item].RaidETWhenDestroyed >= 0 ? LootInfo[item].RaidETWhenDestroyed.ToString() : "");
            }

            string filename = LoggingController.LoggingPath
                + "loot_"
                + currentLocationName.Replace(" ", "")
                + "_"
                + DateTime.Now.ToFileTimeUtc()
                + ".csv";

            try
            {
                if (!Directory.Exists(LoggingController.LoggingPath))
                {
                    Directory.CreateDirectory(LoggingController.LoggingPath);
                }

                File.WriteAllText(filename, sb.ToString());

                LoggingController.LogInfo("Writing loot log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LoggingController.LogError("Writing loot log file...failed!");
                LoggingController.LogError(e.ToString());
            }
        }
    }
}