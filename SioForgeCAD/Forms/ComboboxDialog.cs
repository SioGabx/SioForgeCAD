using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class ComboboxDialog : Form
    {
        public class SelectionItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private bool include = true;
            private string name = string.Empty;

            public SelectionItem(string name, bool initialCheck)
            {
                this.name = name;
                this.include = initialCheck;
            }

            public bool Include
            {
                get => include;
                set
                {
                    if (include != value)
                    {
                        include = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Include)));
                    }
                }
            }

            public string Name
            {
                get => name;
                set
                {
                    if (name != value)
                    {
                        name = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    }
                }
            }
        }

        private readonly BindingList<SelectionItem> _items = new BindingList<SelectionItem>();
        private bool _isUpdatingHeader = false;

        public ComboboxDialog(IEnumerable<string> DataList, string Prompt, bool CheckAllByDefault = true)
        {
            InitializeComponent();
            SetupDataGridView(Prompt, CheckAllByDefault);
            this.Text = Prompt;
            // Écouter les modifications dans la BindingList
            _items.ListChanged += Items_ListChanged;

            foreach (var item in DataList)
            {
                _items.Add(new SelectionItem(item, CheckAllByDefault));
            }

            dataGridView1.DataSource = new BindingSource(_items, null);

            // Synchronisation initiale
            UpdateHeaderCheckBoxState();
        }

        private void SetupDataGridView(string Prompt, bool CheckAllByDefault)
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Clear();

            var includeCol = new DataGridViewCheckBoxColumn
            {
                HeaderText = "",
                DataPropertyName = nameof(SelectionItem.Include),
                Name = nameof(SelectionItem.Include),
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            var nameCol = new DataGridViewTextBoxColumn
            {
                HeaderText = Prompt,
                DataPropertyName = nameof(SelectionItem.Name),
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridView1.Columns.AddRange(includeCol, nameCol);

            // Gestion du clic utilisateur sur la Checkbox d'en-tête
            headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;
            dataGridView1.Controls.Add(headerCheckBox);

            // Validation immédiate de l'édition d'une cellule pour déclencher INotifyPropertyChanged sans attendre de changement de focus
            dataGridView1.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            dummy.Focus();
        }

        private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingHeader) return;

            _isUpdatingHeader = true;
            foreach (var item in _items)
            {
                item.Include = headerCheckBox.Checked;
            }
            _isUpdatingHeader = false;

            dataGridView1.Refresh();
        }

        private void Items_ListChanged(object sender, ListChangedEventArgs e)
        {
            UpdateHeaderCheckBoxState();
        }

        private void UpdateHeaderCheckBoxState()
        {
            if (_isUpdatingHeader) return;

            _isUpdatingHeader = true;
            // Coché uniquement si TOUS les éléments sont cochés (et la liste non vide)
            headerCheckBox.Checked = _items.Count > 0 && _items.All(x => x.Include);
            _isUpdatingHeader = false;
        }

        public List<string> GetSelectedItems()
        {
            return _items.Where(x => x.Include).Select(x => x.Name).ToList();
        }

        public void SetSelectedItems(List<string> items)
        {
            var selectedSet = new HashSet<string>(items ?? Enumerable.Empty<string>());

            foreach (var item in _items)
            {
                item.Include = selectedSet.Contains(item.Name);
            }

            UpdateHeaderCheckBoxState();
        }

        private void ValidateButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SelectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is SelectionItem item)
                    {
                        item.Include = true;
                    }
                }
            }
        }

        private void DeselectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is SelectionItem item)
                    {
                        item.Include = false;
                    }
                }
            }
        }
    }
}