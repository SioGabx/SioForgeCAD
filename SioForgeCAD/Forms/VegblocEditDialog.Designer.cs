using System;
using static SioForgeCAD.Functions.VEGBLOC;

namespace SioForgeCAD.Forms
{
    partial class VegblocEditDialog
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
            this.MainTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.NameLabel = new System.Windows.Forms.Label();
            this.NameInput = new System.Windows.Forms.TextBox();
            this.DimensionsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.HeightLabel = new System.Windows.Forms.Label();
            this.WidthLabel = new System.Windows.Forms.Label();
            this.HeightInput = new System.Windows.Forms.TextBox();
            this.WidthInput = new System.Windows.Forms.TextBox();
            this.TypeLabel = new System.Windows.Forms.Label();
            this.TypeInput = new System.Windows.Forms.ComboBox();
            this.ColorLabel = new System.Windows.Forms.Label();
            this.ColorFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.ColorPreviewPanel = new System.Windows.Forms.Panel();
            this.ColorSelectButton = new System.Windows.Forms.Button();
            this.ButtonFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.PromptCancelButton = new System.Windows.Forms.Button();
            this.PromptAcceptButton = new System.Windows.Forms.Button();

            this.MainTableLayout.SuspendLayout();
            this.DimensionsLayout.SuspendLayout();
            this.ColorFlowLayout.SuspendLayout();
            this.ButtonFlowLayout.SuspendLayout();
            this.SuspendLayout();

            // 
            // MainTableLayout
            // 
            this.MainTableLayout.AutoSize = true;
            this.MainTableLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MainTableLayout.ColumnCount = 1;
            this.MainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTableLayout.Controls.Add(this.NameLabel, 0, 0);
            this.MainTableLayout.Controls.Add(this.NameInput, 0, 1);
            this.MainTableLayout.Controls.Add(this.DimensionsLayout, 0, 2);
            this.MainTableLayout.Controls.Add(this.TypeLabel, 0, 3);
            this.MainTableLayout.Controls.Add(this.TypeInput, 0, 4);
            this.MainTableLayout.Controls.Add(this.ColorLabel, 0, 5);
            this.MainTableLayout.Controls.Add(this.ColorFlowLayout, 0, 6);
            this.MainTableLayout.Controls.Add(this.ButtonFlowLayout, 0, 7);
            this.MainTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTableLayout.Location = new System.Drawing.Point(20, 20);
            this.MainTableLayout.Name = "MainTableLayout";
            this.MainTableLayout.RowCount = 8;
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.Size = new System.Drawing.Size(410, 280);
            this.MainTableLayout.TabIndex = 0;

            // 
            // NameLabel & NameInput
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.NameLabel.Location = new System.Drawing.Point(3, 0);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(111, 15);
            this.NameLabel.Text = "Nom latin 'Cultivar' :";

            this.NameInput.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.NameInput.Margin = new System.Windows.Forms.Padding(3, 2, 3, 15);
            this.NameInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NameInput.Name = "NameInput";

            // 
            // DimensionsLayout (Grille pour Hauteur/Largeur)
            // 
            this.DimensionsLayout.AutoSize = true;
            this.DimensionsLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.DimensionsLayout.ColumnCount = 2;
            this.DimensionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.DimensionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.DimensionsLayout.Controls.Add(this.HeightLabel, 0, 0);
            this.DimensionsLayout.Controls.Add(this.WidthLabel, 1, 0);
            this.DimensionsLayout.Controls.Add(this.HeightInput, 0, 1);
            this.DimensionsLayout.Controls.Add(this.WidthInput, 1, 1);
            this.DimensionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DimensionsLayout.Location = new System.Drawing.Point(0, 60);
            this.DimensionsLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
            this.DimensionsLayout.Name = "DimensionsLayout";
            this.DimensionsLayout.RowCount = 2;
            this.DimensionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.DimensionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.DimensionsLayout.Size = new System.Drawing.Size(410, 50);

