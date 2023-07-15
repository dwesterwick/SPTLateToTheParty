using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LTTPConfigEditor
{
    public partial class LTTPConfigEditorForm : Form
    {
        private LateToTheParty.Configuration.ModConfig Config;

        public LTTPConfigEditorForm()
        {
            InitializeComponent();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            if (openConfigDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = File.ReadAllText(openConfigDialog.FileName);
                    LateToTheParty.Configuration.ModConfig _config = Newtonsoft.Json.JsonConvert.DeserializeObject<LateToTheParty.Configuration.ModConfig>(json);
                    Config = _config;

                    saveToolStripButton.Enabled = true;
                    openToolStripButton.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error when Reading Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {

        }

        private void LTTPConfigEditorFormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("You have unsaved changes. Are you sure you want to quit?", "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
