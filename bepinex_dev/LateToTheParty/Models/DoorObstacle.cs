using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using LateToTheParty.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.Models
{
    public class DoorObstacle
    {
        public Door LinkedDoor { get; private set; }

        private MeshCollider meshCollider;
        private NavMeshObstacle navMeshObstacle = null;
        private PathVisualizationData visualizationData = null;

        public Vector3? Position
        {
            get { return navMeshObstacle?.transform.position; }
        }

        public DoorObstacle(MeshCollider _meshCollider, Door _door)
        {
            meshCollider = _meshCollider;
            LinkedDoor = _door;
        }

        public void Update()
        {
            bool canOpenDoor = LinkedDoor.DoorState == EDoorState.Open;
            canOpenDoor |= LinkedDoor.DoorState == EDoorState.Shut;

            if (canOpenDoor)
            {
                Remove();
                return;
            }

            Add();
        }

        public void Remove()
        {
            if (navMeshObstacle != null)
            {
                UnityEngine.Object.Destroy(navMeshObstacle);
                navMeshObstacle = null;
            }

            if (visualizationData != null)
            {
                PathRender.RemovePath(visualizationData);
                visualizationData.Clear();
            }
        }

        private void Add()
        {
            string id = "Door_" + LinkedDoor.Id.Replace(" ", "_") + "_Obstacle";

            GameObject doorBlockerObj = new GameObject(id);
            doorBlockerObj.transform.SetParent(meshCollider.transform);
            doorBlockerObj.transform.position = meshCollider.bounds.center;

            navMeshObstacle = doorBlockerObj.AddComponent<NavMeshObstacle>();
            navMeshObstacle.size = meshCollider.bounds.size;
            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = false;

            Vector3 ellipsoidSize = PathRender.IncreaseVector3ToMinSize(navMeshObstacle.size, 0.3f);
            Vector3[] obstaclePoints = PathRender.GetEllipsoidPoints(LinkedDoor.transform.position, ellipsoidSize, 10);
            visualizationData = new PathVisualizationData(id, obstaclePoints, Color.yellow);
            PathRender.AddOrUpdatePath(visualizationData);
        }
    }
}