            this.HeightLabel.AutoSize = true;
            this.HeightLabel.Text = "Hauteur :";
            this.HeightLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.HeightLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);

            this.WidthLabel.AutoSize = true;
            this.WidthLabel.Text = "Largeur (diamètre) :";
            this.WidthLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.WidthLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);

            this.HeightInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HeightInput.Font = new System.Drawing.Font("Segoe UI", 9.75F);

            this.WidthInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WidthInput.Font = new System.Drawing.Font("Segoe UI", 9.75F);

            // 
            // TypeLabel & TypeInput
            // 
            this.TypeLabel.AutoSize = true;
            this.TypeLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.TypeLabel.Text = "Type de végétal :";

            this.TypeInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeInput.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.TypeInput.Margin = new System.Windows.Forms.Padding(3, 2, 3, 15);
            this.TypeInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TypeInput.Items.AddRange(Enum.GetNames(typeof(VegblocTypes)));

            // 
            // ColorLabel & ColorFlow
            // 
            this.ColorLabel.AutoSize = true;
            this.ColorLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.ColorLabel.Text = "Couleur du calque :";

            this.ColorFlowLayout.AutoSize = true;
            this.ColorFlowLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ColorFlowLayout.Controls.Add(this.ColorPreviewPanel);
            this.ColorFlowLayout.Controls.Add(this.ColorSelectButton);
            this.ColorFlowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ColorFlowLayout.Location = new System.Drawing.Point(0, 190);
            this.ColorFlowLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
            this.ColorFlowLayout.Name = "ColorFlowLayout";
            this.ColorFlowLayout.Size = new System.Drawing.Size(410, 32);

            this.ColorPreviewPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ColorPreviewPanel.Size = new System.Drawing.Size(60, 28);
            this.ColorPreviewPanel.Margin = new System.Windows.Forms.Padding(3, 2, 10, 0);
            this.ColorPreviewPanel.Click += new System.EventHandler(this.ColorSelectButton_Click);

            this.ColorSelectButton.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.ColorSelectButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ColorSelectButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.ColorSelectButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ColorSelectButton.Size = new System.Drawing.Size(100, 28);
            this.ColorSelectButton.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.ColorSelectButton.Text = "Choisir...";
            this.ColorSelectButton.UseVisualStyleBackColor = false;
            this.ColorSelectButton.Click += new System.EventHandler(this.ColorSelectButton_Click);

            // 
            // ButtonFlowLayout
            // 
            this.ButtonFlowLayout.AutoSize = true;
            this.ButtonFlowLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ButtonFlowLayout.Controls.Add(this.PromptCancelButton);
            this.ButtonFlowLayout.Controls.Add(this.PromptAcceptButton);
            this.ButtonFlowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtonFlowLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.ButtonFlowLayout.Location = new System.Drawing.Point(0, 245);
            this.ButtonFlowLayout.Margin = new System.Windows.Forms.Padding(0);
            this.ButtonFlowLayout.Name = "ButtonFlowLayout";
            this.ButtonFlowLayout.Size = new System.Drawing.Size(410, 35);

            // Button Cancel
            this.PromptCancelButton.BackColor = System.Drawing.Color.White;
            this.PromptCancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PromptCancelButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(204, 204, 204);
            this.PromptCancelButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.PromptCancelButton.Size = new System.Drawing.Size(90, 32);
            this.PromptCancelButton.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.PromptCancelButton.Text = "Annuler";
            this.PromptCancelButton.Click += new System.EventHandler(this.PromptCancelButton_Click);

            // Button OK
            this.PromptAcceptButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.PromptAcceptButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PromptAcceptButton.FlatAppearance.BorderSize = 0;
            this.PromptAcceptButton.ForeColor = System.Drawing.Color.White;
            this.PromptAcceptButton.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.PromptAcceptButton.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.PromptAcceptButton.Size = new System.Drawing.Size(90, 32);
            this.PromptAcceptButton.Text = "Enregistrer";
            this.PromptAcceptButton.Click += new System.EventHandler(this.PromptAcceptButton_Click);

            // 
            // VegblocEditDialog (Form)
            // 
            this.AcceptButton = this.PromptAcceptButton;
            this.CancelButton = this.PromptCancelButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            // AutoSize sur la fenêtre principale pour gérer le ratio automatiquement
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MinimumSize = new System.Drawing.Size(450, 0);

            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(450, 320);
            this.Controls.Add(this.MainTableLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VegblocEditDialog";
            this.Padding = new System.Windows.Forms.Padding(20);
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Édition du Végétal";

            this.MainTableLayout.ResumeLayout(false);
            this.MainTableLayout.PerformLayout();
            this.DimensionsLayout.ResumeLayout(false);
            this.DimensionsLayout.PerformLayout();
            this.ColorFlowLayout.ResumeLayout(false);
            this.ButtonFlowLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel MainTableLayout;
        private System.Windows.Forms.TableLayoutPanel DimensionsLayout;
        private System.Windows.Forms.FlowLayoutPanel ColorFlowLayout;
        private System.Windows.Forms.FlowLayoutPanel ButtonFlowLayout;

        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.Label HeightLabel;
        private System.Windows.Forms.Label WidthLabel;
        private System.Windows.Forms.Label TypeLabel;
        private System.Windows.Forms.Label ColorLabel;

        public System.Windows.Forms.TextBox NameInput;
        public System.Windows.Forms.TextBox HeightInput;
        public System.Windows.Forms.TextBox WidthInput;
        public System.Windows.Forms.ComboBox TypeInput;
        public System.Windows.Forms.Panel ColorPreviewPanel;

        private System.Windows.Forms.Button ColorSelectButton;
        private System.Windows.Forms.Button PromptAcceptButton;
        private System.Windows.Forms.Button PromptCancelButton;
    }
}