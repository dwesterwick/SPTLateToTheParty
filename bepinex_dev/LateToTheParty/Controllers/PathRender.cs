using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class PathRender : MonoBehaviour
    {
        private static object pathLock = new object();
        private static Dictionary<string, Vector3[]> paths = new Dictionary<string, Vector3[]>();
        private static Dictionary<string, Color> pathColors = new Dictionary<string, Color>();
        private static Dictionary<string, LineRenderer> pathRenderers = new Dictionary<string, LineRenderer>();
        private static float lineWidth = 0.2f;

        private void LateUpdate()
        {
            lock (pathLock)
            {
                foreach (string pathName in paths.Keys)
                {
                    if (!pathRenderers.ContainsKey(pathName))
                    {
                        pathRenderers.Add(pathName, (new GameObject("Path_" + pathName)).GetOrAddComponent<LineRenderer>());
                        pathRenderers[pathName].material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                        //pathRenderers[pathName].material = new Material(Shader.Find("Unlit/Texture"));
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
    }
}
