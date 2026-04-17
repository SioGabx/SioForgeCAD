using System;
using static SioForgeCAD.Functions.VEGBLOC;

namespace SioForgeCAD.Forms
{
    partial class VegblocDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.DataGrid = new System.Windows.Forms.DataGridView();
            this.NAME = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HEIGHT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WIDTH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TYPE = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.ValidateButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ContentPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.DataGrid)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.ContentPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // DataGrid
            // 
            this.DataGrid.AllowUserToResizeRows = false;
            this.DataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.DataGrid.BackgroundColor = System.Drawing.Color.White;
            this.DataGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DataGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Single; // Retour au quadrillage complet (horizontal + vertical)
            this.DataGrid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single; // Lignes verticales aussi dans l'en-tête

            // Style de l'en-tête
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            this.DataGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.DataGrid.ColumnHeadersHeight = 35;
            this.DataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.DataGrid.EnableHeadersVisualStyles = false;

            this.DataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.NAME,
            this.HEIGHT,
            this.WIDTH,
            this.TYPE});

            // Style des cellules
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(240)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;

            this.DataGrid.DefaultCellStyle = dataGridViewCellStyle2;
            this.DataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataGrid.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220))))); // Gris clair pour que le quadrillage soit doux mais visible
            this.DataGrid.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.DataGrid.Location = new System.Drawing.Point(0, 0);
            this.DataGrid.Margin = new System.Windows.Forms.Padding(2);
            this.DataGrid.Name = "DataGrid";
            this.DataGrid.RowHeadersVisible = false;
            this.DataGrid.RowTemplate.Height = 32;
            this.DataGrid.RowTemplate.MinimumHeight = 32; 
            this.DataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.DataGrid.Size = new System.Drawing.Size(700, 423);
            this.DataGrid.TabIndex = 0;
            this.DataGrid.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.DataGrid_CellBeginEdit);
            this.DataGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGrid_CellEndEdit);
            this.DataGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DataGrid_KeyDown);
            // 
            // NAME
            // 
            this.NAME.FillWeight = 200F;
            this.NAME.HeaderText = "Nom latin \'Cultivar\'";
            this.NAME.MinimumWidth = 150;
            this.NAME.Name = "NAME";
            // 
            // HEIGHT
            // 
            this.HEIGHT.FillWeight = 80F;
            this.HEIGHT.HeaderText = "Hauteur";
            this.HEIGHT.MinimumWidth = 60;
            this.HEIGHT.Name = "HEIGHT";
            // 
            // WIDTH
            // 
            this.WIDTH.FillWeight = 80F;
            this.WIDTH.HeaderText = "Largeur";
            this.WIDTH.MinimumWidth = 60;
            this.WIDTH.Name = "WIDTH";
            // 
            // TYPE
            // 
            this.TYPE.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.TYPE.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.TYPE.FillWeight = 100F;
            this.TYPE.HeaderText = "Type";
            this.TYPE.Items.AddRange(Enum.GetNames(typeof(VegblocTypes)));
            this.TYPE.MinimumWidth = 100;
            this.TYPE.Name = "TYPE";
            // 
            // toolStrip1
            // 
            this.toolStrip1.AllowMerge = false;
            this.toolStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240))))); // Bandeau gris clair pour mieux le démarquer
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
            this.toolStrip1.Size = new System.Drawing.Size(700, 35); // Hauteur légèrement augmentée pour faire "ruban"
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // ValidateButton
            // 
            this.ValidateButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ValidateButton.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold); // Bouton Valider plus lisible
            this.ValidateButton.Image = global::SioForgeCAD.Properties.Resources.Form_Validate;
            this.ValidateButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ValidateButton.Name = "ValidateButton";
            this.ValidateButton.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4); // Bouton un peu plus "gras"
            this.ValidateButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ValidateButton.Size = new System.Drawing.Size(91, 32);
            this.ValidateButton.Text = "Valider";
            this.ValidateButton.Click += new System.EventHandler(this.ValidateButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator1.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 35);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Controls.Add(this.DataGrid);
            this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContentPanel.Location = new System.Drawing.Point(0, 35);
            this.ContentPanel.Margin = new System.Windows.Forms.Padding(2);
            this.ContentPanel.Name = "ContentPanel";
            this.ContentPanel.Size = new System.Drawing.Size(700, 415);
            this.ContentPanel.TabIndex = 2;
            // 
            // VegblocDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(700, 450);
            this.Controls.Add(this.ContentPanel);
            this.Controls.Add(this.toolStrip1);
            this.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(500, 300);
            this.Name = "VegblocDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VEGBLOC - Création";
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