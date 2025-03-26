namespace SioForgeCAD.Forms
{
    partial class VegblocDialog
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
            this.DataGrid = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.ValidateButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ContentPanel = new System.Windows.Forms.Panel();
            this.NAME = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HEIGHT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WIDTH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TYPE = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.DataGrid)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.ContentPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // DataGrid
            // 
            this.DataGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.NAME,
            this.HEIGHT,
            this.WIDTH,
            this.TYPE});
            this.DataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataGrid.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.DataGrid.Location = new System.Drawing.Point(0, 0);
            this.DataGrid.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.DataGrid.Name = "DataGrid";
            this.DataGrid.RowHeadersWidth = 51;
            this.DataGrid.RowTemplate.Height = 24;
            this.DataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.DataGrid.Size = new System.Drawing.Size(507, 339);
            this.DataGrid.TabIndex = 0;
            this.DataGrid.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.DataGrid_CellBeginEdit);
            this.DataGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGrid_CellEndEdit);
            this.DataGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DataGrid_KeyDown);
            // 
            // toolStrip1
            // 
            this.toolStrip1.AllowMerge = false;
            this.toolStrip1.CanOverflow = false;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ValidateButton,
            this.toolStripSeparator1});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip1.ShowItemToolTips = false;
            this.toolStrip1.Size = new System.Drawing.Size(507, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // ValidateButton
            // 
            this.ValidateButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ValidateButton.Image = global::SioForgeCAD.Properties.Resources.VEGBLOC_Validate;
            this.ValidateButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ValidateButton.Name = "ValidateButton";
            this.ValidateButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.ValidateButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ValidateButton.Size = new System.Drawing.Size(86, 24);
            this.ValidateButton.Text = "Valider";
            this.ValidateButton.Click += new System.EventHandler(this.ValidateButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator1.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Controls.Add(this.DataGrid);
            this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContentPanel.Location = new System.Drawing.Point(0, 27);
            this.ContentPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.ContentPanel.Name = "ContentPanel";
            this.ContentPanel.Size = new System.Drawing.Size(507, 339);
            this.ContentPanel.TabIndex = 2;
            // 
            // NAME
            // 
            this.NAME.HeaderText = "Nom latin \'Cultivar\'";
            this.NAME.MinimumWidth = 6;
            this.NAME.Name = "NAME";
            this.NAME.Width = 325;
            // 
            // HEIGHT
            // 
            this.HEIGHT.HeaderText = "Hauteur";
            this.HEIGHT.MinimumWidth = 6;
            this.HEIGHT.Name = "HEIGHT";
            this.HEIGHT.Width = 125;
            // 
            // WIDTH
            // 
            this.WIDTH.HeaderText = "Largeur";
            this.WIDTH.MinimumWidth = 6;
            this.WIDTH.Name = "WIDTH";
            this.WIDTH.Width = 125;
            // 
            // TYPE
            // 
            this.TYPE.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.TYPE.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.TYPE.HeaderText = "Type";
            this.TYPE.Items.AddRange(new object[] {
            "ARBRES",
            "CÉPÉES",
            "ARBUSTES",
            "GRIMPANTES",
            "GRAMINÉES",
            "VIVACES",
            "BULBEUSES"});
            this.TYPE.MinimumWidth = 6;
            this.TYPE.Name = "TYPE";
            this.TYPE.Width = 150;
            // 
            // VegblocDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 366);
            this.Controls.Add(this.ContentPanel);
            this.Controls.Add(this.toolStrip1);
            this.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "VegblocDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VEGBLOC";
            this.Load += new System.EventHandler(this.VegblocDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DataGrid)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ContentPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView DataGrid;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton ValidateButton;
        private System.Windows.Forms.Panel ContentPanel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.DataGridViewTextBoxColumn NAME;
        private System.Windows.Forms.DataGridViewTextBoxColumn HEIGHT;
        private System.Windows.Forms.DataGridViewTextBoxColumn WIDTH;
        private System.Windows.Forms.DataGridViewComboBoxColumn TYPE;
    }
}