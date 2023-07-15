using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LTTPConfigEditor
{
    public partial class LTTPConfigEditorForm : Form
    {
        private LateToTheParty.Configuration.ModConfig modConfig;
        private ModPackageConfig modPackage;

        private BreadCrumbControl breadCrumbControl;
        private Dictionary<TreeNode, Type> configTypes = new Dictionary<TreeNode, Type>();

        public LTTPConfigEditorForm()
        {
            InitializeComponent();

            breadCrumbControl = new BreadCrumbControl();
            breadCrumbControl.Dock = DockStyle.Fill;
            nodePropsTableLayoutPanel.Controls.Add(breadCrumbControl, 0, 0);
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            if (openConfigDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string packagePath = openConfigDialog.FileName.Substring(0, openConfigDialog.FileName.LastIndexOf('\\')) + "\\..\\package.json";
                    modPackage = LoadConfig<ModPackageConfig>(packagePath);

                    if (!IsModVersionCompatible(new Version(modPackage.Version)))
                    {
                        throw new InvalidOperationException("The selected configuration file is for a version of the LTTP mod that is incompatible with this version of the editor.");
                    }

                    modConfig = LoadConfig<LateToTheParty.Configuration.ModConfig>(openConfigDialog.FileName);
                    configTypes.Clear();
                    configTreeView.Nodes.AddRange(CreateTreeNodesForType(modConfig.GetType(), modConfig));

                    saveToolStripButton.Enabled = true;
                    openToolStripButton.Enabled = false;
                    loadTemplateButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error when Reading Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                SaveConfig(openConfigDialog.FileName, modConfig);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error when Saving Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadTemplateButton_Click(object sender, EventArgs e)
        {

        }

        private void LTTPConfigEditorFormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("You have unsaved changes. Are you sure you want to quit?", "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void ConfigNodeSelected(object sender, TreeViewEventArgs e)
        {
            Action callbackAction = new Action(() => {
                ConfigNodeSelected(configTreeView, new TreeViewEventArgs(configTreeView.SelectedNode));
            });

            BreadCrumbControl.UpdateBreadCrumbControlForTreeView(breadCrumbControl, configTreeView, e.Node, callbackAction);
        }

        private T LoadConfig<T>(string filename)
        {
            string json = File.ReadAllText(filename);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        private void SaveConfig<T>(string filename, T obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filename, json);
        }

        private bool IsModVersionCompatible(Version modVersion)
        {
            if (modVersion.CompareTo(LateToTheParty.Controllers.ConfigController.MinVersion) < 0)
            {
                return false;
            }

            if (modVersion.CompareTo(LateToTheParty.Controllers.ConfigController.MaxVersion) < 0)
            {
                return false;
            }

            return true;
        }

        private TreeNode[] CreateTreeNodesForType(Type type, object obj)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            PropertyInfo[] props = type.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                TreeNode node = new TreeNode(prop.Name);
                Type propType = prop.PropertyType;

                if
                (
                    !propType.IsArray
                    && (propType != typeof(string))
                    && !(propType.IsGenericType && (propType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                )
                {
                    node.Nodes.AddRange(CreateTreeNodesForType(propType, prop.GetValue(obj, null)));
                }

                if (propType.IsGenericType && (propType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    IDictionary dict = prop.GetValue(obj, null) as IDictionary;
                    Type valueType = propType.GetGenericArguments()[1];
                    foreach(DictionaryEntry entry in dict)
                    {
                        TreeNode entryNode = new TreeNode(entry.Key.ToString());
                        node.Nodes.Add(entryNode);
                        entryNode.Nodes.AddRange(CreateTreeNodesForType(entry.Value.GetType(), entry.Value));
                    }
                }

                nodes.Add(node);
                configTypes.Add(node, propType);
            }

            return nodes.ToArray();
        }
    }
}
