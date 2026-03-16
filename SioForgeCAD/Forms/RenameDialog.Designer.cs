namespace SioForgeCAD.Forms
{
    partial class RenameDialog
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

            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.headerCheckBox = new System.Windows.Forms.CheckBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.txtReplace = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.lblReplace = new System.Windows.Forms.Label();
            this.chkUseRegex = new System.Windows.Forms.CheckBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.flowControls = new System.Windows.Forms.FlowLayoutPanel();
            this.lblMessage = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.panelLeft.SuspendLayout();
            this.flowControls.SuspendLayout();
            this.SuspendLayout();

            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
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
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(230, 240, 250);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.EnableHeadersVisualStyles = false;
            this.dataGridView1.GridColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.dataGridView1.Location = new System.Drawing.Point(320, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(580, 480);
            this.dataGridView1.TabIndex = 5;

            // 
            // panelLeft (Conteneur Parent)
            // 
            this.panelLeft.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.panelLeft.Controls.Add(this.flowControls); // Ajout du flux
            this.panelLeft.Controls.Add(this.btnApply);    // Ajout du bouton fixe
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(320, 480);

            // 
            // flowControls (Conteneur Dynamique)
            // 
            this.flowControls.AutoSize = true;
            this.flowControls.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowControls.Controls.Add(this.lblSearch);
            this.flowControls.Controls.Add(this.txtSearch);
            this.flowControls.Controls.Add(this.lblReplace);
            this.flowControls.Controls.Add(this.txtReplace);
            this.flowControls.Controls.Add(this.lblMessage);
            this.flowControls.Controls.Add(this.chkUseRegex);
            this.flowControls.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowControls.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowControls.Location = new System.Drawing.Point(0, 0);
            this.flowControls.Name = "flowControls";
            this.flowControls.Padding = new System.Windows.Forms.Padding(20, 25, 20, 0);
            this.flowControls.Size = new System.Drawing.Size(320, 218); // Hauteur auto
            this.flowControls.WrapContents = false;

            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblSearch.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.lblSearch.Text = "Rechercher :";

            // 
            // txtSearch
            // 
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(3, 0, 3, 15);
            this.txtSearch.Size = new System.Drawing.Size(275, 25);

            // 
            // lblReplace
            // 
            this.lblReplace.AutoSize = true;
            this.lblReplace.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblReplace.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.lblReplace.Text = "Remplacer par :";

            // 
            // txtReplace
            // 
            this.txtReplace.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtReplace.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0); // Pas de marge bas car message dessous
            this.txtReplace.Size = new System.Drawing.Size(275, 25);

            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = true;
            this.lblMessage.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic);
            this.lblMessage.ForeColor = System.Drawing.Color.DimGray;
            this.lblMessage.Margin = new System.Windows.Forms.Padding(3, 5, 3, 10);
            this.lblMessage.Text = "Message d'exemple";
            this.lblMessage.Visible = false; // Caché par défaut

            // 
            // chkUseRegex
            // 
            this.chkUseRegex.AutoSize = true;
            this.chkUseRegex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkUseRegex.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this.chkUseRegex.Text = "Utiliser les Regex (.*)";

            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnApply.Location = new System.Drawing.Point(23, 428);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(275, 30);
            this.btnApply.Text = "Appliquer le renommage";
            this.btnApply.Click += new System.EventHandler(this.BtnApply_Click);

            // 
            // RenameDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 480);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.panelLeft);
            this.MinimumSize = new System.Drawing.Size(700, 400);
            this.Name = "RenameDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PowerRename SioForge";

            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.panelLeft.ResumeLayout(false);
            this.panelLeft.PerformLayout();
            this.flowControls.ResumeLayout(false);
            this.flowControls.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.CheckBox headerCheckBox;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.TextBox txtReplace;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Label lblReplace;
        private System.Windows.Forms.CheckBox chkUseRegex;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.FlowLayoutPanel flowControls;
        private System.Windows.Forms.Label lblMessage;
    }
}