using LateToTheParty.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

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

        public void Clear()
        {
            PathRender.RemovePath(PathData);
            PathRender.RemovePath(PathEndPointData);
            PathRender.RemovePath(LootOutlineData);

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
