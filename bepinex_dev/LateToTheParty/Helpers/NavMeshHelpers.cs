using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.Components;
using LateToTheParty.Controllers;
using LateToTheParty.Models;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.Helpers
{
    public static class NavMeshHelpers
    {
        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();

        public static Player GetNearestPlayer(Vector3 position)
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return null;
            }

            float closestDistance = float.MaxValue;
            Player closestPlayer = Singleton<GameWorld>.Instance.MainPlayer;

            foreach (Player player in Singleton<GameWorld>.Instance.AllPlayersEverExisted)
            {
                if ((player == null) || (!player.isActiveAndEnabled))
                {
                    continue;
                }

                float distance = Vector3.Distance(position, player.Transform.position);
                if (distance < closestDistance)
                {
                    closestPlayer = player;
                    closestDistance = distance;
                }
            }

            return closestPlayer;
        }

        public static float GetDistanceToNearestLockedDoor(Vector3 position)
        {
            float closestDistance = float.MaxValue;

            foreach (Door door in Singleton<DoorTogglingComponent>.Instance.ToggleableLockedDoors)
            {
                float distance = Vector3.Distance(position, door.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance;
        }

        public static Vector3? FindNearestNavMeshPosition(Vector3 position, float searchDistance)
        {
            if (nearestNavMeshPoint.ContainsKey(position))
            {
                return nearestNavMeshPoint[position];
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit sourceNearestPoint, searchDistance, NavMesh.AllAreas))
            {
                nearestNavMeshPoint.Add(position, sourceNearestPoint.position);
                return sourceNearestPoint.position;
            }

            return null;
        }

        public static PathAccessibilityData GetPathAccessibilityData(Vector3 sourcePosition, Vector3 targetPosition, string targetPositionName)
        {
            PathAccessibilityData accessibilityData = new PathAccessibilityData();

            // Draw a sphere around the loot item (white = accessibility is undetermined)
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.OutlineLoot)
            {
                Vector3[] targetCirclePoints = DebugHelpers.GetSpherePoints
                (
                    targetPosition,
                    ConfigController.Config.Debug.LootPathVisualization.LootOutlineRadius,
                    ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                );
                accessibilityData.LootOutlineData = new PathVisualizationData(targetPositionName + "_itemOutline", targetCirclePoints, Color.white);
            }

            // Find the nearest NavMesh point to the source position. If one can't be found, give up. 
            Vector3? sourceNearestPoint = FindNearestNavMeshPosition(sourcePosition, ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshSearchMaxDistancePlayer);
            if (!sourceNearestPoint.HasValue)
            {
                return accessibilityData;
            }

            // Find the nearest NavMesh point to the target position. If one can't be found, give up. 
            Vector3? targetNearestPoint = FindNearestNavMeshPosition(targetPosition, ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshSearchMaxDistanceLoot);
            if (!targetNearestPoint.HasValue)
            {
                return accessibilityData;
            }

            // Try to find a path using the NavMesh from the source position to the target position (using the nearest NavMesh points found above)
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(sourceNearestPoint.Value, targetNearestPoint.Value, NavMesh.AllAreas, path);

            // Modify the path vertices so they're off the ground
            Vector3[] pathPoints = new Vector3[path.corners.Length];
            float heightOffset = ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshHeightOffsetComplete;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                pathPoints[i] = new Vector3(path.corners[i].x, path.corners[i].y + heightOffset, path.corners[i].z);
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                // Lower the path vertices so they're clearly separate from complete paths when drawn in the game
                for (int i = 0; i < pathPoints.Length; i++)
                {
                    pathPoints[i].y = path.corners[i].y + ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshHeightOffsetIncomplete;
                }

                if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.DrawIncompletePaths)
                {
                    accessibilityData.PathData = new PathVisualizationData(targetPositionName + "_path", pathPoints, Color.white);

                    // Draw a sphere around the target NavMesh point
                    Vector3 targetNavMeshPosition = new Vector3
                    (
                        targetNearestPoint.Value.x,
                        targetNearestPoint.Value.y + ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshHeightOffsetIncomplete,
                        targetNearestPoint.Value.z
                    );
                    Vector3[] targetCirclePoints = DebugHelpers.GetSpherePoints
                    (
                        targetNavMeshPosition,
                        ConfigController.Config.Debug.LootPathVisualization.CollisionPointRadius,
                        ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                    );
                    accessibilityData.LastNavPointOutline = new PathVisualizationData(targetPositionName + "_targetNavMeshPoint", targetCirclePoints, Color.yellow);
                }

                return accessibilityData;
            }

            // Draw the path in the game
            Vector3[] endLine = new Vector3[] { pathPoints.Last(), targetPosition };
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.DrawCompletePaths)
            {
                accessibilityData.PathData = new PathVisualizationData(targetPositionName + "_path", pathPoints, Color.blue);
            }

            // Check for obstacles between the last NavMesh point (determined above) and the actual target position
            float distToNavMesh = Vector3.Distance(targetPosition, pathPoints.Last());
            Vector3 direction = targetPosition - pathPoints.Last();
            RaycastHit[] targetRaycastHits = Physics.RaycastAll(pathPoints.Last(), direction, distToNavMesh, LayerMaskClass.HighPolyWithTerrainMask);

            // Draw boxes enveloping the colliders for all obstacles between the two points
            if
            (
                ConfigController.Config.Debug.LootPathVisualization.Enabled
                && ConfigController.Config.Debug.LootPathVisualization.OutlineObstacles
                && !ConfigController.Config.Debug.LootPathVisualization.OnlyOutlineFilteredObstacles
            )
            {
                for (int ray = 0; ray < targetRaycastHits.Length; ray++)
                {
                    Vector3[] boundingBoxPoints = targetRaycastHits[ray].collider.bounds.GetBoundingBoxPoints();
                    accessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_boundingBox" + ray, boundingBoxPoints, Color.magenta));

                    /*LoggingController.LogInfo(
                        targetPositionName
                        + " Collider: "
                        + targetRaycastHits[ray].collider.name
                        + " (Bounds Size: "
                        + targetRaycastHits[ray].collider.bounds.size.ToString()
                        + ")"
                    );*/
                }
            }

            // Filter obstacles to remove ones we don't care about
            RaycastHit[] targetRaycastHitsFiltered = targetRaycastHits
                .Where(r => r.collider.bounds.size.y > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshObstacleMinHeight)
                .Where(r => r.collider.attachedRigidbody == null)
                .Where(r => r.collider.bounds.Volume() > ConfigController.Config.DestroyLootDuringRaid.CheckLootAccessibility.NavMeshObstacleMinVolume)
                .ToArray();
            
            // After filtering, draw spheres at all collision points and outline all obstacles
            if (targetRaycastHitsFiltered.Length > 0)
            {
                for (int ray = 0; ray < targetRaycastHitsFiltered.Length; ray++)
                {
                    if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.OutlineObstacles)
                    {
                        Vector3[] boundingBoxPoints = targetRaycastHitsFiltered[ray].collider.bounds.GetBoundingBoxPoints();
                        accessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_boundingBoxFiltered" + ray, boundingBoxPoints, Color.red));
                    }

                    if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.ShowObstacleCollisionPoints)
                    {
                        Vector3[] circlepoints = DebugHelpers.GetSpherePoints
                        (
                            targetRaycastHitsFiltered[ray].point,
                            ConfigController.Config.Debug.LootPathVisualization.CollisionPointRadius,
                            ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle
                        );
                        accessibilityData.RaycastHitMarkers.Add(new PathVisualizationData(targetPositionName + "_ray" + ray, circlepoints, Color.red));
                    }

                    /*LoggingController.LogInfo(
                        targetPositionName
                        + " Collider: "
                        + targetRaycastHitsFiltered[ray].collider.name
                        + " (Bounds Size: "
                        + targetRaycastHitsFiltered[ray].collider.bounds.size.ToString()
                        + ")"
                    );*/
                }

                // Draw a line from the last NavMesh point (determined above) and the actual target position
                if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.ShowObstacleCollisionPoints)
                {
                    accessibilityData.PathEndPointData = new PathVisualizationData(targetPositionName + "_end", endLine, Color.red);
                }
                return accessibilityData;
            }

            // Draw a line from the last NavMesh point (determined above) and the actual target position
            if (ConfigController.Config.Debug.LootPathVisualization.Enabled && ConfigController.Config.Debug.LootPathVisualization.DrawCompletePaths)
            {
                accessibilityData.PathEndPointData = new PathVisualizationData(targetPositionName + "_end", endLine, Color.green);
            }

            // Update accessibility and the color of the sphere around the item
            accessibilityData.IsAccessible = true;
            if (accessibilityData.LootOutlineData != null)
            {
                accessibilityData.LootOutlineData.LineColor = Color.green;
            }

            return accessibilityData;
        }
    }
}
