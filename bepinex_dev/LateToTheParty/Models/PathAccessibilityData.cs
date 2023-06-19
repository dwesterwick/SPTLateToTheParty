using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LateToTheParty.Controllers;
using UnityEngine;

namespace LateToTheParty.Models
{
    public class PathAccessibilityData
    {
        public bool IsAccessible { get; set; } = false;
        public PathVisualizationData PathData { get; set; }
        public PathVisualizationData PathEndPointData { get; set; }
        public PathVisualizationData LootOutlineData { get; set; }
        public List<PathVisualizationData> BoundingBoxes { get; set; } = new List<PathVisualizationData>();
        public List<PathVisualizationData> RaycastHitMarkers { get; set; } = new List<PathVisualizationData>();

        public PathAccessibilityData()
        {

        }

        public void Merge(PathAccessibilityData other)
        {
            if (other.PathData != null) { PathData = other.PathData; }
            if (other.PathEndPointData != null) { PathEndPointData = other.PathEndPointData; }
            if (other.LootOutlineData != null) { LootOutlineData = other.LootOutlineData; }

            foreach (PathVisualizationData data in other.BoundingBoxes)
            {
                if (!BoundingBoxes.Contains(data)) { BoundingBoxes.Add(data); }
            }
            foreach (PathVisualizationData data in other.RaycastHitMarkers)
            {
                if (!RaycastHitMarkers.Contains(data)) { RaycastHitMarkers.Add(data); }
            }
        }

        public void MergeAndUpdate(PathAccessibilityData other)
        {
            Merge(other);
            Update();
        }

        public void Update()
        {
            PathRender.AddOrUpdatePath(PathData);
            PathRender.AddOrUpdatePath(PathEndPointData);
            PathRender.AddOrUpdatePath(LootOutlineData);

            foreach (PathVisualizationData data in BoundingBoxes)
            {
                PathRender.AddOrUpdatePath(data);
            }
            foreach (PathVisualizationData data in RaycastHitMarkers)
            {
                PathRender.AddOrUpdatePath(data);
            }
        }

        public void Clear(bool keepLootOutline = false)
        {
            PathRender.RemovePath(PathData);
            PathRender.RemovePath(PathEndPointData);

            if (!keepLootOutline)
            {
                PathRender.RemovePath(LootOutlineData);
            }

            foreach (PathVisualizationData data in BoundingBoxes)
            {
                PathRender.RemovePath(data);
            }
            foreach (PathVisualizationData data in RaycastHitMarkers)
            {
                PathRender.RemovePath(data);
            }
        }
    }
}
