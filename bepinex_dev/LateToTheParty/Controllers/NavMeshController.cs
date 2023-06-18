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
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Models;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.Controllers
{
    public class NavMeshController: MonoBehaviour
    {
        public static bool IsUpdatingDoorsObstacles { get; private set; } = false;

        private static Dictionary<Door, DoorObstacle> doorObstacles = new Dictionary<Door, DoorObstacle>();
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.OpenDoorsDuringRaid.MaxCalcTimePerFrame);
        private static Stopwatch updateTimer = Stopwatch.StartNew();

        private void OnDisable()
        {
            Clear();
        }

        private void LateUpdate()
        {
            // Clear all arrays if not in a raid to reset them for the next raid
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                Clear();
                return;
            }

            // Ensure enough time has passed since the last check
            if (IsUpdatingDoorsObstacles || (updateTimer.ElapsedMilliseconds < 2 * 1000))
            {
                return;
            }

            if (doorObstacles.Count() > 0)
            {
                // Update the nav mesh to reflect the door state changes
                StartCoroutine(UpdateDoorObstacles());
                updateTimer.Restart();

                return;
            }

            foreach (MeshCollider collider in FindObjectsOfType<MeshCollider>())
            {
                CheckIfColliderIsDoor(collider);
            }
        }

        public static void Clear()
        {
            if (IsUpdatingDoorsObstacles)
            {
                enumeratorWithTimeLimit.Abort();
                TaskWithTimeLimit.WaitForCondition(() => !IsUpdatingDoorsObstacles);
            }

            foreach (DoorObstacle doorObstacle in doorObstacles.Values)
            {
                doorObstacle.Remove();
            }
            doorObstacles.Clear();

            updateTimer.Restart();
        }

        public static Player GetNearestPlayer(Vector3 position)
        {
            float closestDistance = float.MaxValue;
            Player closestPlayer = Singleton<GameWorld>.Instance.MainPlayer;

            foreach (Player player in Singleton<GameWorld>.Instance.AllPlayers)
            {
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

            foreach (DoorObstacle obstacle in doorObstacles.Values)
            {
                if (!obstacle.Position.HasValue)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, obstacle.Position.Value);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance;
        }

        public static void CheckIfColliderIsDoor(MeshCollider meshCollider)
        {
            if (meshCollider.gameObject.layer != LayerMaskClass.DoorLayer)
            {
                return;
            }

            GameObject doorObject = meshCollider.transform.parent.gameObject;
            Door door = doorObject.GetComponent<Door>();

            if (door == null)
            {
                return;
            }

            if (!DoorController.IsToggleableDoor(door))
            {
                return;
            }

            doorObstacles.Add(door, new DoorObstacle(meshCollider, door));
        }

        public static IEnumerator UpdateDoorObstacles()
        {
            IsUpdatingDoorsObstacles = true;

            // Update door blockers
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(doorObstacles.Keys.ToArray(), UpdateDoorObstacle);

            IsUpdatingDoorsObstacles = false;
        }

        public static void UpdateDoorObstacle(Door door)
        {
            doorObstacles[door].Update();
        }

        public static Vector3? FindNearestNavMeshPosition(Vector3 position, float searchDistance)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit sourceNearestPoint, searchDistance, NavMesh.AllAreas))
            {
                return sourceNearestPoint.position;
            }

            return null;
        }

        public static PathAccessibilityData GetPathAccessibilityData(Vector3 sourcePosition, Vector3 targetPosition, string targetPositionName)
        {
            PathAccessibilityData lootAccessibilityData = new PathAccessibilityData();

            Vector3[] targetCirclePoints = PathRender.GetSpherePoints(targetPosition, 0.1f, 10);
            lootAccessibilityData.LootOutlineData = new PathVisualizationData(targetPositionName + "_itemOutline", targetCirclePoints, Color.white);

            Vector3? sourceNearestPoint = FindNearestNavMeshPosition(sourcePosition, 10);
            if (!sourceNearestPoint.HasValue)
            {
                return lootAccessibilityData;
            }

            Vector3? targetNearestPoint = FindNearestNavMeshPosition(targetPosition, 2);
            if (!targetNearestPoint.HasValue)
            {
                return lootAccessibilityData;
            }

            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(sourceNearestPoint.Value, targetNearestPoint.Value, NavMesh.AllAreas, path);

            Vector3[] pathPoints = new Vector3[path.corners.Length];
            float heightOffset = 1.25f;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                pathPoints[i] = new Vector3(path.corners[i].x, path.corners[i].y + heightOffset, path.corners[i].z);
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                for (int i = 0; i < pathPoints.Length; i++)
                {
                    pathPoints[i].y -= 0.25f;
                }
                lootAccessibilityData.PathData = new PathVisualizationData(targetPositionName + "_path", pathPoints, Color.white);
                return lootAccessibilityData;
            }

            Vector3[] endLine = new Vector3[] { pathPoints.Last(), targetPosition };
            lootAccessibilityData.PathData = new PathVisualizationData(targetPositionName + "_path", pathPoints, Color.blue);

            float distToNavMesh = Vector3.Distance(targetPosition, pathPoints.Last());
            Vector3 direction = targetPosition - pathPoints.Last();
            RaycastHit[] targetRaycastHits = Physics.RaycastAll(pathPoints.Last(), direction, distToNavMesh, LayerMaskClass.HighPolyWithTerrainMask);

            for (int ray = 0; ray < targetRaycastHits.Length; ray++)
            {
                Vector3[] boundingBoxPoints = PathRender.GetBoundingBoxPoints(targetRaycastHits[ray].collider.bounds);
                lootAccessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_boundingBox" + ray, boundingBoxPoints, Color.magenta));

                /*LoggingController.LogInfo(
                    targetPositionName
                    + " Collider: "
                    + targetRaycastHits[ray].collider.name
                    + " (Bounds Size: "
                    + targetRaycastHits[ray].collider.bounds.size.ToString()
                    + ")"
                );*/
            }

            RaycastHit[] targetRaycastHitsFiltered = targetRaycastHits
                .Where(r => r.collider.bounds.size.y > 0.9)
                .Where(r => r.collider.attachedRigidbody == null)
                .Where(r => r.collider.bounds.Volume() > 2)
                .ToArray();
            
            if (targetRaycastHitsFiltered.Length > 0)
            {
                for (int ray = 0; ray < targetRaycastHitsFiltered.Length; ray++)
                {
                    Vector3[] boundingBoxPoints = PathRender.GetBoundingBoxPoints(targetRaycastHitsFiltered[ray].collider.bounds);
                    lootAccessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_boundingBoxFiltered" + ray, boundingBoxPoints, Color.red));

                    Vector3[] circlepoints = PathRender.GetSpherePoints(targetRaycastHitsFiltered[ray].point, 0.05f, 10);
                    lootAccessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_ray" + ray, circlepoints, Color.red));

                    /*LoggingController.LogInfo(
                        targetPositionName
                        + " Collider: "
                        + targetRaycastHitsFiltered[ray].collider.name
                        + " (Bounds Size: "
                        + targetRaycastHitsFiltered[ray].collider.bounds.size.ToString()
                        + ")"
                    );*/
                }

                
                lootAccessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_end", endLine, Color.red));
                return lootAccessibilityData;
            }

            lootAccessibilityData.IsAccessible = true;
            lootAccessibilityData.LootOutlineData.LineColor = Color.green;
            lootAccessibilityData.BoundingBoxes.Add(new PathVisualizationData(targetPositionName + "_end", endLine, Color.green));
            return lootAccessibilityData;
        }
    }
}
