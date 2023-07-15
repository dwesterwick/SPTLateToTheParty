namespace LTTPConfigEditor
{
    partial class LTTPConfigEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LTTPConfigEditorForm));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.openConfigDialog = new System.Windows.Forms.OpenFileDialog();
            this.configTreeView = new System.Windows.Forms.TreeView();
            this.commonChangesGroupBox = new System.Windows.Forms.GroupBox();
            this.commonChangesFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.mainToolStrip.SuspendLayout();
            this.mainTableLayoutPanel.SuspendLayout();
            this.commonChangesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripButton,
            this.saveToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(884, 25);
            this.mainToolStrip.TabIndex = 0;
            this.mainToolStrip.Text = "toolStrip1";
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Enabled = false;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveToolStripButton_Click);
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 2;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.Controls.Add(this.configTreeView, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.commonChangesGroupBox, 0, 0);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 25);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 2;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(884, 436);
            this.mainTableLayoutPanel.TabIndex = 1;
            // 
            // openConfigDialog
            // 
            this.openConfigDialog.DefaultExt = "json";
            this.openConfigDialog.FileName = "config.json";
            this.openConfigDialog.Filter = "Late to the Party Configuration|config.json|All Files|*.*";
            this.openConfigDialog.Title = "Open Late to the Party Configuration";
            // 
            // configTreeView
            // 
            this.configTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configTreeView.Location = new System.Drawing.Point(3, 83);
            this.configTreeView.Name = "configTreeView";
            this.configTreeView.Size = new System.Drawing.Size(344, 350);
            this.configTreeView.TabIndex = 0;
            // 
            // commonChangesGroupBox
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.commonChangesGroupBox, 2);
            this.commonChangesGroupBox.Controls.Add(this.commonChangesFlowLayoutPanel);
            this.commonChangesGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commonChangesGroupBox.Location = new System.Drawing.Point(3, 3);
            this.commonChangesGroupBox.Name = "commonChangesGroupBox";
            this.commonChangesGroupBox.Size = new System.Drawing.Size(878, 74);
            this.commonChangesGroupBox.TabIndex = 1;
            this.commonChangesGroupBox.TabStop = false;
            this.commonChangesGroupBox.Text = "Common Changes";
            // 
            // commonChangesFlowLayoutPanel
            // 
            this.commonChangesFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commonChangesFlowLayoutPanel.Location = new System.Drawing.Point(3, 16);
            this.commonChangesFlowLayoutPanel.Name = "commonChangesFlowLayoutPanel";
            this.commonChangesFlowLayoutPanel.Size = new System.Drawing.Size(872, 55);
            this.commonChangesFlowLayoutPanel.TabIndex = 0;
            // 
            // LTTPConfigEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 461);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Controls.Add(this.mainToolStrip);
            this.Name = "LTTPConfigEditorForm";
            this.Text = "Late to the Party Config Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LTTPConfigEditorFormClosing);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.commonChangesGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.OpenFileDialog openConfigDialog;
        private System.Windows.Forms.TreeView configTreeView;
        private System.Windows.Forms.GroupBox commonChangesGroupBox;
        private System.Windows.Forms.FlowLayoutPanel commonChangesFlowLayoutPanel;
    }
}

