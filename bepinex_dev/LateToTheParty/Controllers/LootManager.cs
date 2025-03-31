using System;
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
using LateToTheParty.Helpers.Loot;
using LateToTheParty.Models.LootInfo;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class LootManager
    {
        public bool IsFindingAndDestroyingLoot { get; private set; } = false;
        public bool HasInitialLootBeenDestroyed { get; private set; } = false;

        private List<LootableContainer> AllLootableContainers = new List<LootableContainer>();
        private object lootableContainerLock = new object();

        private Dictionary<Item, Models.LootInfo.AbstractLootInfo> LootInfo = new Dictionary<Item, Models.LootInfo.AbstractLootInfo>();
        private List<Item> ItemsDroppedByMainPlayer = new List<Item>();
        private Stopwatch lastLootDestroyedTimer = Stopwatch.StartNew();
        private EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.DestroyLootDuringRaid.MaxCalcTimePerFrame);
        private int destroyedLootSlots = 0;

        public int LootableContainerCount => AllLootableContainers.Count;
        public int TotalLootItemsCount => LootInfo.Count;
        public int RemainingLootItemsCount => LootInfo.Where(l => !l.Value.IsDestroyed && !l.Value.IsInPlayerInventory).Count();

        public bool WasDroppedByPlayer(Item item) => ItemsDroppedByMainPlayer.Contains(item);
        public void WriteLootLogFile(string locationName) => LoggingController.WriteLootLogFile(LootInfo, locationName);

        public LootManager()
        {
            
        }

        public AbstractLootInfo FindLootInfo(Item item)
        {
            if (!LootInfo.ContainsKey(item))
            {
                return null;
            }

            return LootInfo[item];
        }

        public Item FindItem(AbstractLootInfo abstractLootInfo)
        {
            if (!LootInfo.ContainsValue(abstractLootInfo))
            {
                return null;
            }

            return LootInfo.First(i => i.Value == abstractLootInfo).Key;
        }

        public void AddLootInfo(Item item, AbstractLootInfo lootInfo)
        {
            if (LootInfo.ContainsKey(item))
            {
                throw new InvalidOperationException("An entry already exists for item " + item.Id);
            }

            LootInfo.Add(item, lootInfo);
            //LoggingController.LogInfo("Found loot item: " + item.LocalizedName());
        }

        public int FindAllLootableContainers()
        {
            LoggingController.LogInfo("Searching for lootable containers in the map...");
            AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>().ToList();
            LoggingController.LogInfo("Searching for lootable containers in the map...found " + LootableContainerCount + " lootable containers.");

            return LootableContainerCount;
        }

        public void AddLootableContainer(LootableContainer container)
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

        public void RegisterItemDroppedByPlayer(Item item, bool preventFromDespawning = false)
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

        public void RegisterItemPickedUpByPlayer(Item item)
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

        public IEnumerator FindAndDestroyLoot(IEnumerable<Vector3> playerPositions, float timeRemainingFraction, double raidET)
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
                    //setInitialLootDestroyed();
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
                    //LoggingController.LogInfo("Limiting the number of items to destroy to " + ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items);
                    lootItemsToDestroy = ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Items;
                }
                if ((lootItemsToDestroy == 0) && (lastLootDestroyedTimer.ElapsedMilliseconds >= ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot * 1000.0))
                {
                    LoggingController.LogInfo("Max time of " + ConfigController.Config.DestroyLootDuringRaid.MaxTimeWithoutDestroyingAnyLoot + "s elapsed since destroying loot. Forcing at least 1 item to be removed...");
                    lootItemsToDestroy = 1;
                }
                if (lootItemsToDestroy == 0)
                {
                    setInitialLootDestroyed();
                    yield break;
                }

                // Find amount of loot slots to destroy
                int targetTotalLootSlotsDestroyed = LocationSettingsController.GetTargetLootSlotsDestroyed(timeRemainingFraction);
                int targetLootSlotsToDestroy = targetTotalLootSlotsDestroyed - GetTotalDestroyedSlots();
                if (targetLootSlotsToDestroy > ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots)
                {
                    //LoggingController.LogInfo("Limiting the number of item slots to destroy to " + ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots);
                    targetLootSlotsToDestroy = ConfigController.Config.DestroyLootDuringRaid.DestructionEventLimits.Slots;
                }
                if (targetLootSlotsToDestroy <= 0)
                {
                    setInitialLootDestroyed();
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
                else
                {
                    //setInitialLootDestroyed();
                }

                // Destroy items
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(itemsToDestroy, LootDestructionHelpers.StartDestruction);

                itemsToDestroy.Clear();
            }
            finally
            {
                IsFindingAndDestroyingLoot = false;
            }
        }

        private void setInitialLootDestroyed()
        {
            if (HasInitialLootBeenDestroyed)
            {
                return;
            }

            LoggingController.LogInfo("Initial loot has been destroyed");

            HasInitialLootBeenDestroyed = true;
        }

        private int GetNumberOfLootItemsToDestroy(double targetLootRemainingFraction)
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

        private double GetCurrentLootRemainingFraction()
        {
            IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> accessibleItems = LootInfo.Where(l => l.Value.PathData.IsAccessible);
            IEnumerable<KeyValuePair<Item, Models.LootInfo.AbstractLootInfo>> remainingItems = accessibleItems
                .Where(v => !v.Value.IsDestroyed)
                .Where(v => !v.Value.IsInPlayerInventory)
                .Where(v => !ItemsDroppedByMainPlayer.Contains(v.Key));

            return (double)remainingItems.Count() / accessibleItems.Count();
        }

        private int GetTotalDestroyedSlots()
        {
            IEnumerable<Item> collectedItems = LootInfo
                .Where(i => i.Value.IsInPlayerInventory)
                .Select(i => i.Key)
                .Concat(ItemsDroppedByMainPlayer);

            return destroyedLootSlots + collectedItems.Select(i => i.GetItemSlots()).Count();
        }

        public void ConfirmItemDestruction(Item item)
        {
            AbstractLootInfo lootInfo = FindLootInfo(item);
            if (lootInfo == null)
            {
                throw new InvalidOperationException("Loot has not been found");
            }

            float raidTimeElapsed = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();

            lootInfo.IsDestroyed = true;
            lootInfo.RaidETWhenDestroyed = raidTimeElapsed;
            lastLootDestroyedTimer.Restart();
            destroyedLootSlots += item.GetItemSlots();

            string parentItemText = "";
            if ((lootInfo.ParentItem != null) && (lootInfo.ParentItem.TemplateId != item.TemplateId))
            {
                parentItemText = " in " + lootInfo.ParentItem.LocalizedName();
            }

            string lootValueText = "";
            if (ConfigController.Config.DestroyLootDuringRaid.LootRanking.Enabled && ConfigController.LootRanking.Items.ContainsKey(item.TemplateId))
            {
                lootValueText = " (Value=" + ConfigController.LootRanking.Items[item.TemplateId].Value + ")";
            }

            LoggingController.LogInfo("Destroyed " + lootInfo.LootTypeName + parentItemText + lootValueText + ": " + item.LocalizedName());

            lootInfo.PathData.Clear();
        }
    }
}