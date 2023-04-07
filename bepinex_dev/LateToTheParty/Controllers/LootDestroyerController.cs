using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using EFT.UI;
using System.ComponentModel;

namespace LateToTheParty.Controllers
{
    public class LootDestroyerController : MonoBehaviour
    {
        public static BepInEx.Logging.ManualLogSource Logger { get; set; } = null;
        public static Configuration.ModConfig ModConfig { get; set; } = null;

        public static List<Item> ItemsDroppedByMainPlayer { get; set; } = new List<Item>();
        public static Dictionary<Item, LootInfo> LooseLootInfo = new Dictionary<Item, LootInfo>();
        public static Dictionary<Item, LootInfo> StaticLootInfo = new Dictionary<Item, LootInfo>();

        private static Vector3 lastUpdatePosition = Vector3.zero;
        private static List<string> secureContainerIDs = new List<string>();
        private static LootableContainer[] AllLootableContainers = new LootableContainer[0];        

        private void Update()
        {
            if (!ModConfig.DestroyLootDuringRaid.Enabled)
            {
                return;
            }

            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                ItemsDroppedByMainPlayer.Clear();
                AllLootableContainers = new LootableContainer[0];

                LooseLootInfo.Clear();
                StaticLootInfo.Clear();

                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            float escapeTimeSec = GClass1423.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float timeRemainingFraction = escapeTimeSec / (Patches.ReadyToPlayPatch.LastOriginalEscapeTime * 60f);
            if ((escapeTimeSec > 3600 * 24 * 90) || (timeRemainingFraction > 0.99))
            {
                return;
            }

            // Only run the script if you've traveled a minimum distance from the last update. Othewise, stuttering will occur. 
            // However, ignore this check initially so loot can be despawned at the very beginning of the raid before you start moving if you spawn in late
            Vector3 yourPosition = Camera.main.transform.position;
            float lastUpdateDist = Vector3.Distance(yourPosition, lastUpdatePosition);
            if ((lastUpdateDist < ModConfig.DestroyLootDuringRaid.MinDistanceTraveledForUpdate) && (StaticLootInfo.Count > 0))
            {
                return;
            }

            // This should only be run once to generate the list of secure container ID's
            if (secureContainerIDs.Count == 0)
            {
                Logger.LogInfo("Enumerating secure container ID's...");
                secureContainerIDs = GetSecureContainerIDs();
                Logger.LogInfo("Enumerating secure container ID's...found " + secureContainerIDs.Count + " secure containers.");
            }

            // This should only be run once to generate the list of lootable containers in the map
            if (AllLootableContainers.Length == 0)
            {
                Logger.LogInfo("Searching for lootable containers in the map...");
                AllLootableContainers = GameWorld.FindObjectsOfType<LootableContainer>();
                Logger.LogInfo("Searching for lootable containers in the map...found " + AllLootableContainers.Length + " lootable containers.");
            }

            //Stopwatch jobTimer = Stopwatch.StartNew();
            //Logger.LogInfo("Searching for new loot...");
            FindLooseLoot();
            FindStaticLoot();
            //Logger.LogInfo("Found " + LooseLootInfo.Count + " loose loot items (" + LooseLootInfo.Values.Where(v => v.IsDestroyed == false).Count() + " remaining)");
            //Logger.LogInfo("Found " + StaticLootInfo.Count + " static loot items (" + StaticLootInfo.Values.Where(v => v.IsDestroyed == false).Count() + " remaining)");

            //Logger.LogInfo("Destroying loot...");
            double targetLootRemainingFraction = Patches.ReadyToPlayPatch.GetLootRemainingFactor(timeRemainingFraction);
            DestroyLooseLoot(yourPosition, targetLootRemainingFraction);
            DestroyStaticLoot(yourPosition, targetLootRemainingFraction);

            lastUpdatePosition = yourPosition;
            //Logger.LogInfo("Destroying loot...done. (" + jobTimer.ElapsedMilliseconds + ")");
        }

        public static List<string> GetSecureContainerIDs()
        {
            List<string> secureContainerIDs = new List<string>();

            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            if (itemFactory == null)
            {
                return secureContainerIDs;
            }

            // Find all possible secure containers
            foreach (Item item in itemFactory.CreateAllItemsEver())
            {
                if (item.Template is SecureContainerTemplateClass)
                {
                    if ((item.Template as SecureContainerTemplateClass).isSecured)
                    {
                        secureContainerIDs.Add(item.TemplateId);
                    }
                }
            }

            return secureContainerIDs;
        }

