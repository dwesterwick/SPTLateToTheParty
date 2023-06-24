namespace SphereOverlapTest
{
    partial class SphereOverlapTestForm
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
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.renderingSplitContainer = new System.Windows.Forms.SplitContainer();
            this.XYPanel = new System.Windows.Forms.Panel();
            this.XZPanel = new System.Windows.Forms.Panel();
            this.controlsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.overallRadiusLabel = new System.Windows.Forms.Label();
            this.overallRadiusTrackBar = new System.Windows.Forms.TrackBar();
            this.mainTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.renderingSplitContainer)).BeginInit();
            this.renderingSplitContainer.Panel1.SuspendLayout();
            this.renderingSplitContainer.Panel2.SuspendLayout();
            this.renderingSplitContainer.SuspendLayout();
            this.controlsTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overallRadiusTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 2;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.Controls.Add(this.renderingSplitContainer, 1, 0);
            this.mainTableLayoutPanel.Controls.Add(this.controlsTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 1;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(884, 561);
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // renderingSplitContainer
            // 
            this.renderingSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.renderingSplitContainer.Location = new System.Drawing.Point(303, 3);
            this.renderingSplitContainer.Name = "renderingSplitContainer";
            this.renderingSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // renderingSplitContainer.Panel1
            // 
            this.renderingSplitContainer.Panel1.Controls.Add(this.XYPanel);
            // 
            // renderingSplitContainer.Panel2
            // 
            this.renderingSplitContainer.Panel2.Controls.Add(this.XZPanel);
            this.renderingSplitContainer.Size = new System.Drawing.Size(578, 555);
            this.renderingSplitContainer.SplitterDistance = 205;
            this.renderingSplitContainer.TabIndex = 0;
            // 
            // XYPanel
            // 
            this.XYPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.XYPanel.Location = new System.Drawing.Point(0, 0);
            this.XYPanel.Name = "XYPanel";
            this.XYPanel.Size = new System.Drawing.Size(578, 205);
            this.XYPanel.TabIndex = 0;
            this.XYPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.XYPanelPaint);
            // 
            // XZPanel
            // 
            this.XZPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.XZPanel.Location = new System.Drawing.Point(0, 0);
            this.XZPanel.Name = "XZPanel";
            this.XZPanel.Size = new System.Drawing.Size(578, 346);
            this.XZPanel.TabIndex = 0;
            this.XZPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.XZPanelPaint);
            // 
            // controlsTableLayoutPanel
            // 
            this.controlsTableLayoutPanel.ColumnCount = 2;
            this.controlsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 29.93197F));
            this.controlsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70.06802F));
            this.controlsTableLayoutPanel.Controls.Add(this.overallRadiusLabel, 0, 0);
            this.controlsTableLayoutPanel.Controls.Add(this.overallRadiusTrackBar, 1, 0);
            this.controlsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlsTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.controlsTableLayoutPanel.Name = "controlsTableLayoutPanel";
            this.controlsTableLayoutPanel.RowCount = 3;
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.controlsTableLayoutPanel.Size = new System.Drawing.Size(294, 555);
            this.controlsTableLayoutPanel.TabIndex = 1;
            // 
            // overallRadiusLabel
            // 
            this.overallRadiusLabel.AutoSize = true;
            this.overallRadiusLabel.Location = new System.Drawing.Point(3, 0);
            this.overallRadiusLabel.Name = "overallRadiusLabel";
            this.overallRadiusLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.overallRadiusLabel.Size = new System.Drawing.Size(76, 19);
            this.overallRadiusLabel.TabIndex = 0;
            this.overallRadiusLabel.Text = "Overall Radius";
            // 
            // overallRadiusTrackBar
            // 
            this.overallRadiusTrackBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.overallRadiusTrackBar.Location = new System.Drawing.Point(91, 3);
            this.overallRadiusTrackBar.Maximum = 100;
            this.overallRadiusTrackBar.Minimum = 10;
            this.overallRadiusTrackBar.Name = "overallRadiusTrackBar";
            this.overallRadiusTrackBar.Size = new System.Drawing.Size(200, 20);
            this.overallRadiusTrackBar.TabIndex = 1;
            this.overallRadiusTrackBar.TickFrequency = 5;
            this.overallRadiusTrackBar.Value = 10;
            this.overallRadiusTrackBar.ValueChanged += new System.EventHandler(this.Refresh);
            // 
            // SphereOverlapTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 561);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Name = "SphereOverlapTestForm";
            this.Text = "Sphere Overlap Test";
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.renderingSplitContainer.Panel1.ResumeLayout(false);
            this.renderingSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.renderingSplitContainer)).EndInit();
            this.renderingSplitContainer.ResumeLayout(false);
            this.controlsTableLayoutPanel.ResumeLayout(false);
            this.controlsTableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overallRadiusTrackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.SplitContainer renderingSplitContainer;
        private System.Windows.Forms.Panel XYPanel;
        private System.Windows.Forms.Panel XZPanel;
        private System.Windows.Forms.TableLayoutPanel controlsTableLayoutPanel;
        private System.Windows.Forms.Label overallRadiusLabel;
        private System.Windows.Forms.TrackBar overallRadiusTrackBar;
    }
}

