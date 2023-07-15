using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LTTPConfigEditor
{
    public class BreadCrumbControl: FlowLayoutPanel
    {
        private Dictionary<string, EventHandler> linkEvents = new Dictionary<string, EventHandler>();

        public BreadCrumbControl()
        {

        }

        public BreadCrumbControl(Dictionary<string, EventHandler> linksAndEvents) : this()
        {
            linkEvents = linksAndEvents;
            RebuildAllBreadCrumbs();
        }

        public void RemoveAllBreadCrumbs()
        {
            UnlinkAllBreadCrumbs();
            linkEvents.Clear();
        }

        public void AddBreadCrumb(string text, EventHandler action)
        {
            if (linkEvents.ContainsKey(text))
            {
                throw new InvalidOperationException("There is already a bread crumb with that name.");
            }

            linkEvents.Add(text, action);

            if (linkEvents.Count > 1)
            {
                this.Controls.Add(GetSeparator());
            }
            this.Controls.Add(CreateBreadCrumb(text, action));
        }

        private static Control CreateBreadCrumb(string text, EventHandler action)
        {
            LinkLabel label = new LinkLabel();
            label.Name = text;
            label.Text = text;
            Size labelSize = TextRenderer.MeasureText(label.Text, label.Font);
            label.Width = labelSize.Width;

            if (action != null)
            {
                label.Click += action;
            }

            return label;
        }

        private static Control GetSeparator()
        {
            Label separator = new Label();
            separator.Text = ">";
            Size separatorSize = TextRenderer.MeasureText(separator.Text, separator.Font);
            separator.Width = separatorSize.Width;

            return separator;
        }

        private void RebuildAllBreadCrumbs()
        {
            UnlinkAllBreadCrumbs();

            string[] keys = linkEvents.Keys.ToArray();
            if (keys.Length == 0)
            {
                return;
            }

            this.Controls.Add(CreateBreadCrumb(keys[0], linkEvents[keys[0]]));
            for (int key = 1; key < keys.Length; key++)
            {
                this.Controls.Add(GetSeparator());
                this.Controls.Add(CreateBreadCrumb(keys[key], linkEvents[keys[key]]));
            }
        }

        private void UnlinkAllBreadCrumbs()
        {
            foreach (Control control in this.Controls)
            {
                UnlinkBreadCrumb(control.Name);
            }
            this.Controls.Clear();
        }

        private void UnlinkBreadCrumb(string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            this.Controls[text].Click -= linkEvents[text];
        }
    }
}
