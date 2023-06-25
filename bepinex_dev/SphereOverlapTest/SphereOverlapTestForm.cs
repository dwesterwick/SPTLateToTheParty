using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SphereOverlapTest
{
    enum Plane
    {
        XZ,
        XY,
        YZ,
    }

    public partial class SphereOverlapTestForm : Form
    {
        private Pen overallCirclePen = new Pen(Color.Black, 1);
        private Pen overlappingCirclesPen = new Pen(Color.Red, 1);
        private IEnumerable<UnityEngine.Vector3> overlappingCircles = Enumerable.Empty<UnityEngine.Vector3>();
        private static object circlesLockObj = new object();

        public SphereOverlapTestForm()
        {
            InitializeComponent();
        }

        private void Refresh(object sender, EventArgs e)
        {
            lock (circlesLockObj)
            {
                overlappingCircles = GetOverlappingCircles(overallRadiusTrackBar.Value, maxYRadiusTrackBar.Value, 1f - minOverlapTrackBar.Value / 100.0f, minCirclesPerRingTrackBar.Value);
                totalCirclesValueLabel.Text = overlappingCircles.Count().ToString();
            }

            XYPanel.Refresh();
            XZPanel.Refresh();
        }

        private void XYPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XYPanel.Size.Width / 2, XYPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawCircle(g, overallCirclePen, center.X, center.Y, overallRadiusTrackBar.Value);
            lock (circlesLockObj)
            {
                DrawCirclesFromOrigin(g, overlappingCirclesPen, center, overlappingCircles, maxYRadiusTrackBar.Value, Plane.XY);
            }
        }

        private void XZPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XZPanel.Size.Width / 2, XZPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawCircle(g, overallCirclePen, center.X, center.Y, overallRadiusTrackBar.Value);
            lock (circlesLockObj)
            {
                DrawCirclesFromOrigin(g, overlappingCirclesPen, center, overlappingCircles, maxYRadiusTrackBar.Value, Plane.XZ);
            }
        }

        private IEnumerable<UnityEngine.Vector3> GetOverlappingCircles(float overallRadius, float maxYRadius, float minOverlap, int minCirclesPerRing)
        {
            if (overallRadius <= maxYRadius)
            {
                return Enumerable.Empty<UnityEngine.Vector3>();
            }
            
            List<UnityEngine.Vector3> circles = new List<UnityEngine.Vector3>
            {
                new UnityEngine.Vector3(0, 0, 0)
            };

            int rings = (int)Math.Max(Math.Ceiling((overallRadius - 3 * maxYRadius) / (2 * maxYRadius * minOverlap)), 1);
            float ringStepSize = Math.Max(overallRadius - 3 * maxYRadius, maxYRadius) / rings;
            float minRad = Math.Min(maxYRadius, overallRadius - maxYRadius);
            for (float ringRad = overallRadius - maxYRadius; ringRad >= minRad; ringRad -= ringStepSize)
            {
                int ringCircles = (int)Math.Max(Math.Ceiling(Math.PI * 2 / minCirclesPerRing * ringRad / (2 * maxYRadius * minOverlap)), 1) * minCirclesPerRing;
                float ringAngStepSize = (float)Math.PI * 2 / ringCircles;
                
                for (float ringAng = 0; ringAng < Math.PI * 2; ringAng += ringAngStepSize)
                {
                    circles.Add(new UnityEngine.Vector3(0 + (float)Math.Sin(ringAng) * ringRad, 0, 0 + (float)Math.Cos(ringAng) * ringRad));
                }
            }

            return circles;
        }

        private void DrawCirclesFromOrigin(Graphics g, Pen pen, Point origin, IEnumerable<UnityEngine.Vector3> circles, float radius, Plane plane)
        {
            foreach (UnityEngine.Vector3 circle in circles)
            {
                DrawCircleFromOrigin(g, pen, origin, circle, radius, plane);
            }
        }

        private void DrawCircleFromOrigin(Graphics g, Pen pen, Point origin, UnityEngine.Vector3 circle, float radius, Plane plane)
        {
            Point circleCenterPoint;
            switch (plane)
            {
                case Plane.XZ:
                case Plane.YZ:
                    circleCenterPoint = new Point(origin.X + (int)circle.x, origin.Y + (int)circle.z);
                    break;
                case Plane.XY:
                    circleCenterPoint = new Point(origin.X + (int)circle.x, origin.Y + (int)circle.y);
                    break;
                default:
                    throw new ArgumentException("Invalid plane: " + plane, "plane");
            }

            DrawCircle(g, pen, circleCenterPoint, radius);
        }

        private void DrawCircle(Graphics g, Pen pen, Point center, float radius)
        {
            DrawEllipse(g, pen, center.X, center.Y, radius, radius);
        }

        private void DrawCircle(Graphics g, Pen pen, int centerX, int centerY, float radius)
        {
            DrawEllipse(g, pen, centerX, centerY, radius, radius);
        }

        private void DrawEllipse(Graphics g, Pen pen, int centerX, int centerY, float xRadius, float yRadius)
        {
            g.DrawEllipse(pen, centerX - xRadius, centerY -  yRadius, xRadius * 2, yRadius * 2);
        }
    }
}