        public static IEnumerable<Item> FindAllItemsInContainer(Item container)
        {
            IEnumerable<Item> containedItems = container.GetAllItems().Where(i => i.Id != container.Id);
            foreach (Item item in containedItems)
            {
                containedItems.Concat(FindAllItemsInContainer(item));
            }
            return containedItems.Distinct();
        }

        public static IEnumerable<Item> FindAllItemsInContainers(IEnumerable<Item> containers)
        {
            IEnumerable<Item> allItems = Enumerable.Empty<Item>();
            foreach (Item container in containers)
            {
                allItems = allItems.Concat(FindAllItemsInContainer(container));
            }
            return allItems.Distinct();
        }

        public static IEnumerable<Item> FindAllRelatedItems(IEnumerable<Item> items)
        {
            IEnumerable<Item> allItems = Enumerable.Empty<Item>();
            foreach (Item item in items)
            {
                Item parentItem = item.GetAllParentItemsAndSelf().Last();
                allItems = allItems.Concat(parentItem.GetAllItems().Reverse());
            }
            return allItems.Distinct();
        }

        private IEnumerable<Item> RemoveExcludedItems(IEnumerable<Item> items)
        {
            return items
                .Where(i => !ModConfig.DestroyLootDuringRaid.ExcludedParents.Any(p => i.Template.IsChildOf(p)))
                .Where(i => !ModConfig.DestroyLootDuringRaid.ExcludedParents.Any(p => p == i.TemplateId))
                .Where(i => !secureContainerIDs.Contains(i.TemplateId));
        }

        private IEnumerable<Item> RemoveItemsNotDroppedByPlayer(IEnumerable<Item> items)
        {
            return items.Where(i => !ItemsDroppedByMainPlayer.Contains(i));
        }

        public IEnumerable<Item> FindLootToDestroy(Dictionary<Item, LootInfo> lootInfo, double targetLootRemainingFraction)
        {
            // Calculate the fraction of loot that should be removed from the map
            double currentLootRemainingFraction = (double)lootInfo.Values.Where(v => v.IsDestroyed == false).Count() / lootInfo.Count;
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            //Logger.LogInfo("Target loot remaining: " + targetLootRemainingFraction + ", Current loot remaining: " + currentLootRemainingFraction);
            if (lootFractionToDestroy <= 0)
            {
                return Enumerable.Empty<Item>();
            }

            // Calculate the number of loot items to destrory and randomly sort the loot dictionary before creating an initial selection
            System.Random randomGen = new System.Random();
            int lootItemsToDestroy = (int)Math.Floor(lootFractionToDestroy * lootInfo.Count);
            int targetLootIndex = lootItemsToDestroy + 1;
            int actualLootBeingDestroyed = lootItemsToDestroy + 1;
            IEnumerable<KeyValuePair<Item, LootInfo>> randomlySortedLoot = lootInfo.OrderBy(e => randomGen.NextDouble());
            IEnumerable<KeyValuePair<Item, LootInfo>> lootToDestroy = randomlySortedLoot.Take(targetLootIndex);
            
            // Generate a list of loot to be destroyed. This needs to be iterated because each item in the loot dictionaries has an unknown number of child items in it. 
            while (actualLootBeingDestroyed > lootItemsToDestroy)
            {
                lootToDestroy = randomlySortedLoot.Take(--targetLootIndex);
                actualLootBeingDestroyed = FindAllRelatedItems(lootToDestroy.Where(l => !l.Value.IsDestroyed).Select(l => l.Key)).Count();
            }
            //Logger.LogInfo("Target loot to destroy: " + lootItemsToDestroy + ", Loot Being Destroyed: " + actualLootBeingDestroyed + ", Iterations: " + (lootItemsToDestroy - targetLootIndex));

            return lootToDestroy.Select(l => l.Key);
        }

        private void FindLooseLoot()
        {
            LootItem[] allLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
            foreach (LootItem lootItem in allLootItems)
            {
                // Ignore quest items like the bronze pocket watch for Checking
                if (lootItem.Item.QuestItem)
                {
                    continue;
                }

                // Find all items associated with lootItem that are eligible for despawning
                IEnumerable<Item> allItems = RemoveItemsNotDroppedByPlayer(RemoveExcludedItems(FindAllItemsInContainer(lootItem.Item).Append(lootItem.Item)));
                if (allItems.Count() == 0)
                {
                    continue;
                }

                foreach (Item item in allItems)
                {
                    if (!LooseLootInfo.ContainsKey(item))
                    {
                        LooseLootInfo.Add(item, new LootInfo(lootItem.ItemOwner, lootItem.transform));
                    }
                }
            }
        }

