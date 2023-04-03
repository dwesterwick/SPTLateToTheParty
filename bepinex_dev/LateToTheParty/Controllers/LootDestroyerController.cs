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
        public BepInEx.Logging.ManualLogSource Logger { get; set; } = null;
        public Configuration.ModConfig ModConfig { get; set; } = null;

        private float LootDestroyDist = 20;
        private float minUpdateDist = 1;
        private Vector3 lastUpdatePosition = Vector3.zero;

        private LootItem[] AllLootItems = new LootItem[0];
        private LootableContainer[] AllLootableContainers = new LootableContainer[0];

        private Dictionary<LootItem, bool> LooseLootDestroyed = new Dictionary<LootItem, bool>();
        private Dictionary<Item, bool> StaticLootDestroyed = new Dictionary<Item, bool>();
        private Dictionary<Item, TraderControllerClass> StaticLootController = new Dictionary<Item, TraderControllerClass>();
        private Dictionary<Item, Transform> StaticLootTransform = new Dictionary<Item, Transform>();
        private Dictionary<Item, Item[]> StaticLootChildItems = new Dictionary<Item, Item[]>();

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                AllLootableContainers = new LootableContainer[0];
                return;
            }

            if (AllLootableContainers.Length == 0)
            {
                AllLootableContainers = UnityEngine.Object.FindObjectsOfType<LootableContainer>();
            }

            float escapeTimeSec = GClass1423.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float timeRemainingFraction = escapeTimeSec / (Patches.ReadyToPlayPatch.LastOriginalEscapeTime * 60f);
            if (timeRemainingFraction == 1)
            {
                return;
            }

            Vector3 yourPosition = Camera.main.transform.position;
            float lastUpdateDist = Vector3.Distance(yourPosition, lastUpdatePosition);
            if ((lastUpdateDist < minUpdateDist) && (LooseLootDestroyed.Count > 0))
            {
                return;
            }

            Stopwatch jobTimer = Stopwatch.StartNew();
            Logger.LogInfo("Destroying loot...");            

            FindLooseLoot();
            FindStaticLoot();
            //Logger.LogInfo("Found " + LooseLootDestroyed.Count + " loose loot items (" + LooseLootDestroyed.Values.Where(v => v == false).Count() + " remaining)");
            //Logger.LogInfo("Found " + StaticLootDestroyed.Count + " static loot items (" + StaticLootDestroyed.Values.Where(v => v == false).Count() + " remaining)");

            double targetLootRemainingFraction = Patches.ReadyToPlayPatch.GetLootRemainingFactor(timeRemainingFraction);
            DestroyLooseLoot(yourPosition, targetLootRemainingFraction);
            DestroyStaticLoot(yourPosition, targetLootRemainingFraction);

            lastUpdatePosition = yourPosition;
            Logger.LogInfo("Destroying loot...done. (" + jobTimer.ElapsedMilliseconds + ")");
        }

        private void FindLooseLoot()
        {
            AllLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
            foreach (LootItem lootItem in AllLootItems)
            {
                if (lootItem.Item.QuestItem)
                {
                    continue;
                }

                if (!LooseLootDestroyed.ContainsKey(lootItem))
                {
                    LooseLootDestroyed.Add(lootItem, false);
                }
            }
        }

        private void DestroyLooseLoot(Vector3 yourPosition, double targetLootRemainingFraction)
        {
            targetLootRemainingFraction = 0;
            LootItem[] lootToDestroy = FindLootToDestroy(LooseLootDestroyed, targetLootRemainingFraction);
            foreach (LootItem lootItem in lootToDestroy)
            {
                if (LooseLootDestroyed[lootItem])
                {
                    continue;
                }

                float lootDist = Vector3.Distance(yourPosition, lootItem.transform.position);
                if (lootDist < LootDestroyDist)
                {
                    continue;
                }

                TraderControllerClass traderController = lootItem.ItemOwner;

                Item parentItem = lootItem.Item.GetAllParentItemsAndSelf().Last();
                Item[] allItems = parentItem.GetAllItems().Reverse().ToArray();
                Logger.LogInfo("Destroying loose loot: " + string.Join(",", allItems.Select(i => i.LocalizedName())));
                foreach (Item containedItem in allItems)
                {
                    LootItem[] matchingLootItems = LooseLootDestroyed.Keys.Where(k => k.ItemId == containedItem.Id).ToArray();
                    if ((matchingLootItems.Length != 1) || (!LooseLootDestroyed.ContainsKey(matchingLootItems[0])))
                    {
                        Logger.LogWarning("Could not find entry for " + containedItem.LocalizedName());
                        continue;
                    }

                    Logger.LogInfo("Destroying loose loot in " + lootItem.Item.LocalizedName() + ": " + containedItem.LocalizedName());
                    traderController.DestroyItem(containedItem);
                    //Singleton<GameWorld>.Instance.DestroyLoot(lootItem);
                    LooseLootDestroyed[matchingLootItems[0]] = true;
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

                foreach (Item childItem in lootableContainer.ItemOwner.Items)
                {
                    FindStaticLootInContainer(childItem, lootableContainer.ItemOwner, lootableContainer.transform);
                }
            }
        }

        private void FindStaticLootInContainer(Item container, TraderControllerClass traderController, Transform containerTransform)
        {
            Item[] containedItems = container.GetAllItems().Where(i => i.Id != container.Id).ToArray();
            foreach (Item item in containedItems)
            {
                FindStaticLootInContainer(item, traderController, containerTransform);

                if (!StaticLootDestroyed.ContainsKey(item))
                {
                    StaticLootDestroyed.Add(item, false);
                    StaticLootController.Add(item, traderController);
                    StaticLootTransform.Add(item, containerTransform);
                }
            }
        }

        private void DestroyStaticLoot(Vector3 yourPosition, double targetLootRemainingFraction)
        {
            Item[] lootToDestroy = FindLootToDestroy(StaticLootDestroyed, targetLootRemainingFraction);
            foreach (Item item in lootToDestroy)
            {
                if (StaticLootDestroyed[item])
                {
                    continue;
                }

                float lootDist = Vector3.Distance(yourPosition, StaticLootTransform[item].position);
                if (lootDist < LootDestroyDist)
                {
                    continue;
                }

                Item[] containedItems = item.GetAllItems().Where(i => i.Id != item.Id).Reverse().ToArray();
                foreach (Item containedItem in containedItems)
                {
                    if (!StaticLootDestroyed.ContainsKey(containedItem))
                    {
                        Logger.LogWarning("Could not find entry for " + containedItem.LocalizedName());
                        continue;
                    }

                    Logger.LogInfo("Destroying static loot in " + item.LocalizedName() + ": " + item.LocalizedName());
                    StaticLootController[containedItem].DestroyItem(containedItem);
                    StaticLootDestroyed[containedItem] = true;
                }

                Logger.LogInfo("Destroying static loot: " + item.LocalizedName() + " (Parents: " + string.Join(",", item.GetAllParentItems().Select(p => p.LocalizedName())));
                StaticLootController[item].DestroyItem(item);
                StaticLootDestroyed[item] = true;
            }
        }

        public static T[] FindLootToDestroy<T>(Dictionary<T, bool> lootDict, double targetLootRemainingFraction)
        {
            double currentLootRemainingFraction = (double)lootDict.Values.Where(v => v == false).Count() / lootDict.Count;
            double lootFractionToDestroy = currentLootRemainingFraction - targetLootRemainingFraction;
            if (lootFractionToDestroy <= 0)
            {
                return new T[0];
            }

            System.Random randomGen = new System.Random();
            T[] lootToDestroy = lootDict.Keys.ToArray().OrderBy(e => randomGen.NextDouble()).ToArray();
            lootToDestroy = lootToDestroy.Take((int)Math.Floor(lootFractionToDestroy * lootDict.Count)).ToArray();

            return lootToDestroy;
        }
    }
}
