using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;

namespace SioForgeCAD.Forms
{
    public partial class RenameDialog : Form
    {
        // On déplace la classe à l'extérieur ou on la garde ici, 
        // mais INotifyPropertyChanged est crucial pour le refresh auto du Grid
        public class RenameItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private bool include = true;
            private string original = string.Empty;
            private string renamed = string.Empty;

            public RenameItem(string original)
            {
                this.original = original;
                this.renamed = original;
            }

            public bool Include
            {
                get => include;
                set { include = value; NotifyPropertyChanged(); }
            }
            public string Original
            {
                get => original;
                set { original = value; NotifyPropertyChanged(); }
            }
            public string Renamed
            {
                get => renamed;
                set { renamed = value; NotifyPropertyChanged(); }
            }
        }

        private readonly BindingList<RenameItem> _items = new BindingList<RenameItem>();
        private Func<string, string, string> _transformationLogic;

        public RenameDialog(List<string> filesToRename, Func<string, string, string> transformationLogic)
        {
            InitializeComponent();
            SetupDataGridView();
            // Initialisation des données
            foreach (var file in filesToRename)
            {
                _items.Add(new RenameItem(file));
            }

            dataGridView1.DataSource = new BindingSource(_items, null);

            // Liaison des événements de recherche (Assume que tu as ces noms de controls)
            txtSearch.TextChanged += (s, e) => ApplyRenameLogic();
            txtReplace.TextChanged += (s, e) => ApplyRenameLogic();
            chkUseRegex.CheckedChanged += (s, e) => ApplyRenameLogic();
            _transformationLogic = transformationLogic;
        }

        public void UpdateMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                lblMessage.Visible = false;
            }
            else
            {
                lblMessage.Text = text;
                lblMessage.Visible = true;
            }
        }

        public List<RenameItem> GetRenamingResults()
        {
            return _items.Where(x => x.Include && x.Original != x.Renamed).ToList();
        }

        private void SetupDataGridView()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Clear();

            var includeCol = new DataGridViewCheckBoxColumn
            {
                HeaderText = "",
                DataPropertyName = nameof(RenameItem.Include),
                Name = nameof(RenameItem.Include),
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };


            var originalCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Original",
                DataPropertyName = nameof(RenameItem.Original),
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Color.Gray }
            };

            var renamedCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Nouveau nom",
                DataPropertyName = nameof(RenameItem.Renamed),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridView1.Columns.AddRange(includeCol, originalCol, renamedCol);

            // Setup Header Checkbox
            headerCheckBox.Size = new Size(15, 15);
            headerCheckBox.BackColor = Color.White;
            // On la place manuellement : X=13 pour l'aligner avec les autres checkbox, Y=10 pour centrer dans l'en-tête de 35px
            headerCheckBox.Location = new Point(13, 12);
            headerCheckBox.Checked = true;
            headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;
            dataGridView1.Controls.Add(headerCheckBox);

            dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
            dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;
        }




        /// <summary>
        /// Logique principale de renommage (Cœur de PowerRename)
        /// </summary>
        private void ApplyRenameLogic()
        {
            string searchText = txtSearch.Text;
            string replaceText = txtReplace.Text;
            bool useRegex = chkUseRegex.Checked;

            foreach (var item in _items)
            {
                string ItemRenamed = string.Empty;
                if (string.IsNullOrEmpty(searchText))
                {
                    ItemRenamed = item.Original;
                    continue;
                }

                try
                {
                    if (useRegex)
                    {
                        ItemRenamed = Regex.Replace(item.Original, searchText, replaceText);
                    }
                    else
                    {
                        // Remplacement simple (insensible à la casse comme Windows)
                        ItemRenamed = item.Original.Replace(searchText, replaceText);
                    }
                }
                catch
                {
                    // En cas de Regex invalide pendant la saisie, on ne fait rien
                    ItemRenamed = item.Original;
                }
                item.Renamed = _transformationLogic(item.Original, ItemRenamed);
            }
        }

        #region Events Grid
        private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var item in _items)
            {
                item.Include = headerCheckBox.Checked;
            }

            dataGridView1.Refresh();
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == nameof(RenameItem.Include))
            {
                Debug.WriteLine(dataGridView1.SelectedRows.Count);
                bool newValue = _items[e.RowIndex].Include;
                foreach (DataGridViewRow Row in dataGridView1.SelectedRows)
                {
                    var item = (RenameItem)Row.DataBoundItem;
                    item.Include = newValue;
                }

                if (Control.ModifierKeys.HasFlag(Keys.Control) || Control.ModifierKeys.HasFlag(Keys.Shift))
                {
                    dataGridView1.Rows[e.RowIndex].Selected = true;
                }
                UpdateHeaderCheckBoxState();
            }
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell is DataGridViewCheckBoxCell)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void UpdateHeaderCheckBoxState()
        {
            headerCheckBox.CheckedChanged -= HeaderCheckBox_CheckedChanged;
            headerCheckBox.Checked = _items.All(x => x.Include);
            headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;
        }
        #endregion

        // Bouton Valider final
        private void BtnApply_Click(object sender, EventArgs e)
        {
            var results = _items.Where(x => x.Include && x.Original != x.Renamed).ToList();
            // Ici, exécute ton System.IO.File.Move ou passe la liste à ton contrôleur
            this.DialogResult = DialogResult.OK;
        }

        private void CopyOriginal_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem is RenameItem item && !string.IsNullOrEmpty(item.Original))
            {
                Clipboard.SetText(item.Original);
            }
        }

        private void CopyRenamed_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem is RenameItem item && !string.IsNullOrEmpty(item.Renamed))
            {
                Clipboard.SetText(item.Renamed);
            }
        }

        private void RenameDialog_Shown(object sender, EventArgs e)
        {
            txtSearch.Focus();
        }
    }
}