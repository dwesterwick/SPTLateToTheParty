using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using LateToTheParty.Components;

namespace LateToTheParty.Models
{
    public class PathAccessibilityData
    {
        public bool IsAccessible { get; set; } = false;
        public PathRenderer pathRenderer;
        public PathVisualizationData PathData { get; set; }
        public PathVisualizationData LastNavPointOutline { get; set; }
        public PathVisualizationData PathEndPointData { get; set; }
        public PathVisualizationData LootOutlineData { get; set; }
        public List<PathVisualizationData> BoundingBoxes { get; set; } = new List<PathVisualizationData>();
        public List<PathVisualizationData> RaycastHitMarkers { get; set; } = new List<PathVisualizationData>();

        public PathAccessibilityData()
        {
            pathRenderer = Singleton<PathRenderer>.Instance;
        }

        public void Merge(PathAccessibilityData other)
        {
            if (pathRenderer == null)
            {
                return;
            }

            IsAccessible |= other.IsAccessible;

            if (other.PathData != null)
            {
                if (PathData != null)
                {
                    PathData.Replace(other.PathData);
                }
                else
                {
                    PathData = other.PathData;
                }
            }
            if (other.LastNavPointOutline != null)
            {
                if (LastNavPointOutline != null)
                {
                    LastNavPointOutline.Replace(other.LastNavPointOutline);
                }
                else
                {
                    LastNavPointOutline = other.LastNavPointOutline;
                }
            }
            if (other.PathEndPointData != null)
            {
                if (PathEndPointData != null)
                {
                    PathEndPointData.Replace(other.PathEndPointData);
                }
                else
                {
                    PathEndPointData = other.PathEndPointData;
                }
            }
            if (other.LootOutlineData != null)
            {
                if (LootOutlineData != null)
                {
                    LootOutlineData.Replace(other.LootOutlineData);
                }
                else
                {
                    LootOutlineData = other.LootOutlineData;
                }
            }

            if (other.BoundingBoxes.Count > 0)
            {
                foreach (PathVisualizationData data in BoundingBoxes)
                {
                    pathRenderer.RemovePath(data);
                }
                BoundingBoxes.Clear();
                foreach (PathVisualizationData data in other.BoundingBoxes)
                {
                    BoundingBoxes.Add(data);
                }
            }

            if (other.RaycastHitMarkers.Count > 0)
            {
                foreach (PathVisualizationData data in RaycastHitMarkers)
                {
                    pathRenderer.RemovePath(data);
                }
                RaycastHitMarkers.Clear();
                foreach (PathVisualizationData data in other.RaycastHitMarkers)
                {
                    RaycastHitMarkers.Add(data);
                }
            }
        }

        public void MergeAndUpdate(PathAccessibilityData other)
        {
            Merge(other);
            Update();
        }

        public void Update()
        {
            if (pathRenderer == null)
            {
                return;
            }

            pathRenderer.AddOrUpdatePath(PathData);
            pathRenderer.AddOrUpdatePath(LastNavPointOutline);
            pathRenderer.AddOrUpdatePath(PathEndPointData);
            pathRenderer.AddOrUpdatePath(LootOutlineData);

            foreach (PathVisualizationData data in BoundingBoxes)
            {
                pathRenderer.AddOrUpdatePath(data);
            }
            foreach (PathVisualizationData data in RaycastHitMarkers)
            {
                pathRenderer.AddOrUpdatePath(data);
            }
        }

        public void Clear(bool keepLootOutline = false)
        {
            if (pathRenderer == null)
            {
                return;
            }

            if (PathData != null)
            {
                pathRenderer.RemovePath(PathData);
                PathData.Clear();
            }
            if (LastNavPointOutline != null)
            {
                pathRenderer.RemovePath(LastNavPointOutline);
                LastNavPointOutline.Clear();
            }
            if (PathEndPointData != null)
            {
                pathRenderer.RemovePath(PathEndPointData);
                PathEndPointData.Clear();
            }

            if (!keepLootOutline)
            {
                if (LootOutlineData != null)
                {
                    pathRenderer.RemovePath(LootOutlineData);
                    LootOutlineData.Clear();
                }
            }

            foreach (PathVisualizationData data in BoundingBoxes)
            {
                pathRenderer.RemovePath(data);
                data.Clear();
            }
            BoundingBoxes.Clear();

            foreach (PathVisualizationData data in RaycastHitMarkers)
            {
                pathRenderer.RemovePath(data);
                data.Clear();
            }
            RaycastHitMarkers.Clear();
        }
    }
}
