using System;
using System.Drawing;

namespace SioForgeCAD.Forms
{
    partial class ComboboxDialog
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.StripContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SelectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DeselectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.ValidateButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.headerCheckBox = new System.Windows.Forms.CheckBox();
            this.dummy = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.StripContextMenu.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dataGridView1.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.ColumnHeadersHeight = 35;
            this.dataGridView1.ContextMenuStrip = this.StripContextMenu;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(240)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.EnableHeadersVisualStyles = false;
            this.dataGridView1.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.dataGridView1.Location = new System.Drawing.Point(0, 35);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(700, 445);
            this.dataGridView1.TabIndex = 0;
            // 
            // StripContextMenu
            // 
            this.StripContextMenu.BackColor = System.Drawing.Color.White;
            this.StripContextMenu.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.StripContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SelectToolStripMenuItem,
            this.DeselectToolStripMenuItem});
            this.StripContextMenu.Name = "StripContextMenu";
            this.StripContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.StripContextMenu.ShowImageMargin = false;
            this.StripContextMenu.Size = new System.Drawing.Size(128, 48);
            // 
            // SelectToolStripMenuItem
            // 
            this.SelectToolStripMenuItem.Name = "SelectToolStripMenuItem";
            this.SelectToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.SelectToolStripMenuItem.Text = "Sélectionner";
            this.SelectToolStripMenuItem.Click += new System.EventHandler(this.SelectToolStripMenuItem_Click);
            // 
            // DeselectToolStripMenuItem
            // 
            this.DeselectToolStripMenuItem.Name = "DeselectToolStripMenuItem";
            this.DeselectToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.DeselectToolStripMenuItem.Text = "Déselectionner";
            this.DeselectToolStripMenuItem.Click += new System.EventHandler(this.DeselectToolStripMenuItem_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.AllowMerge = false;
            this.toolStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
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
            this.toolStrip1.Size = new System.Drawing.Size(700, 35);
            this.toolStrip1.TabIndex = 1;
            // 
            // ValidateButton
            // 
            this.ValidateButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ValidateButton.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.ValidateButton.Image = global::SioForgeCAD.Properties.Resources.Form_Validate;
            this.ValidateButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ValidateButton.Name = "ValidateButton";
            this.ValidateButton.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.ValidateButton.Size = new System.Drawing.Size(92, 32);
            this.ValidateButton.Text = "Valider";
            this.ValidateButton.Click += new System.EventHandler(this.ValidateButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 35);
            // 
            // headerCheckBox
            // 
            this.headerCheckBox.BackColor = System.Drawing.Color.White;
            this.headerCheckBox.Location = new System.Drawing.Point(13, 12);
            this.headerCheckBox.Name = "headerCheckBox";
            this.headerCheckBox.Size = new System.Drawing.Size(15, 15);
            this.headerCheckBox.TabIndex = 0;
            this.headerCheckBox.UseVisualStyleBackColor = false;
            // 
            // dummy
            // 
            this.dummy.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.dummy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dummy.Location = new System.Drawing.Point(0, 0);
            this.dummy.Margin = new System.Windows.Forms.Padding(0);
            this.dummy.Name = "dummy";
            this.dummy.Size = new System.Drawing.Size(10, 10);
            this.dummy.TabIndex = 0;
            this.dummy.UseVisualStyleBackColor = true;
            this.dummy.Visible = false;
            // 
            // ComboboxDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(700, 480);
            this.Controls.Add(this.dummy);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "ComboboxDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sélection d\'éléments";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.StripContextMenu.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton ValidateButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.CheckBox headerCheckBox;

        private System.Windows.Forms.ContextMenuStrip StripContextMenu;
        private System.Windows.Forms.ToolStripMenuItem DeselectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SelectToolStripMenuItem;
        private System.Windows.Forms.Button dummy;
    }
}