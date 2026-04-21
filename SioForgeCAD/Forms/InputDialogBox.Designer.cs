namespace SioForgeCAD.Forms
{
    partial class InputDialogBox
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
            this.PromptLabel = new System.Windows.Forms.Label();
            this.UserInputBox = new System.Windows.Forms.TextBox();
            this.PromptAcceptButton = new System.Windows.Forms.Button();
            this.PromptCancelButton = new System.Windows.Forms.Button();
            this.MainTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ButtonFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.MainTableLayout.SuspendLayout();
            this.ButtonFlowLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainTableLayout
            // 
            this.MainTableLayout.AutoSize = true;
            this.MainTableLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MainTableLayout.ColumnCount = 1;
            this.MainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTableLayout.Controls.Add(this.PromptLabel, 0, 0);
            this.MainTableLayout.Controls.Add(this.UserInputBox, 0, 1);
            this.MainTableLayout.Controls.Add(this.ButtonFlowLayout, 0, 2);
            this.MainTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTableLayout.Location = new System.Drawing.Point(15, 15);
            this.MainTableLayout.Name = "MainTableLayout";
            this.MainTableLayout.RowCount = 3;
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayout.Size = new System.Drawing.Size(400, 100);
            this.MainTableLayout.TabIndex = 0;
            // 
            // PromptLabel
            // 
            this.PromptLabel.AutoSize = true;
            this.PromptLabel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.PromptLabel.Location = new System.Drawing.Point(3, 0);
            this.PromptLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.PromptLabel.MaximumSize = new System.Drawing.Size(390, 0);
            this.PromptLabel.Name = "PromptLabel";
            this.PromptLabel.Size = new System.Drawing.Size(82, 17);
            this.PromptLabel.TabIndex = 0;
            this.PromptLabel.Text = "PromptLabel";
            // 
            // UserInputBox
            // 
            this.UserInputBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.UserInputBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.UserInputBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.UserInputBox.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.UserInputBox.Location = new System.Drawing.Point(3, 30);
            this.UserInputBox.Margin = new System.Windows.Forms.Padding(3, 0, 3, 15);
            this.UserInputBox.Name = "UserInputBox";
            this.UserInputBox.Size = new System.Drawing.Size(394, 25);
            this.UserInputBox.TabIndex = 1;
            this.UserInputBox.Validated += new System.EventHandler(this.UserInputBox_Validated);
            // 
            // ButtonFlowLayout
            // 
            this.ButtonFlowLayout.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ButtonFlowLayout.AutoSize = true;
            this.ButtonFlowLayout.Controls.Add(this.PromptCancelButton);
            this.ButtonFlowLayout.Controls.Add(this.PromptAcceptButton);
            this.ButtonFlowLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.ButtonFlowLayout.Location = new System.Drawing.Point(197, 73);
            this.ButtonFlowLayout.Margin = new System.Windows.Forms.Padding(0);
            this.ButtonFlowLayout.Name = "ButtonFlowLayout";
            this.ButtonFlowLayout.Size = new System.Drawing.Size(203, 35);
            this.ButtonFlowLayout.TabIndex = 2;
            this.ButtonFlowLayout.WrapContents = false;
            // 
            // PromptCancelButton
            // 
            this.PromptCancelButton.BackColor = System.Drawing.Color.White;
            this.PromptCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.PromptCancelButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.PromptCancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PromptCancelButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.PromptCancelButton.Location = new System.Drawing.Point(110, 3);
            this.PromptCancelButton.Margin = new System.Windows.Forms.Padding(5, 3, 0, 3);
            this.PromptCancelButton.Name = "PromptCancelButton";
            this.PromptCancelButton.Size = new System.Drawing.Size(93, 29);
            this.PromptCancelButton.TabIndex = 3;
            this.PromptCancelButton.Text = "&Annuler";
            this.PromptCancelButton.UseVisualStyleBackColor = false;
            this.PromptCancelButton.Click += new System.EventHandler(this.PromptCancelButton_Click);
            // 
            // PromptAcceptButton
            // 
            this.PromptAcceptButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215))))); // Bleu accentuation
            this.PromptAcceptButton.ForeColor = System.Drawing.Color.White;
            this.PromptAcceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.PromptAcceptButton.FlatAppearance.BorderSize = 0;
            this.PromptAcceptButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PromptAcceptButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.PromptAcceptButton.Location = new System.Drawing.Point(7, 3);
            this.PromptAcceptButton.Margin = new System.Windows.Forms.Padding(3, 3, 5, 3);
            this.PromptAcceptButton.Name = "PromptAcceptButton";
            this.PromptAcceptButton.Size = new System.Drawing.Size(93, 29);
            this.PromptAcceptButton.TabIndex = 2;
            this.PromptAcceptButton.Text = "&Ok";
            this.PromptAcceptButton.UseVisualStyleBackColor = false;
            this.PromptAcceptButton.Click += new System.EventHandler(this.PromptAcceptButton_Click);
            // 
            // InputDialogBox
            // 
            this.AcceptButton = this.PromptAcceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.PromptCancelButton;
            this.ClientSize = new System.Drawing.Size(430, 140);
            this.Controls.Add(this.MainTableLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputDialogBox";
            this.Padding = new System.Windows.Forms.Padding(15);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MainTableLayout.ResumeLayout(false);
            this.MainTableLayout.PerformLayout();
            this.ButtonFlowLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.Label PromptLabel;
        private System.Windows.Forms.Button PromptCancelButton;
        private System.Windows.Forms.TextBox UserInputBox;
        private System.Windows.Forms.Button PromptAcceptButton;
        private System.Windows.Forms.TableLayoutPanel MainTableLayout;
        private System.Windows.Forms.FlowLayoutPanel ButtonFlowLayout;
    }
}