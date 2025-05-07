namespace SioForgeCAD.Forms
{
    partial class NotesPalette
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabHistory = new System.Windows.Forms.TabPage();
            this.tabNew = new System.Windows.Forms.TabPage();
            this.tabPinned = new System.Windows.Forms.TabPage();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabHistory);
            this.tabControl1.Controls.Add(this.tabNew);
            this.tabControl1.Controls.Add(this.tabPinned);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(526, 298);
            this.tabControl1.TabIndex = 0;
            // 
            // tabHistory
            // 
            this.tabHistory.Location = new System.Drawing.Point(4, 22);
            this.tabHistory.Name = "tabHistory";
            this.tabHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabHistory.Size = new System.Drawing.Size(518, 272);
            this.tabHistory.TabIndex = 0;
            this.tabHistory.Text = "Historique";
            this.tabHistory.UseVisualStyleBackColor = true;
            // 
            // tabNew
            // 
            this.tabNew.Location = new System.Drawing.Point(4, 22);
            this.tabNew.Name = "tabNew";
            this.tabNew.Padding = new System.Windows.Forms.Padding(3);
            this.tabNew.Size = new System.Drawing.Size(518, 272);
            this.tabNew.TabIndex = 1;
            this.tabNew.Text = "Nouveau";
            this.tabNew.UseVisualStyleBackColor = true;
            // 
            // tabPinned
            // 
            this.tabPinned.Location = new System.Drawing.Point(4, 22);
            this.tabPinned.Name = "tabPinned";
            this.tabPinned.Size = new System.Drawing.Size(518, 272);
            this.tabPinned.TabIndex = 2;
            this.tabPinned.Text = "Épinglé";
            this.tabPinned.UseVisualStyleBackColor = true;
            // 
            // NotesPalette
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "NotesPalette";
            this.Size = new System.Drawing.Size(526, 298);
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabHistory;
        private System.Windows.Forms.TabPage tabNew;
        private System.Windows.Forms.TabPage tabPinned;
    }
}
