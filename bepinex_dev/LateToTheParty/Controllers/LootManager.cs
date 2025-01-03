﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Helpers;
using LateToTheParty.Models.LootInfo;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public static class LootManager
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsFindingAndDestroyingLoot { get; private set; } = false;
        public static bool HasInitialLootBeenDestroyed { get; private set; } = false;

        private static List<LootableContainer> AllLootableContainers = new List<LootableContainer>();
        private static object lootableContainerLock = new object();

        private static Dictionary<Item, Models.LootInfo.AbstractLootInfo> LootInfo = new Dictionary<Item, Models.LootInfo.AbstractLootInfo>();
        private static List<Item> ItemsDroppedByMainPlayer = new List<Item>();
        private static Stopwatch lastLootDestroyedTimer = Stopwatch.StartNew();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
        private static string currentLocationName = "";
        private static int destroyedLootSlots = 0;

        public static int LootableContainerCount => AllLootableContainers.Count;
        public static int TotalLootItemsCount => LootInfo.Count;
        public static int RemainingLootItemsCount => LootInfo.Where(l => !l.Value.IsDestroyed && !l.Value.IsInPlayerInventory).Count();

        public static bool WasDroppedByPlayer(this Item item) => ItemsDroppedByMainPlayer.Contains(item);

        public static IEnumerator Clear()
        {
            if (IsFindingAndDestroyingLoot)
            {
                enumeratorWithTimeLimit.Abort();

                EnumeratorWithTimeLimit conditionWaiter = new EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsFindingAndDestroyingLoot, nameof(IsFindingAndDestroyingLoot), 3000);

                IsFindingAndDestroyingLoot = false;
            }

            if (ConfigController.Config.Debug.Enabled && (LootInfo.Count > 0))
            {
                LoggingController.WriteLootLogFile(LootInfo, currentLocationName);
            }

            Components.PathRender.Clear();

            lock (lootableContainerLock)
            {
                AllLootableContainers.Clear();
            }

            LootInfo.Clear();
            ItemsDroppedByMainPlayer.Clear();

            HasInitialLootBeenDestroyed = false;
            currentLocationName = "";
            destroyedLootSlots = 0;

            LootRankingHelpers.ResetLootValueRandomFactor();

            lastLootDestroyedTimer.Restart();
        }

        public static AbstractLootInfo FindLootInfo(this Item item)
        {
            if (!LootInfo.ContainsKey(item))
            {
                return null;
            }

            return LootInfo[item];
        }

        public static void AddLootInfo(Item item, AbstractLootInfo lootInfo)
        {
            if (LootInfo.ContainsKey(item))
            {
                throw new InvalidOperationException("An entry already exists for item " + item.Id);
            }

            LootInfo.Add(item, lootInfo);
            //LoggingController.LogInfo("Found loot item: " + item.LocalizedName());
        }

        public static int FindAllLootableContainers(string _currentMapName)
        {
            // Only run this once per map
            if (currentLocationName == _currentMapName)
            {
                return LootableContainerCount;
            }

            LoggingController.LogInfo("Searching for lootable containers in the map...");
            AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>().ToList();
            LoggingController.LogInfo("Searching for lootable containers in the map...found " + LootableContainerCount + " lootable containers.");

            currentLocationName = _currentMapName;

            return LootableContainerCount;
        }

        public static void AddLootableContainer(LootableContainer container)
        {
            if (AllLootableContainers.Contains(container))
            {
                LoggingController.LogWarning("Container " + container.name + " is already included when searching for loot.");
                return;
            }

            lock (lootableContainerLock)
            {
                LoggingController.LogInfo("Including container " + container.name + " when searching for loot.");
                AllLootableContainers.Add(container);
            }
        }

        public static void RegisterItemDroppedByPlayer(Item item, bool preventFromDespawning = false)
        {
            if (item == null)
            {
                LoggingController.LogError("Cannot register a null item dropped by a player or bot");
                return;
            }

            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in item.FindAllItemsInContainer(true))
            {
                if (LootInfo.ContainsKey(relevantItem))
                {
                    LootInfo[relevantItem].IsInPlayerInventory = false;
                    LootInfo[relevantItem].NearbyInteractiveObject = null;
                }

                if (preventFromDespawning && !ItemsDroppedByMainPlayer.Contains(relevantItem))
                {
                    LoggingController.LogInfo("Preventing dropped item from despawning: " + relevantItem.LocalizedName());
                    ItemsDroppedByMainPlayer.Add(relevantItem);
                }
            }
        }

        public static void RegisterItemPickedUpByPlayer(Item item)
        {
            if (item == null)
            {
                LoggingController.LogError("Cannot register a null item picked up by a player or bot");
                return;
            }

            // If the item is a container (i.e. a backpack), all of the items it contains also need to be added to the ignore list
            foreach (Item relevantItem in item.ToEnumerable().FindAllRelatedItems())
            {
                //LoggingController.LogInfo("Checking for picked-up item in eligible loot: " + relevantItem.LocalizedName());
                if (LootInfo.Any(i => i.Key.Id == relevantItem.Id))
                {
                    if (!LootInfo.ContainsKey(relevantItem))
                    {
                        LoggingController.LogError("Item " + relevantItem.Id + " (" + relevantItem.LocalizedName() + ") is not a discovered loot item. Cannot prevent it from despawning in your inventory!");
                        continue;
                    }

                    if (LootInfo[relevantItem].IsInPlayerInventory)
                    {
                        continue;
                    }

                    LoggingController.LogInfo("Removing picked-up item from eligible loot: " + relevantItem.LocalizedName());
                    LootInfo[relevantItem].IsInPlayerInventory = true;
                    LootInfo[relevantItem].NearbyInteractiveObject = null;
                    LootInfo[item].PathData.Clear();
                    LootInfo[relevantItem].PathData.Clear();
                }
            }
        }

        public static IEnumerator FindAndDestroyLoot(IEnumerable<Vector3> playerPositions, float timeRemainingFraction, double raidET)
        {
            try
            {
                IsFindingAndDestroyingLoot = true;

                // Check if this is the first time looking for loot in the map
                bool firstLootSearch = LootInfo.Count == 0;

                // Find all loose loot
                LootItem[] allLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allLootItems, LootDiscoveryHelpers.ProcessFoundLooseLootItem, firstLootSearch ? 0 : raidET);

                // Search all lootable containers for loot
                enumeratorWithTimeLimit.Reset();
                lock (lootableContainerLock)
                {
                    yield return enumeratorWithTimeLimit.Run(AllLootableContainers, LootDiscoveryHelpers.ProcessStaticLootContainer, firstLootSearch ? 0 : raidET);
                }

                // Ensure there is still loot on the map
                if ((LootInfo.Count == 0) || LootInfo.All(l => l.Value.IsDestroyed || l.Value.IsInPlayerInventory))
                {
                    yield break;
                }

                // After loot has initially been destroyed, limit the destruction rate
                double maxItemsToDestroy = 99999;
                if (HasInitialLootBeenDestroyed)
                {
                    maxItemsToDestroy = Math.Floor(ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Rate * lastLootDestroyedTimer.ElapsedMilliseconds / 1000.0);
                }

                // Find amount of loot to destroy
                double targetLootRemainingFraction = LocationSettingsController.GetLootRemainingFactor(timeRemainingFraction);
                int lootItemsToDestroy = (int)Math.Min(GetNumberOfLootItemsToDestroy(targetLootRemainingFraction), maxItemsToDestroy);
                if (lootItemsToDestroy > ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items)
                {
                    LoggingController.LogInfo("Limiting the number of items to destroy to " + ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items);
                    lootItemsToDestroy = ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items;
                }
                if ((lootItemsToDestroy == 0) && (lastLootDestroyedTimer.ElapsedMilliseconds >= ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot * 1000.0))
                {
                    LoggingController.LogInfo("Max time of " + ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot + "s elapsed since destroying loot. Forcing at least 1 item to be removed...");
                    lootItemsToDestroy = 1;
                }
                if (lootItemsToDestroy == 0)
                {
                    if (!HasInitialLootBeenDestroyed)
                    {
                        LoggingController.LogInfo("Initial loot has been destroyed");
                        HasInitialLootBeenDestroyed = true;
                    }

                    yield break;
                }

                // Find amount of loot slots to destroy
                int targetTotalLootSlotsDestroyed = LocationSettingsController.GetTargetLootSlotsDestroyed(timeRemainingFraction);
                int targetLootSlotsToDestroy = targetTotalLootSlotsDestroyed - GetTotalDestroyedSlots();
                if (targetLootSlotsToDestroy > ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots)
                {
                    LoggingController.LogInfo("Limiting the number of item slots to destroy to " + ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots);
                    targetLootSlotsToDestroy = ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots;
                }
                if (targetLootSlotsToDestroy <= 0)
                {
                    if (!HasInitialLootBeenDestroyed)
                    {
                        LoggingController.LogInfo("Initial loot has been destroyed");
                        HasInitialLootBeenDestroyed = true;
                    }

                    yield break;
                }

                // Enumerate loot that hasn't been destroyed and hasn't previously been deemed accessible
                IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> remainingItems = LootInfo.Where(l => !l.Value.IsDestroyed && !l.Value.IsInPlayerInventory);
                Item[] inaccessibleItems = remainingItems.Where(l => !l.Value.PathData.IsAccessible).Select(l => l.Key).ToArray();

                // Check which items are accessible
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(inaccessibleItems, LootAccessibilityHelpers.UpdateAccessibility);

                // Determine which loot is eligible to destroy
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(LootInfo.Keys.ToArray(), LootDestructionHelpers.UpdateLootEligibility, playerPositions, raidET);

                // Sort eligible loot
                IEnumerable <KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> eligibleItems = LootInfo
                    .Where(l => !l.Value.CannotBeDestroyed && l.Value.EligibleForDestruction && l.Value.PathData.IsAccessible)
                    .RemoveItemsWithoutValidTemplates();
                
                Item[] sortedLoot = eligibleItems.Sort().Select(i => i.Key).ToArray();

                // Identify items to destroy
                List<Item> itemsToDestroy = new List<Item>();
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(sortedLoot, LootDestructionHelpers.FindChildItemsToDestroy, lootItemsToDestroy, targetLootSlotsToDestroy, itemsToDestroy);
                
                // Show the percentage of accessible loot before destroying any of it
                if (itemsToDestroy.Count > 0)
                {
                    int slotsToDestroy = itemsToDestroy.Sum(i => i.GetItemSlots());
                    double percentAccessible = Math.Round(100.0 * remainingItems.Where(i => i.Value.PathData.IsAccessible).Count() / remainingItems.Count(), 1);

                    string slotsDestroyedText = "Destroying " + itemsToDestroy.Count + "/" + maxItemsToDestroy + " items filling " + slotsToDestroy + "/" + targetLootSlotsToDestroy + " slots";
                    string lootFractionDestroyedText = Math.Round(GetCurrentLootRemainingFraction()  * 100.0, 2) + "%/" + Math.Round(targetLootRemainingFraction * 100.0, 2) + "%";
                    string lootSlotsDestroyedText = GetTotalDestroyedSlots() + "/" + targetTotalLootSlotsDestroyed + " slots.";
                    LoggingController.LogInfo(percentAccessible + "% of " + remainingItems.Count() + " items are accessible. " + slotsDestroyedText + ". Loot remaining: " + lootFractionDestroyedText + ", " + lootSlotsDestroyedText);
                }

                // Destroy items
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(itemsToDestroy, LootDestructionHelpers.DestroyLoot);

                itemsToDestroy.Clear();
            }
            finally
            {
                IsFindingAndDestroyingLoot = false;
            }
        }

        private static int GetNumberOfLootItemsToDestroy(double targetLootRemainingFraction)
        {
            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = GetCurrentLootRemainingFraction();
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //LoggingController.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + GetCurrentLootRemainingFraction());

            // Calculate the number of loot items to destroy
            IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> accessibleItems = LootInfo.Where(l => l.Value.PathData.IsAccessible);
            int lootItemsToDestroy = (int)Math.Floor(Math.Max(0, lootFractionToDestroy) * accessibleItems.Count());

            return lootItemsToDestroy;
        }

        private static double GetCurrentLootRemainingFraction()
        {
            IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> accessibleItems = LootInfo.Where(l => l.Value.PathData.IsAccessible);
            IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> remainingItems = accessibleItems
                .Where(v => !v.Value.IsDestroyed)
                .Where(v => !v.Value.IsInPlayerInventory)
                .Where(v => !ItemsDroppedByMainPlayer.Contains(v.Key));

            return (double)remainingItems.Count() / accessibleItems.Count();
        }

        private static int GetTotalDestroyedSlots()
        {
            IEnumerable<Item> collectedItems = LootInfo
                .Where(i => i.Value.IsInPlayerInventory)
                .Select(i => i.Key)
                .Concat(ItemsDroppedByMainPlayer);

            return destroyedLootSlots + collectedItems.Select(i => i.GetItemSlots()).Count();
        }

        public static void ConfirmItemDestruction(Item item)
        {
            AbstractLootInfo lootInfo = item.FindLootInfo();
            if (lootInfo == null)
            {
                throw new InvalidOperationException("Loot has not been found");
            }

            float raidTimeElapsed = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();

            lootInfo.IsDestroyed = true;
            lootInfo.RaidETWhenDestroyed = raidTimeElapsed;
            lastLootDestroyedTimer.Restart();
            destroyedLootSlots += item.GetItemSlots();

            LoggingController.LogInfo(
                "Destroyed " + lootInfo.LootTypeName
                + (((lootInfo.ParentItem != null) && (lootInfo.ParentItem.TemplateId != item.TemplateId)) ? " in " + lootInfo.ParentItem.LocalizedName() : "")
                + (ConfigController.LootRanking.Items.ContainsKey(item.TemplateId) ? " (Value=" + ConfigController.LootRanking.Items[item.TemplateId].Value + ")" : "")
                + ": " + item.LocalizedName()
            );

            lootInfo.PathData.Clear();
        }
    }
}