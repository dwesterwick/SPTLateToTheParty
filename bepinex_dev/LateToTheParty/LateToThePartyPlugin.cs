using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

namespace LateToTheParty
{
    [BepInPlugin("com.DanW.LateToTheParty", "LateToThePartyPlugin", "1.1.0.0")]
    public class LateToThePartyPlugin : BaseUnityPlugin
    {
        public static Configuration.ModConfig ModConfig { get; set; } = null;
        public static string[] CarExtractNames { get; set; } = new string[0];

        private float LootDestroyDist = 20;
        private float minUpdateDist = 1;
        private Vector3 lastUpdatePosition = Vector3.zero;
        private int lastLooseLootCount = 0;
        private int lastStaticLootCount = 0;

        private void Awake()
        {
            Logger.LogInfo("Loading LateToThePartyPlugin...");

            Logger.LogInfo("Loading LateToThePartyPlugin...getting configuration data...");
            ModConfig = Controllers.ConfigController.GetConfig();

            if (ModConfig.Enabled)
            {
                Logger.LogInfo("Loading LateToThePartyPlugin...enabling patches...");
                new Patches.ReadyToPlayPatch().Enable();
                new Patches.ShowScreenPatch().Enable();

                Logger.LogInfo("Loading LateToThePartyPlugin...getting car extract names...");
                CarExtractNames = Controllers.ConfigController.GetCarExtractNames();
            }
            
            Logger.LogInfo("Loading LateToThePartyPlugin...done.");
        }

        private void FixedUpdate()
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }
            Vector3 yourPosition = Camera.main.transform.position;

            float lastUpdateDist = Vector3.Distance(yourPosition, lastUpdatePosition);
            if (lastUpdateDist < minUpdateDist)
            {
                return;
            }

            DestroyLooseLoot(yourPosition);
            DestroyStaticLoot(yourPosition);
            lastUpdatePosition = yourPosition;
        }

        private void DestroyLooseLoot(Vector3 yourPosition)
        {
            List<GClass1171> allLootSeriously = Singleton<GameWorld>.Instance.AllLoot;
            List<GInterface7> allLootSeriously2 = Singleton<GameWorld>.Instance.LootList;
            GClass743<int, LootItem> allLootSeriously3 = Singleton<GameWorld>.Instance.LootItems;
            List<LootItem> allLoot = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToList();

            /*if (allLoot.Count == lastLooseLootCount)
            {
                return;
            }
            lastLooseLootCount = allLoot.Count;*/

            foreach (GInterface7 item in allLootSeriously2.ToArray())
            {
                //Singleton<GameWorld>.Instance.DestroyLoot(item);
            }

            foreach (LootItem lootItem in allLoot)
            {
                if (lootItem.Item.QuestItem)
                {
                    //Logger.LogInfo("Skipping " + lootItem.Item.LocalizedName());
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
            LootableContainer[] lootableContainers = UnityEngine.Object.FindObjectsOfType<LootableContainer>();
            //int itemCount = lootableContainers.Sum(lc => lc.ItemOwner.Items.Sum(i => i.GetAllItems().Count()));

            /*if (itemCount == lastStaticLootCount)
            {
                return;
            }
            lastStaticLootCount = itemCount;*/

            foreach (LootableContainer lootableContainer in lootableContainers)
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

                Logger.LogInfo("Discarding " + item.LocalizedName() + "...");
                traderController.DestroyItem(item);
            }
        }
    }
}