        private void DestroyLooseLoot(Vector3 yourPosition, double targetLootRemainingFraction)
        {
            if ((LooseLootInfo.Count == 0) || LooseLootInfo.All(l => l.Value.IsDestroyed))
            {
                return;
            }

            Item[] itemsToDestroy = FindLootToDestroy(LooseLootInfo, targetLootRemainingFraction).ToArray();
            foreach (Item item in itemsToDestroy)
            {
                if (LooseLootInfo[item].IsDestroyed)
                {
                    continue;
                }

                // Ignore loot that's too close to you
                float lootDist = Vector3.Distance(yourPosition, LooseLootInfo[item].Transform.position);
                if (lootDist < ModConfig.DestroyLootDuringRaid.ExclusionRadius)
                {
                    continue;
                }

                // Find all parents of the item. Need to do this in case the item is (for example) a gun. If only the gun item is destroyed,
                // all of the mods, magazines, etc. on it will be orphaned and cause errors
                IEnumerable<Item> parentItems = RemoveExcludedItems(item.GetAllParentItemsAndSelf());
                if (parentItems.Count() == 0)
                {
                    continue;
                }

                // Get all child items of the parent item. The array needs to be reversed to prevent any of the items from becoming orphaned. 
                Item parentItem = parentItems.Last();
                Item[] allItems = parentItem.GetAllItems().Reverse().ToArray();
                foreach (Item containedItem in allItems)
                {
                    if (!LooseLootInfo.ContainsKey(containedItem))
                    {
                        Logger.LogWarning("Could not find entry for " + containedItem.LocalizedName());
                        continue;
                    }

                    Logger.LogInfo("Destroying loose loot" + ((item.Id != containedItem.Id) ? " in " + parentItem.LocalizedName() + " (" + parentItem.TemplateId + ")" : "") + ": " + containedItem.LocalizedName());
                    LooseLootInfo[item].TraderController.DestroyItem(containedItem);
                    LooseLootInfo[containedItem].IsDestroyed = true;
                }
            }
        }

        private void FindStaticLoot()
        {
            foreach (LootableContainer lootableContainer in AllLootableContainers)
            {
                if (lootableContainer.ItemOwner == null)
                {
                    continue;
                }

                // NOTE: This level is for containers like weapon boxes, not like backpacks
                foreach (Item containerItem in lootableContainer.ItemOwner.Items)
                {
                    foreach(Item item in RemoveItemsNotDroppedByPlayer(FindAllItemsInContainer(containerItem)))
                    {
                        if (!StaticLootInfo.ContainsKey(item))
                        {
                            StaticLootInfo.Add(item, new LootInfo(lootableContainer.ItemOwner, lootableContainer.transform));
                        }
                    }
                }
            }
        }

        private void DestroyStaticLoot(Vector3 yourPosition, double targetLootRemainingFraction)
        {
            if ((StaticLootInfo.Count == 0) || StaticLootInfo.All(l => l.Value.IsDestroyed))
            {
                return;
            }

            Item[] itemsToDestroy = FindLootToDestroy(StaticLootInfo, targetLootRemainingFraction).ToArray();
            foreach (Item item in itemsToDestroy)
            {
                if (StaticLootInfo[item].IsDestroyed)
                {
                    continue;
                }

                // Ignore loot that's too close to you
                float lootDist = Vector3.Distance(yourPosition, StaticLootInfo[item].Transform.position);
                if (lootDist < ModConfig.DestroyLootDuringRaid.ExclusionRadius)
                {
                    continue;
                }

                // Need to find the parent item, but not the lootable container itself (thus TakeLast(2).First(), not Last())
                Item parentItem = item.GetAllParentItemsAndSelf().TakeLast(2).First();

                // Get all child items of the parent item. The array needs to be reversed to prevent any of the items from becoming orphaned. 
                Item[] allItems = parentItem.GetAllItems().Reverse().ToArray();
                foreach (Item containedItem in allItems)
                {
                    if (!StaticLootInfo.ContainsKey(containedItem))
                    {
                        Logger.LogWarning("Could not find entry for " + containedItem.LocalizedName());
                        continue;
                    }

                    Logger.LogInfo("Destroying static loot" + ((parentItem.Id != containedItem.Id) ? " in " + parentItem.LocalizedName() : "") + ": " + containedItem.LocalizedName());
                    StaticLootInfo[item].TraderController.DestroyItem(containedItem);
                    StaticLootInfo[containedItem].IsDestroyed = true;
                }
            }
        }
    }
}
