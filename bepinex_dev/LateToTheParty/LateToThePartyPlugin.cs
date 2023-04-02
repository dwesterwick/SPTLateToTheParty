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

        private Transform _mainCameraTransform;
        private float LootDestroyDist = 50;
        private int lastLootCount = 0;

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

            if (this._mainCameraTransform == null)
            {
                Camera main = Camera.main;
                if (main != null)
                {
                    this._mainCameraTransform = main.transform;
                }
                return;
            }

            Vector3 position = this._mainCameraTransform.position;

            List<GClass1171> allLootSeriously = Singleton<GameWorld>.Instance.AllLoot;
            List<GInterface7> allLootSeriously2 = Singleton<GameWorld>.Instance.LootList;
            GClass743<int, LootItem> allLootSeriously3 = Singleton<GameWorld>.Instance.LootItems;

            List<LootItem> allLoot = Singleton<GameWorld>.Instance.LootList.OfType<LootItem>().ToList();

            if (lastLootCount == allLoot.Count)
            {
                return;
            }
            lastLootCount = allLoot.Count;

            Logger.LogInfo("Loot count: " + allLoot.Count + " (" + allLootSeriously.Count + "), (" + allLootSeriously2.Count + ")");

            foreach (GInterface7 item in allLootSeriously2.ToArray())
            {
                //Singleton<GameWorld>.Instance.DestroyLoot(item);
            }

            foreach (LootItem lootItem in allLoot)
            {
                if (lootItem.Item.QuestItem)
                {
                    Logger.LogInfo("Skipping " + lootItem.Item.LocalizedName());
                    continue;
                }

                float lootDist = Vector3.Distance(position, lootItem.transform.position);

                if (lootDist > LootDestroyDist)
                {
                    Logger.LogInfo("Destroying loot: " + lootItem.Item.LocalizedName());
                    Singleton<GameWorld>.Instance.DestroyLoot(lootItem);
                }
            }

            LootableContainer[] lootableContainers = UnityEngine.Object.FindObjectsOfType<LootableContainer>();
            foreach(LootableContainer lootableContainer in lootableContainers)
            {
                foreach(Item childItem in lootableContainer.ItemOwner.Items)
                {
                    Item[] containedItems = childItem.GetAllItems().ToArray();
                    foreach(Item item in containedItems.Reverse())
                    {
                        if (item.Id == childItem.Id)
                        {
                            continue;
                        }

                        Logger.LogInfo("Discarding " + item.LocalizedName() + "...");
                        lootableContainer.ItemOwner.DestroyItem(item);
                    }
                }
            }
        }
    }
}
