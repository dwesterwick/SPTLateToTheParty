using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Components
{
    public class PathRenderer : MonoBehaviour
    {
        private Dictionary<string, Models.PathVisualizationData> paths = new Dictionary<string, Models.PathVisualizationData>();
        private object pathDictLock = new object();

        protected void LateUpdate()
        {
            // Update each registered path
            lock (pathDictLock)
            {
                foreach (string pathName in paths.Keys)
                {
                    paths[pathName].Update();
                }
            }
        }

        public bool AddOrUpdatePath(Models.PathVisualizationData data)
        {
            if (data == null)
            {
                return false;
            }

            lock (pathDictLock)
            {
                if (paths.ContainsKey(data.PathName))
                {
                    // Need to erase the existing path before replacing it
                    //paths[data.PathName].Erase();
                    paths[data.PathName].Replace(data);
                }
                else
                {
                    paths.Add(data.PathName, data);
                }

                // Draw the new or updated path
                paths[data.PathName].Update();
            }

            return true;
        }

        public bool RemovePath(string pathName)
        {
            lock (pathDictLock)
            {
                if (paths.ContainsKey(pathName))
                {
                    // Prevent the path from being drawn again
                    paths[pathName].Clear();

                    paths.Remove(pathName);
                    return true;
                }
            }

            return false;
        }

        public bool RemovePath(Models.PathVisualizationData data)
        {
            if (data == null)
            {
                Controllers.LoggingController.LogInfo("Path data is null");
                return false;
            }

            // In case the path isn't registered, erase it anyway
            if (!RemovePath(data.PathName))
            {
                //Controllers.LoggingController.LogInfo("Path " + data.PathName + " not found");
                data.Clear();
            }

            return true;
        }
    }
}
