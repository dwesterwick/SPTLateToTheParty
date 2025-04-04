﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using EFT.InventoryLogic;
using LateToTheParty.Controllers;
using LateToTheParty.Models.LootInfo;
using UnityEngine;
using LateToTheParty.Components;

namespace LateToTheParty.Helpers.Loot
{
    public static class LootDestructionHelpers
    {
        public static event Action<Item> OnDestroyLoot;

        public static void StartDestruction(this Item item)
        {
            AbstractLootInfo lootInfo = Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item);
            if (lootInfo == null)
            {
                throw new InvalidOperationException("Cannot destroy loot that has not been found");
            }

            try
            {
                item.openNearbyDoorForLoot();

                item.DestroyViaLTTP();

                if (OnDestroyLoot != null)
                {
                    OnDestroyLoot(item);
                }

                Singleton<LootDestroyerComponent>.Instance.LootManager.ConfirmItemDestruction(item);
            }
            catch (Exception ex)
            {
                LoggingController.LogError("Could not destroy " + item.LocalizedName());
                LoggingController.LogError(ex.ToString());
                lootInfo.CannotBeDestroyed = true;
            }
        }

        public static void DestroyViaLTTP(this Item item)
        {
            // This method no longer exists in SPT 3.10
            //lootInfo.TraderController.DestroyItem(item);

            ItemAddress address = item.CurrentAddress;
            if (address == null)
            {
                throw new InvalidOperationException($"The address of {item.LocalizedName()} was null");
            }

            // Item in container
            if (item.Owner == null || item.Owner.ID != item.Id)
            {
                address.RemoveWithoutRestrictions(item);
                return;
            }

            // Item in world
            item.Owner.RaiseRemoveEvent(new GEventArgs3(item, item.CurrentAddress, CommandStatus.Succeed, item.Owner));
        }

        private static void openNearbyDoorForLoot(this Item item)
        {
            AbstractLootInfo lootInfo = Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item);
            if (lootInfo == null)
            {
                throw new InvalidOperationException("Cannot destroy loot that has not been found");
            }

            // Check if there is a door near the item
            if (lootInfo.NearbyInteractiveObject == null)
            {
                return;
            }

            if (lootInfo.NearbyInteractiveObject.DoorState == EDoorState.Locked)
            {
                throw new InvalidOperationException("Cannot destroy loot behind a locked interactive object");
            }

            // This should be checked after ensuring the door is not locked as a sanity check
            if (!ConfigController.Config.OpenDoorsDuringRaid.Enabled)
            {
                return;
            }

            if (lootInfo.NearbyInteractiveObject.DoorState == EDoorState.Open)
            {
                return;
            }

            if (lootInfo.NearbyInteractiveObject.DoorState != EDoorState.Shut)
            {
                LoggingController.LogError("Cannot open door " + lootInfo.NearbyInteractiveObject.Id + " because its state is " + lootInfo.NearbyInteractiveObject.DoorState);
                return;
            }

