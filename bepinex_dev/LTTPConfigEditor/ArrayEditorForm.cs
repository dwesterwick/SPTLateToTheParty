using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LTTPConfigEditor
{
    public partial class ArrayEditorForm : Form
    {
        public Type ArrayType { get; }
        public object ArrayObject { get; private set; }

        private Array array
        {
            get { return ArrayObject as Array; }
            set { ArrayObject = value; }
        }

        public ArrayEditorForm(Type _arrayType, object _arrayObj)
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;

            this.ArrayType = _arrayType;
            this.ArrayObject = _arrayObj;

            buildDataGridView();
        }

        public int GetJaggedDimensions()
        {
            return ArrayType.Name.Count((c) => c == '[');
        }

        public void buildDataGridView()
        {
            int[] indices = new int[array.Rank];
            for (int d = 0; d < array.Rank; d++)
            {
                int rows = array.GetLength(d);
                for (int r = 0; r < rows; r++)
                {
                    indices[d] = r;
                    object val = array.GetValue(indices);

                    int cols = 1;
                    if (val.GetType().IsArray)
                    {
                        Array innerArray = val as Array;
                        cols = innerArray.GetLength(0);
                    }

                    while (arrayDataGridView.Columns.Count < cols)
                    {
                        DataGridViewTextBoxColumn newCol = new DataGridViewTextBoxColumn();
                        arrayDataGridView.Columns.Add(newCol);
                    }
                }
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
