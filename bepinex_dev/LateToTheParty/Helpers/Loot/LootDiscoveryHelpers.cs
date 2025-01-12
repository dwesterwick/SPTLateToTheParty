using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using LateToTheParty.Controllers;
using UnityEngine;
using LateToTheParty.Components;

namespace LateToTheParty.Helpers.Loot
{
    public static class LootDiscoveryHelpers
    {
        public static void ProcessFoundLooseLootItem(LootItem lootItem, double raidET)
        {
            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (lootItem.transform == null))
            {
                return;
            }

            // Ignore quest items like the bronze pocket watch for "Checking"
            if (lootItem.Item.QuestItem)
            {
                return;
            }

            // Find the nearest spawn point. If none is found, the map is invalid or the raid has ended
            Vector3? nearestSpawnPoint = LocationSettingsController.GetNearestSpawnPointPosition(lootItem.transform.position, EPlayerSideMask.Pmc);
            if (!nearestSpawnPoint.HasValue)
            {
                return;
            }
            double distanceToNearestSpawnPoint = Vector3.Distance(lootItem.transform.position, nearestSpawnPoint.Value);

            // Find all items associated with lootItem that are eligible for despawning
            IEnumerable<Item> allItems = lootItem.Item.FindAllItemsInContainer(true).RemoveExcludedItems().RemoveItemsDroppedByPlayer();
            foreach (Item item in allItems)
            {
                if (Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item) != null)
                {
                    continue;
                }

                Models.LootInfo.LooseLootInfo newLoot = new Models.LootInfo.LooseLootInfo(lootItem, distanceToNearestSpawnPoint, GetLootFoundTime(raidET));

                findNearbyContainters(item, newLoot);

                if (item.IsLocked())
                {
                    newLoot.CannotBeDestroyed = true;
                }

                Singleton<LootDestroyerComponent>.Instance.LootManager.AddLootInfo(item, newLoot);
            }
        }

        public static void ProcessStaticLootContainer(LootableContainer lootableContainer, double raidET)
        {
            if (lootableContainer.ItemOwner == null)
            {
                return;
            }

            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (lootableContainer.transform == null))
            {
                return;
            }

            // Find the nearest spawn point. If none is found, the map is invalid or the raid has ended
            Vector3? nearestSpawnPoint = LocationSettingsController.GetNearestSpawnPointPosition(lootableContainer.transform.position, EPlayerSideMask.Pmc);
            if (!nearestSpawnPoint.HasValue)
            {
                return;
            }
            double distanceToNearestSpawnPoint = Vector3.Distance(lootableContainer.transform.position, nearestSpawnPoint.Value);

            // NOTE: This level is for containers like weapon boxes, not like backpacks
            foreach (Item containerItem in lootableContainer.ItemOwner.Items)
            {
                foreach (Item item in containerItem.FindAllItemsInContainer().RemoveItemsDroppedByPlayer())
                {
                    if (Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item) != null)
                    {
                        continue;
                    }

                    Models.LootInfo.ContainerLootInfo newLoot = new Models.LootInfo.ContainerLootInfo(lootableContainer, distanceToNearestSpawnPoint, GetLootFoundTime(raidET));

                    if (lootableContainer.DoorState == EDoorState.Locked)
                    {
                        newLoot.ParentContainer = lootableContainer;
                    }

                    findNearbyContainters(item, newLoot);

                    if (item.IsLocked())
                    {
                        newLoot.CannotBeDestroyed = true;
                    }

                    Singleton<LootDestroyerComponent>.Instance.LootManager.AddLootInfo(item, newLoot);
                }
            }
        }

        private static void findNearbyContainters(Item lootItem, Models.LootInfo.AbstractLootInfo lootInfo)
        {
            Type typeToSearch = ConfigController.Config.DestroyLootDuringRaid.OnlySearchForNearbyTrunks ? typeof(Trunk) : typeof(WorldInteractiveObject);

            IEnumerable<WorldInteractiveObject> nearbyInteractiveObjects = Singleton<DoorTogglingComponent>.Instance
                .FindNearbyInteractiveObjects(lootInfo.Transform.position, ConfigController.Config.DestroyLootDuringRaid.NearbyInteractiveObjectSearchDistance, typeToSearch)
                .OrderBy(o => Vector3.Distance(lootInfo.Transform.position, o.transform.position));

            if (nearbyInteractiveObjects.Any())
            {
                lootInfo.NearbyInteractiveObject = nearbyInteractiveObjects.First();
                LoggingController.LogInfo(lootItem.LocalizedName() + " is nearby " + lootInfo.NearbyInteractiveObject.GetType().Name + " " + lootInfo.NearbyInteractiveObject.Id);
            }
        }

        private static double GetLootFoundTime(double raidET)
        {
            return raidET == 0 ? -1.0 * ConfigController.Config.DestroyLootDuringRaid.MinLootAge : raidET;
        }
    }
}
