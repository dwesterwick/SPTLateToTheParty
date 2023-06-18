using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class PathRender : MonoBehaviour
    {
        private static Dictionary<string, Models.PathVisualizationData> paths = new Dictionary<string, Models.PathVisualizationData>();

        private void OnDisable()
        {
            Clear();
        }

        private void LateUpdate()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return;
            }

            foreach (string pathName in paths.Keys)
            {
                paths[pathName].Update();
            }
        }

        public static void Clear()
        {
            foreach (string pathName in paths.Keys)
            {
                paths[pathName].Clear();
            }

            paths.Clear();
        }

        public static bool AddOrUpdatePath(Models.PathVisualizationData data)
        {
            if (data == null)
            {
                return false;
            }

            if (paths.ContainsKey(data.PathName))
            {
                //paths[data.PathName].Clear();
                paths[data.PathName] = data;
            }
            else
            {
                paths.Add(data.PathName, data);
            }

            paths[data.PathName].Update();

            return true;
        }

        public static bool RemovePath(string pathName)
        {
            if (paths.ContainsKey(pathName))
            {
                paths[pathName].Clear();
                paths.Remove(pathName);
                return true;
            }

            return false;
        }

        public static bool RemovePath(Models.PathVisualizationData data)
        {
            if (data == null)
            {
                return false;
            }

            if (!RemovePath(data.PathName))
            {
                data.Clear();
            }

            return true;
        }

        public static Vector3 IncreaseVector3ToMinSize(Vector3 vector, float minSize)
        {
            return new Vector3((float)Math.Max(minSize, vector.x), (float)Math.Max(minSize, vector.y), (float)Math.Max(minSize, vector.z));
        }

        public static Vector3[] GetSpherePoints(Vector3 centerPoint, float radius, float pointCount)
        {
            return GetEllipsoidPoints(centerPoint, new Vector3(radius, radius, radius), pointCount);
        }

        public static Vector3[] GetEllipsoidPoints(Vector3 centerPoint, Vector3 radii, float pointCount)
        {
            List<Vector3> points = new List<Vector3>();

            float theta_increment = (float)Math.PI * 2 / pointCount;
            for (float theta = 0; theta < 2 * Math.PI; theta += theta_increment)
            {
                float x = radii.x * (float)Math.Cos(theta);
                float y = radii.y * (float)Math.Sin(theta);

                points.Add(new Vector3(centerPoint.x + x, centerPoint.y + y, centerPoint.z));
            }
            points.Add(points.First());

            for (float theta = 0; theta < 2 * Math.PI; theta += theta_increment)
            {
                float x = radii.x * (float)Math.Cos(theta);
                float z = radii.z * (float)Math.Sin(theta);

                points.Add(new Vector3(centerPoint.x + x, centerPoint.y, centerPoint.z + z));
            }

            return points.ToArray();
        }

        public static Vector3[] GetBoundingBoxPoints(Bounds bounds)
        {
            return new Vector3[]
            {
                bounds.min,

                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),

                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),

                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),

                bounds.min,

                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                bounds.max,
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)
            };
        }
    }
}
