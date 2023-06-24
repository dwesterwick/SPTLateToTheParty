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
    public partial class SphereOverlapTestForm : Form
    {
        private Pen overallCirclePen = new Pen(Color.Black, 1);

        public SphereOverlapTestForm()
        {
            InitializeComponent();
        }

        private void Refresh(object sender, EventArgs e)
        {
            XYPanel.Refresh();
            XZPanel.Refresh();
        }

        private void XYPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XYPanel.Size.Width / 2, XYPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawCircleFromOrigin(g, overallCirclePen, center.X, center.Y, overallRadiusTrackBar.Value);
        }

        private void XZPanelPaint(object sender, PaintEventArgs e)
        {
            Point center = new Point(XZPanel.Size.Width / 2, XZPanel.Size.Height / 2);

            Graphics g = e.Graphics;
            g.Clear(Color.White);
            DrawCircleFromOrigin(g, overallCirclePen, center.X, center.Y, overallRadiusTrackBar.Value);
        }

        private void DrawCircleFromOrigin(Graphics g, Pen pen, int x, int y, float radius)
        {
            DrawEllipseFromOrigin(g, pen, x, y, radius, radius);
        }

        private void DrawEllipseFromOrigin(Graphics g, Pen pen, int x, int y, float xRadius, float yRadius)
        {
            g.DrawEllipse(pen, x - xRadius, y -  yRadius, xRadius * 2, yRadius * 2);
        }
    }
}
