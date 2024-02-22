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
            this.SuspendLayout();
            // 
            // PromptLabel
            // 
            this.PromptLabel.AutoSize = true;
            this.PromptLabel.Location = new System.Drawing.Point(6, 15);
            this.PromptLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.PromptLabel.Name = "PromptLabel";
            this.PromptLabel.Size = new System.Drawing.Size(66, 13);
            this.PromptLabel.TabIndex = 0;
            this.PromptLabel.Text = "PromptLabel";
            // 
            // UserInputBox
            // 
            this.UserInputBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.UserInputBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.UserInputBox.Location = new System.Drawing.Point(6, 31);
            this.UserInputBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.UserInputBox.Name = "UserInputBox";
            this.UserInputBox.Size = new System.Drawing.Size(405, 20);
            this.UserInputBox.TabIndex = 1;
            this.UserInputBox.Validated += new System.EventHandler(this.UserInputBox_Validated);
            // 
            // PromptAcceptButton
            // 
            this.PromptAcceptButton.Location = new System.Drawing.Point(230, 69);
            this.PromptAcceptButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.PromptAcceptButton.Name = "PromptAcceptButton";
            this.PromptAcceptButton.Size = new System.Drawing.Size(88, 27);
            this.PromptAcceptButton.TabIndex = 2;
            this.PromptAcceptButton.Text = "&Ok";
            this.PromptAcceptButton.UseVisualStyleBackColor = true;
            this.PromptAcceptButton.Click += new System.EventHandler(this.PromptAcceptButton_Click);
            // 
            // PromptCancelButton
            // 
            this.PromptCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.PromptCancelButton.Location = new System.Drawing.Point(322, 69);
            this.PromptCancelButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.PromptCancelButton.Name = "PromptCancelButton";
            this.PromptCancelButton.Size = new System.Drawing.Size(88, 27);
            this.PromptCancelButton.TabIndex = 3;
            this.PromptCancelButton.Text = "&Cancel";
            this.PromptCancelButton.UseVisualStyleBackColor = true;
            this.PromptCancelButton.Click += new System.EventHandler(this.PromptCancelButton_Click);
            // 
            // InputDialogBox
            // 
            this.AcceptButton = this.PromptAcceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.PromptCancelButton;
            this.ClientSize = new System.Drawing.Size(417, 102);
            this.Controls.Add(this.PromptAcceptButton);
            this.Controls.Add(this.UserInputBox);
            this.Controls.Add(this.PromptCancelButton);
            this.Controls.Add(this.PromptLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "InputDialogBox";
            this.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.Label PromptLabel;
        private System.Windows.Forms.Button PromptCancelButton;
        private System.Windows.Forms.TextBox UserInputBox;
        private System.Windows.Forms.Button PromptAcceptButton;
    }
}