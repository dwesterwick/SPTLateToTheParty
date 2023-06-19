using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Models
{
    public class PathVisualizationData
    {
        public string PathName { get; private set; }
        public Color LineColor { get; set; } = Color.magenta;
        public float LineThickness { get; set; } = 0.05f;

        private Vector3[] pathData;
        private LineRenderer lineRenderer;
        private static object lineRendererLockObj = new object();

        public IEnumerable<Vector3> PathData
        {
            get
            {
                return new ReadOnlyCollection<Vector3>(pathData);
            }
            set
            {
                lock (lineRendererLockObj)
                {
                    pathData = value.ToArray(); 
                }
            }
        }

        public PathVisualizationData(string _pathName)
        {
            PathName = _pathName;
        }

        public PathVisualizationData(string _pathName, Vector3[] _pathData): this(_pathName)
        {
            pathData = _pathData;
        }

        public PathVisualizationData(string _pathName, Vector3[] _pathData, Color _color) : this(_pathName, _pathData)
        {
            LineColor = _color;
        }

        public void Update()
        {
            lock (lineRendererLockObj)
            {
                if (lineRenderer == null)
                {
                    lineRenderer = (new GameObject("Path_" + PathName)).GetOrAddComponent<LineRenderer>();
                    lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                }

                // If there are no points, erase any that have been drawn previously
                if ((pathData == null) || (pathData.Length == 0))
                {
                    lineRenderer.positionCount = 0;
                    return;
                }

                lineRenderer.startColor = LineColor;
                lineRenderer.endColor = LineColor;
                lineRenderer.startWidth = LineThickness;
                lineRenderer.endWidth = LineThickness;

                lineRenderer.positionCount = pathData.Length;
                lineRenderer.SetPositions(pathData);
            }
        }

        public void Erase()
        {
            lock (lineRendererLockObj)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 0;
                }
            }
        }

        public void Clear()
        {
            Erase();
            lock (lineRendererLockObj)
            {
                pathData = null;
            }
        }
    }
}
