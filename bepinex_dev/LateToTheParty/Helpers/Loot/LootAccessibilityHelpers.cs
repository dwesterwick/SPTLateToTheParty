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
using LateToTheParty.Models;
using UnityEngine;
using LateToTheParty.Models.LootInfo;
using LateToTheParty.Components;

namespace LateToTheParty.Helpers.Loot
{
    public static class LootAccessibilityHelpers
    {
        public static void UpdateAccessibility(this Item item)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            AbstractLootInfo lootInfo = Singleton<LootDestroyerComponent>.Instance.LootManager.FindLootInfo(item);
            if (lootInfo == null)
            {
                throw new InvalidOperationException("Cannot destroy loot that has not been found");
            }

            string lootPathName = item.getLootPathName();
            Vector3 itemPosition = lootInfo.Transform.position;

            // Draw a sphere around the loot item
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.OutlineLoot)
            {
                Vector3[] targetCirclePoints = DebugHelpers.GetSpherePoints
                (
                    itemPosition,
                    ConfigController.Config.Debug.LootPathVisualization.LootOutlineRadius,
                    ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                );
                PathVisualizationData lootOutline = new PathVisualizationData(lootPathName + "_itemOutline", targetCirclePoints, Color.white);
                if (lootInfo.PathData.LootOutlineData == null)
                {
                    lootInfo.PathData.LootOutlineData = lootOutline;
                }
                else
                {
                    lootInfo.PathData.LootOutlineData.Replace(lootOutline);
                }
                lootInfo.PathData.Update();
            }

            // Mark the loot as inaccessible if it is inside a locked container
            if ((lootInfo.ParentContainer != null) && (lootInfo.ParentContainer.DoorState == EDoorState.Locked))
            {
                lootInfo.updateLootAccessibility(false, Color.red);
                return;
            }

            // Mark the loot as inaccessible if it is likely behind a locked interactive object
            if ((lootInfo.NearbyInteractiveObject != null) && (lootInfo.NearbyInteractiveObject.DoorState == EDoorState.Locked))
            {
                lootInfo.updateLootAccessibility(false, Color.red);
                return;
            }

            // Make everything accessible if the accessibility-checking system is disabled
            if (!ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.Enabled)
            {
                lootInfo.updateLootAccessibility(true, Color.green);
                return;
            }

            // If the item appeared after the start of the raid, assume it must be accessible (it's likely on a dead bot)
            if (lootInfo.RaidETWhenFound > 0)
            {
                lootInfo.updateLootAccessibility(true, Color.green);
                return;
            }

            // Check if the loot is near a locked door. If not, assume it's accessible. 
            float distanceToNearestLockedDoor = NavMeshHelpers.GetDistanceToNearestLockedDoor(itemPosition);
            if
            (
                (distanceToNearestLockedDoor < float.MaxValue)
                && (distanceToNearestLockedDoor > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.ExclusionRadius)
            )
            {
                lootInfo.updateLootAccessibility(true, Color.green);
                return;
            }

            // Find the nearest position where a player could realistically exist
            Player nearestPlayer = NavMeshHelpers.GetNearestPlayer(itemPosition);
            if (nearestPlayer == null)
            {
                return;
            }
            Vector3? nearestSpawnPointPosition = LocationSettingsController.GetNearestSpawnPointPosition(itemPosition);
            Vector3 nearestPosition = nearestPlayer.Transform.position;
            if (nearestSpawnPointPosition.HasValue && (Vector3.Distance(itemPosition, nearestSpawnPointPosition.Value) < Vector3.Distance(itemPosition, nearestPosition)))
            {
                nearestPosition = nearestSpawnPointPosition.Value;
            }

            // Do not try finding a NavMesh path if the item is too far away due to performance concerns
            if (Vector3.Distance(nearestPosition, itemPosition) > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.MaxPathSearchDistance)
            {
                return;
            }

            // Try to find a path to the loot item via the NavMesh from the nearest realistic position determined above
            PathAccessibilityData fullAccessibilityData = NavMeshHelpers.GetPathAccessibilityData(nearestPosition, itemPosition, lootPathName);
            lootInfo.PathData.Merge(fullAccessibilityData);

            // If the last search resulted in an incomplete path, remove the marker for the previous target NavMesh position
            if (lootInfo.PathData.IsAccessible && (lootInfo.PathData.LastNavPointOutline != null))
            {
                lootInfo.PathData.LastNavPointOutline.Clear();
            }

            lootInfo.PathData.Update();
        }

        private static void updateLootAccessibility(this AbstractLootInfo lootInfo, bool isAccessible, Color outlineColor)
        {
            lootInfo.PathData.IsAccessible = isAccessible;

            if (lootInfo.PathData.LootOutlineData != null)
            {
                lootInfo.PathData.LootOutlineData.LineColor = outlineColor;
            }
            lootInfo.PathData.Clear(true);
            lootInfo.PathData.Update();
        }

        private static string getLootPathName(this Item item)
        {
            return item.LocalizedName() + "_" + item.Id;
        }
    }
}
