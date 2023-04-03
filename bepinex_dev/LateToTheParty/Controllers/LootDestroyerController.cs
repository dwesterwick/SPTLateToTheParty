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

            Vector3 yourPosition = Camera.main.transform.position;
            float lastUpdateDist = Vector3.Distance(yourPosition, lastUpdatePosition);
            if (lastUpdateDist < minUpdateDist)
            {
                return;
            }

            Stopwatch jobTimer = Stopwatch.StartNew();
            Logger.LogInfo("Destroying loot...");

            AllLootItems = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToArray();
            DestroyLooseLoot(yourPosition);
            DestroyStaticLoot(yourPosition);

            lastUpdatePosition = yourPosition;
            Logger.LogInfo("Destroying loot...done. (" + jobTimer.ElapsedMilliseconds + " ms)");
        }

        private void DestroyLooseLoot(Vector3 yourPosition)
        {
            foreach (LootItem lootItem in AllLootItems)
            {
                if (lootItem.Item.QuestItem)
                {
                    continue;
                }

                float lootDist = Vector3.Distance(yourPosition, lootItem.transform.position);
                if (lootDist < LootDestroyDist)
                {
                    continue;
                }

                Logger.LogInfo("Destroying loot: " + lootItem.Item.LocalizedName());
                Singleton<GameWorld>.Instance.DestroyLoot(lootItem);
            }
        }

        private void DestroyStaticLoot(Vector3 yourPosition)
        {
            foreach (LootableContainer lootableContainer in AllLootableContainers)
            {
                float lootDist = Vector3.Distance(yourPosition, lootableContainer.transform.position);
                if (lootDist < LootDestroyDist)
                {
                    continue;
                }

                if (lootableContainer.ItemOwner == null)
                {
                    continue;
                }

                foreach (Item childItem in lootableContainer.ItemOwner.Items)
                {
                    DestroyItemsInContainer(childItem, lootableContainer.ItemOwner);
                }
            }
        }

        private void DestroyItemsInContainer(Item container, TraderControllerClass traderController)
        {
            Item[] containedItems = container.GetAllItems().ToArray();
            foreach (Item item in containedItems.Reverse())
            {
                if (item.Id == container.Id)
                {
                    continue;
                }

                DestroyItemsInContainer(item, traderController);

                Logger.LogInfo("Destroying " + item.LocalizedName() + " from " + container.LocalizedName() + "...");
                traderController.DestroyItem(item);
            }
        }
    }
}
