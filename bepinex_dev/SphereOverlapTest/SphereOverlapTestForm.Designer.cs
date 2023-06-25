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
            this.maxYRadiusLabel = new System.Windows.Forms.Label();
            this.maxYRadiusTrackBar = new System.Windows.Forms.TrackBar();
            this.minOverlapLabel = new System.Windows.Forms.Label();
            this.minOverlapTrackBar = new System.Windows.Forms.TrackBar();
            this.minCirclesPerRingLabel = new System.Windows.Forms.Label();
            this.minCirclesPerRingTrackBar = new System.Windows.Forms.TrackBar();
            this.totalCirclesLabel = new System.Windows.Forms.Label();
            this.totalCirclesValueLabel = new System.Windows.Forms.Label();
            this.mainTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.renderingSplitContainer)).BeginInit();
            this.renderingSplitContainer.Panel1.SuspendLayout();
            this.renderingSplitContainer.Panel2.SuspendLayout();
            this.renderingSplitContainer.SuspendLayout();
            this.controlsTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overallRadiusTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxYRadiusTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minOverlapTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minCirclesPerRingTrackBar)).BeginInit();
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
            this.controlsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.controlsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.controlsTableLayoutPanel.Controls.Add(this.totalCirclesValueLabel, 1, 4);
            this.controlsTableLayoutPanel.Controls.Add(this.overallRadiusLabel, 0, 0);
            this.controlsTableLayoutPanel.Controls.Add(this.overallRadiusTrackBar, 1, 0);
            this.controlsTableLayoutPanel.Controls.Add(this.maxYRadiusLabel, 0, 1);
            this.controlsTableLayoutPanel.Controls.Add(this.maxYRadiusTrackBar, 1, 1);
            this.controlsTableLayoutPanel.Controls.Add(this.minOverlapLabel, 0, 2);
            this.controlsTableLayoutPanel.Controls.Add(this.minOverlapTrackBar, 1, 2);
            this.controlsTableLayoutPanel.Controls.Add(this.minCirclesPerRingLabel, 0, 3);
            this.controlsTableLayoutPanel.Controls.Add(this.minCirclesPerRingTrackBar, 1, 3);
            this.controlsTableLayoutPanel.Controls.Add(this.totalCirclesLabel, 0, 4);
            this.controlsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlsTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.controlsTableLayoutPanel.Name = "controlsTableLayoutPanel";
            this.controlsTableLayoutPanel.RowCount = 7;
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.controlsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
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
            this.overallRadiusTrackBar.Location = new System.Drawing.Point(123, 3);
            this.overallRadiusTrackBar.Maximum = 200;
            this.overallRadiusTrackBar.Minimum = 10;
            this.overallRadiusTrackBar.Name = "overallRadiusTrackBar";
            this.overallRadiusTrackBar.Size = new System.Drawing.Size(171, 20);
            this.overallRadiusTrackBar.TabIndex = 1;
            this.overallRadiusTrackBar.TickFrequency = 5;
            this.overallRadiusTrackBar.Value = 10;
            this.overallRadiusTrackBar.ValueChanged += new System.EventHandler(this.Refresh);
            // 
            // maxYRadiusLabel
            // 
            this.maxYRadiusLabel.AutoSize = true;
            this.maxYRadiusLabel.Location = new System.Drawing.Point(3, 26);
            this.maxYRadiusLabel.Name = "maxYRadiusLabel";
            this.maxYRadiusLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.maxYRadiusLabel.Size = new System.Drawing.Size(73, 19);
            this.maxYRadiusLabel.TabIndex = 2;
            this.maxYRadiusLabel.Text = "Max Y Radius";
            // 
            // maxYRadiusTrackBar
            // 
            this.maxYRadiusTrackBar.Location = new System.Drawing.Point(123, 29);
            this.maxYRadiusTrackBar.Maximum = 200;
            this.maxYRadiusTrackBar.Minimum = 10;
            this.maxYRadiusTrackBar.Name = "maxYRadiusTrackBar";
            this.maxYRadiusTrackBar.Size = new System.Drawing.Size(171, 20);
            this.maxYRadiusTrackBar.TabIndex = 3;
            this.maxYRadiusTrackBar.TickFrequency = 5;
            this.maxYRadiusTrackBar.Value = 10;
            this.maxYRadiusTrackBar.ValueChanged += new System.EventHandler(this.Refresh);
            // 
            // minOverlapLabel
            // 
            this.minOverlapLabel.AutoSize = true;
            this.minOverlapLabel.Location = new System.Drawing.Point(3, 52);
            this.minOverlapLabel.Name = "minOverlapLabel";
            this.minOverlapLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.minOverlapLabel.Size = new System.Drawing.Size(81, 19);
            this.minOverlapLabel.TabIndex = 4;
            this.minOverlapLabel.Text = "Min Overlap (%)";
            // 
            // minOverlapTrackBar
            // 
            this.minOverlapTrackBar.Location = new System.Drawing.Point(123, 55);
            this.minOverlapTrackBar.Maximum = 90;
            this.minOverlapTrackBar.Name = "minOverlapTrackBar";
            this.minOverlapTrackBar.Size = new System.Drawing.Size(171, 20);
            this.minOverlapTrackBar.TabIndex = 5;
            this.minOverlapTrackBar.TickFrequency = 5;
            this.minOverlapTrackBar.ValueChanged += new System.EventHandler(this.Refresh);
            // 
            // minCirclesPerRingLabel
            // 
            this.minCirclesPerRingLabel.AutoSize = true;
            this.minCirclesPerRingLabel.Location = new System.Drawing.Point(3, 78);
            this.minCirclesPerRingLabel.Name = "minCirclesPerRingLabel";
            this.minCirclesPerRingLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.minCirclesPerRingLabel.Size = new System.Drawing.Size(101, 19);
            this.minCirclesPerRingLabel.TabIndex = 6;
            this.minCirclesPerRingLabel.Text = "Min Circles per Ring";
            // 
            // minCirclesPerRingTrackBar
            // 
            this.minCirclesPerRingTrackBar.Location = new System.Drawing.Point(123, 81);
            this.minCirclesPerRingTrackBar.Minimum = 1;
            this.minCirclesPerRingTrackBar.Name = "minCirclesPerRingTrackBar";
            this.minCirclesPerRingTrackBar.Size = new System.Drawing.Size(171, 20);
            this.minCirclesPerRingTrackBar.TabIndex = 7;
            this.minCirclesPerRingTrackBar.Value = 1;
            this.minCirclesPerRingTrackBar.ValueChanged += new System.EventHandler(this.Refresh);
            // 
            // totalCirclesLabel
            // 
            this.totalCirclesLabel.AutoSize = true;
            this.totalCirclesLabel.Location = new System.Drawing.Point(3, 104);
            this.totalCirclesLabel.Name = "totalCirclesLabel";
            this.totalCirclesLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.totalCirclesLabel.Size = new System.Drawing.Size(65, 19);
            this.totalCirclesLabel.TabIndex = 8;
            this.totalCirclesLabel.Text = "Total Circles";
            // 
            // totalCirclesValueLabel
            // 
            this.totalCirclesValueLabel.AutoSize = true;
            this.totalCirclesValueLabel.Location = new System.Drawing.Point(123, 104);
            this.totalCirclesValueLabel.Name = "totalCirclesValueLabel";
            this.totalCirclesValueLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.totalCirclesValueLabel.Size = new System.Drawing.Size(13, 19);
            this.totalCirclesValueLabel.TabIndex = 9;
            this.totalCirclesValueLabel.Text = "0";
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
            ((System.ComponentModel.ISupportInitialize)(this.maxYRadiusTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minOverlapTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minCirclesPerRingTrackBar)).EndInit();
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
        private System.Windows.Forms.Label maxYRadiusLabel;
        private System.Windows.Forms.TrackBar maxYRadiusTrackBar;
        private System.Windows.Forms.Label minOverlapLabel;
        private System.Windows.Forms.TrackBar minOverlapTrackBar;
        private System.Windows.Forms.Label minCirclesPerRingLabel;
        private System.Windows.Forms.TrackBar minCirclesPerRingTrackBar;
        private System.Windows.Forms.Label totalCirclesValueLabel;
        private System.Windows.Forms.Label totalCirclesLabel;
    }
}