            lootInfo.NearbyInteractiveObject.StartExecuteInteraction(new InteractionResult(EInteractionType.Open));
        }

        public static void UpdateLootEligibility(Item item, IEnumerable<Vector3> playerPositions, double raidET)
        {
            AbstractLootInfo lootInfo = Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item);
            if (lootInfo == null)
            {
                throw new InvalidOperationException("Cannot update eligibility for loot that has not been found");
            }

            lootInfo.EligibleForDestruction = item.IsEligibleForDestruction(playerPositions, raidET);
        }

        public static bool IsEligibleForDestruction(this Item item, IEnumerable<Vector3> playerPositions, double raidET)
        {
            AbstractLootInfo lootInfo = Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item);
            if (lootInfo == null)
            {
                return false;
            }

            if (lootInfo.IsDestroyed || lootInfo.IsInPlayerInventory || Singleton<LootDestroyerComponent>.Instance.LootManager.WasDroppedByPlayer(item))
            {
                return false;
            }

            // Ensure enough time has elapsed since the loot was first placed on the map (to prevent loot on dead bots from being destroyed too soon)
            double lootAge = raidET - lootInfo.RaidETWhenFound.Value;
            if (lootAge < ConfigController.Config.DestroyLootDuringRaid.MinLootAge)
            {
                //LoggingController.LogInfo("Ignoring " + item.LocalizedName() + " (Loot age: " + lootAge + ")");
                return false;
            }

            // Ensure you're still in the raid to avoid NRE's when it ends
            if ((Camera.main == null) || (lootInfo.Transform == null))
            {
                return false;
            }

            // Ignore loot that's too close to a player
            if (playerPositions.Any(p => Vector3.Distance(p, lootInfo.Transform.position) < ConfigController.Config.DestroyLootDuringRaid.ExclusionRadius))
            {
                return false;
            }

            // Ignore loot that's too close to bots
            Player nearestPlayer = NavMeshHelpers.GetNearestPlayer(lootInfo.Transform.position);
            if (nearestPlayer == null)
            {
                return false;
            }
            float lootDist = Vector3.Distance(nearestPlayer.Position, lootInfo.Transform.position);
            if (lootDist < ConfigController.Config.DestroyLootDuringRaid.ExclusionRadiusBots)
            {
                return false;
            }

            // Ignore loot that players couldn't have possibly reached yet
            double maxBotRunDistance = raidET * ConfigController.Config.DestroyLootDuringRaid.MapTraversalSpeed;
            if (maxBotRunDistance < lootInfo.DistanceToNearestSpawnPoint)
            {
                //LoggingController.LogInfo("Ignoring " + item.LocalizedName() + " (Loot Distance: " + LootInfo[item].DistanceToNearestSpawnPoint + ", Current Distance: " + maxBotRunDistance + ")");
                return false;
            }

            return true;
        }

        public static void FindChildItemsToDestroy(this Item item, int totalItemsToDestroy, int lootSlotsToDestroy, List<Item> allItemsToDestroy)
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

            // Do not search for more items if enough slots will be destroyed for the items in the queue
            if (allItemsToDestroy.Sum(i => i.GetItemSlots()) >= lootSlotsToDestroy)
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
            if (item.CanRemoveFrom(parentItem))
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

                allItems.AddItemsToDespawnList(item, allItemsToDestroy);
                return;
            }
            LoggingController.LogInfo(item.LocalizedName() + " cannot be removed from " + parentItem.LocalizedName() + ". Destroying parent item and all children.");

            // Get all children of the parent item and add them to the despawn list
            allItems = parentItem.GetAllItems().Reverse().ToArray();
            allItems.AddItemsToDespawnList(parentItem, allItemsToDestroy);
        }

        public static int AddItemsToDespawnList(this Item[] items, Item parentItem, List<Item> allItemsToDestroy)
        {
            int despawnCount = 0;
            foreach (Item item in items)
            {
                despawnCount += item.TryAddItemToDespawnList(parentItem, allItemsToDestroy) ? 1 : 0;
            }
            return despawnCount;
        }

        public static bool TryAddItemToDespawnList(this Item item, Item parentItem, List<Item> allItemsToDestroy)
        {
            if (allItemsToDestroy.Contains(item))
            {
                return false;
            }

            AbstractLootInfo lootInfo = Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item);
            if (lootInfo == null)
            {
                LoggingController.LogWarning("Could not find entry for " + item.LocalizedName());
                return false;
            }

            if (lootInfo.CannotBeDestroyed)
            {
                //LoggingController.LogWarning("Ensure parent " + parentItem.LocalizedName() + " for " + item.LocalizedName() + " is being destroyed!");
                return false;
            }

            if (item.CurrentAddress == null)
            {
                LoggingController.LogWarning("Invalid parent for " + item.LocalizedName());
                return false;
            }

            // Ensure child items are destroyed before parent items
            lootInfo.ParentItem = parentItem;
            if ((item.Parent.Container.ParentItem != null) && allItemsToDestroy.Contains(item.Parent.Container.ParentItem))
            {
                allItemsToDestroy.Insert(allItemsToDestroy.IndexOf(item.Parent.Container.ParentItem), item);
            }
            else
            {
                allItemsToDestroy.Add(item);
            }

            return true;
        }
    }
}
