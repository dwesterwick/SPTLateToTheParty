using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LateToTheParty.Controllers
{
    public class PathRender : MonoBehaviour
    {
        private static object pathLock = new object();
        private static Dictionary<string, Vector3[]> paths = new Dictionary<string, Vector3[]>();
        private static Dictionary<string, Color> pathColors = new Dictionary<string, Color>();
        private static Dictionary<string, LineRenderer> pathRenderers = new Dictionary<string, LineRenderer>();
        private static float lineWidth = 0.1f;

        private void LateUpdate()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return;
            }

            lock (pathLock)
            {
                foreach (string pathName in paths.Keys)
                {
                    if (!pathRenderers.ContainsKey(pathName))
                    {
                        pathRenderers.Add(pathName, (new GameObject("Path_" + pathName)).GetOrAddComponent<LineRenderer>());
                        pathRenderers[pathName].material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                    }

                    pathRenderers[pathName].startColor = pathColors[pathName];
                    pathRenderers[pathName].endColor = pathColors[pathName];
                    pathRenderers[pathName].startWidth = lineWidth;
                    pathRenderers[pathName].endWidth = lineWidth;

                    pathRenderers[pathName].positionCount = paths[pathName].Length;
                    pathRenderers[pathName].SetPositions(paths[pathName]);
                }
            }
        }

        public static void Clear()
        {
            lock (pathLock)
            {
                paths.Clear();
                pathColors.Clear();
                pathRenderers.Clear();
            }
        }

        public static void AddPath(string pathName, Vector3[] path, Color color)
        {
            lock (pathLock)
            {
                if (paths.ContainsKey(pathName))
                {
                    paths[pathName] = path;
                }
                else
                {
                    paths.Add(pathName, path);
                }

                if (pathColors.ContainsKey(pathName))
                {
                    pathColors[pathName] = color;
                }
                else
                {
                    pathColors.Add(pathName, color);
                }
            }
        }

        public static Vector3[] GetCirclePoints(Vector3 centerPoint, float radius, float pointCount)
        {
            List<Vector3> points = new List<Vector3>();

            float theta_increment = (float)Math.PI * 2 / pointCount;
            for (float theta = 0; theta < 2 * Math.PI; theta += theta_increment)
            {
                float x = radius * (float)Math.Cos(theta);
                float y = radius * (float)Math.Sin(theta);

                points.Add(new Vector3(centerPoint.x + x, centerPoint.y + y, centerPoint.z));
            }
            points.Add(points.First());

            return points.ToArray();
        }
    }
}
