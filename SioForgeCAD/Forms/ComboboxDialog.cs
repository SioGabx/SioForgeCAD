using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class ComboboxDialog : Form
    {
        // Classe simplifiée pour la sélection
        public class SelectionItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private bool include = true;
            private string name = string.Empty;

            public SelectionItem(string name) => this.name = name;

            public bool Include
            {
                get => include;
                set { include = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Include))); }
            }
            public string Name
            {
                get => name;
                set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
            }
        }

        private readonly BindingList<SelectionItem> _items = new BindingList<SelectionItem>();

        public ComboboxDialog(List<string> dataList)
        {
            InitializeComponent();
            SetupDataGridView();

            foreach (var item in dataList)
            {
                _items.Add(new SelectionItem(item));
            }

            dataGridView1.DataSource = new BindingSource(_items, null);
        }

        private void SetupDataGridView()
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
                HeaderText = "Élément",
                DataPropertyName = nameof(SelectionItem.Name),
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridView1.Columns.AddRange(includeCol, nameCol);

            // Checkbox d'en-tête (Tout sélectionner)
            headerCheckBox.Checked = true;
            headerCheckBox.CheckedChanged += (s, e) =>
            {
                foreach (var item in _items) item.Include = headerCheckBox.Checked;
                dataGridView1.Refresh();
            };
            dataGridView1.Controls.Add(headerCheckBox);

            dataGridView1.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dataGridView1.IsCurrentCellDirty) dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
        }

        // Récupérer les éléments cochés
        public List<string> GetSelectedItems()
        {
            return _items.Where(x => x.Include).Select(x => x.Name).ToList();
        }

        private void ValidateButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}