using LookRankingDataReader.Models;
using Newtonsoft.Json;
using System.Data;

namespace LookRankingDataReader
{
    public partial class LootRankingDataForm : Form
    {
        private static LootRankingContainer? lootRankingContainer;

        public LootRankingDataForm()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openLootRankingDataDialog.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(openLootRankingDataDialog.FileName);
                lootRankingContainer = JsonConvert.DeserializeObject<LootRankingContainer>(json);
                updateLootRankingData();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void updateLootRankingData()
        {
            lootRankingDataGridView.DataSource = null;

            if ((lootRankingContainer == null) || (lootRankingContainer.Items.Count == 0))
            {
                return;
            }

            lootRankingDataGridView.SuspendLayout();

            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Value", typeof(double));
            dt.Columns.Add("Cost Per Slot", typeof(double));
            dt.Columns.Add("Weight", typeof(string));
            dt.Columns.Add("Size", typeof(string));
            dt.Columns.Add("Max Dimension", typeof(string));

            foreach (LootRankingData item in lootRankingContainer.Items.Values)
            {
                DataRow row = dt.NewRow();
                row["ID"] = item.ID;
                row["Name"] = item.Name;
                row["Value"] = item.Value;
                row["Cost Per Slot"] = item.CostPerSlot;
                row["Weight"] = item.Weight;
                row["Size"] = item.Size;
                row["Max Dimension"] = item.MaxDim;
                dt.Rows.Add(row);
            }

            lootRankingDataGridView.DataSource = dt;

            lootRankingDataGridView.ResumeLayout();
        }
    }
}