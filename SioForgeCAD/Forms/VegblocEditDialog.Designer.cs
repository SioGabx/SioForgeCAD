namespace SioForgeCAD.Forms
{
    partial class VegblocEditDialog
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
            this.ContentPanel = new System.Windows.Forms.Panel();
            this.TypeLabel = new System.Windows.Forms.Label();
            this.TypeInput = new System.Windows.Forms.ComboBox();
            this.WidthInput = new System.Windows.Forms.TextBox();
            this.WidthLabel = new System.Windows.Forms.Label();
            this.HeightInput = new System.Windows.Forms.TextBox();
            this.HeightLabel = new System.Windows.Forms.Label();
            this.NameInput = new System.Windows.Forms.TextBox();
            this.PromptAcceptButton = new System.Windows.Forms.Button();
            this.NameLabel = new System.Windows.Forms.Label();
            this.PromptCancelButton = new System.Windows.Forms.Button();
            this.ContentPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ContentPanel
            // 
            this.ContentPanel.Controls.Add(this.TypeLabel);
            this.ContentPanel.Controls.Add(this.TypeInput);
            this.ContentPanel.Controls.Add(this.WidthInput);
            this.ContentPanel.Controls.Add(this.WidthLabel);
            this.ContentPanel.Controls.Add(this.HeightInput);
            this.ContentPanel.Controls.Add(this.HeightLabel);
            this.ContentPanel.Controls.Add(this.NameInput);
            this.ContentPanel.Controls.Add(this.PromptAcceptButton);
            this.ContentPanel.Controls.Add(this.NameLabel);
            this.ContentPanel.Controls.Add(this.PromptCancelButton);
            this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContentPanel.Location = new System.Drawing.Point(0, 0);
            this.ContentPanel.Margin = new System.Windows.Forms.Padding(2);
            this.ContentPanel.Name = "ContentPanel";
            this.ContentPanel.Size = new System.Drawing.Size(417, 190);
            this.ContentPanel.TabIndex = 2;
            // 
            // TypeLabel
            // 
            this.TypeLabel.AutoSize = true;
            this.TypeLabel.Location = new System.Drawing.Point(11, 99);
            this.TypeLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.TypeLabel.Name = "TypeLabel";
            this.TypeLabel.Size = new System.Drawing.Size(31, 13);
            this.TypeLabel.TabIndex = 14;
            this.TypeLabel.Text = "Type";
            // 
            // TypeInput
            // 
            this.TypeInput.AutoCompleteCustomSource.AddRange(new string[] {
            "ARBRES",
            "CEPEES",
            "ARBUSTES",
            "GRIMPANTES",
            "GRAMINEES",
            "VIVACES",
            "BULBEUSES"});
            this.TypeInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.TypeInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.TypeInput.FormattingEnabled = true;
            this.TypeInput.Items.AddRange(new object[] {
            "ARBRES",
            "CEPEES",
            "ARBUSTES",
            "GRIMPANTES",
            "GRAMINEES",
            "VIVACES",
            "BULBEUSES"});
            this.TypeInput.Location = new System.Drawing.Point(11, 116);
            this.TypeInput.Name = "TypeInput";
            this.TypeInput.Size = new System.Drawing.Size(392, 21);
            this.TypeInput.TabIndex = 12;
            // 
            // WidthInput
            // 
            this.WidthInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.WidthInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.WidthInput.Location = new System.Drawing.Point(221, 70);
            this.WidthInput.Margin = new System.Windows.Forms.Padding(2);
            this.WidthInput.Name = "WidthInput";
            this.WidthInput.Size = new System.Drawing.Size(182, 20);
            this.WidthInput.TabIndex = 11;
            // 
            // WidthLabel
            // 
            this.WidthLabel.AutoSize = true;
            this.WidthLabel.Location = new System.Drawing.Point(221, 54);
            this.WidthLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.WidthLabel.Name = "WidthLabel";
            this.WidthLabel.Size = new System.Drawing.Size(92, 13);
            this.WidthLabel.TabIndex = 10;
            this.WidthLabel.Text = "Largeur (diamètre)";
            // 
            // HeightInput
            // 
            this.HeightInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.HeightInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.HeightInput.Location = new System.Drawing.Point(11, 70);
            this.HeightInput.Margin = new System.Windows.Forms.Padding(2);
            this.HeightInput.Name = "HeightInput";
            this.HeightInput.Size = new System.Drawing.Size(182, 20);
            this.HeightInput.TabIndex = 9;
            // 
            // HeightLabel
            // 
            this.HeightLabel.AutoSize = true;
            this.HeightLabel.Location = new System.Drawing.Point(11, 54);
            this.HeightLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.HeightLabel.Name = "HeightLabel";
            this.HeightLabel.Size = new System.Drawing.Size(45, 13);
            this.HeightLabel.TabIndex = 8;
            this.HeightLabel.Text = "Hauteur";
            // 
            // NameInput
            // 
            this.NameInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.NameInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.NameInput.Location = new System.Drawing.Point(11, 25);
            this.NameInput.Margin = new System.Windows.Forms.Padding(2);
            this.NameInput.Name = "NameInput";
            this.NameInput.Size = new System.Drawing.Size(392, 20);
            this.NameInput.TabIndex = 5;
            // 
            // PromptAcceptButton
            // 
            this.PromptAcceptButton.Location = new System.Drawing.Point(223, 152);
            this.PromptAcceptButton.Margin = new System.Windows.Forms.Padding(2);
            this.PromptAcceptButton.Name = "PromptAcceptButton";
            this.PromptAcceptButton.Size = new System.Drawing.Size(88, 27);
            this.PromptAcceptButton.TabIndex = 6;
            this.PromptAcceptButton.Text = "&Ok";
            this.PromptAcceptButton.UseVisualStyleBackColor = true;
            this.PromptAcceptButton.Click += new System.EventHandler(this.PromptAcceptButton_Click);
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(11, 9);
            this.NameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(93, 13);
            this.NameLabel.TabIndex = 4;
            this.NameLabel.Text = "Nom latin \'Cultivar\'";
            // 
            // PromptCancelButton
            // 
            this.PromptCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.PromptCancelButton.Location = new System.Drawing.Point(315, 152);
            this.PromptCancelButton.Margin = new System.Windows.Forms.Padding(2);
            this.PromptCancelButton.Name = "PromptCancelButton";
            this.PromptCancelButton.Size = new System.Drawing.Size(88, 27);
            this.PromptCancelButton.TabIndex = 7;
            this.PromptCancelButton.Text = "&Cancel";
            this.PromptCancelButton.UseVisualStyleBackColor = true;
            this.PromptCancelButton.Click += new System.EventHandler(this.PromptCancelButton_Click);
            // 
            // VegblocEditDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(417, 190);
            this.Controls.Add(this.ContentPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "VegblocEditDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VEGBLOC";
            this.ContentPanel.ResumeLayout(false);
            this.ContentPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel ContentPanel;
        private System.Windows.Forms.Label HeightLabel;
        private System.Windows.Forms.Button PromptAcceptButton;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.Button PromptCancelButton;
        private System.Windows.Forms.Label WidthLabel;
        private System.Windows.Forms.Label TypeLabel;
        public System.Windows.Forms.TextBox HeightInput;
        public System.Windows.Forms.TextBox NameInput;
        public System.Windows.Forms.TextBox WidthInput;
        public System.Windows.Forms.ComboBox TypeInput;
    }
}