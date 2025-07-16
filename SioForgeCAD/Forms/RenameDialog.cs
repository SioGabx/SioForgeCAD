using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class RenameDialog : Form
    {
        public class DataTable
        {
            public event EventHandler<PropertyChangedEventArgs> PropertyChanged;

            // This method is called by the Set accessor of each property.
            // The CallerMemberName attribute that is applied to the optional propertyName
            // parameter causes the property name of the caller to be substituted as an argument.
            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private bool include = true;
            private string original = string.Empty;
            private string renamed = string.Empty;

            public DataTable(bool include, string original, string renamed)
            {
                Include = include;
                Original = original;
                Renamed = renamed;
            }

            public bool Include
            {
                get => include;
                set
                {
                    include = value;
                    NotifyPropertyChanged();
                }
            }
            public string Original
            {
                get => original;
                private set
                {
                    original = value;
                    NotifyPropertyChanged();
                }
            }
            public string Renamed
            {
                get => renamed;
                private set
                {
                    renamed = value;
                    NotifyPropertyChanged();
                }
            }
        }

        readonly BindingList<DataTable> dataTableContent = new BindingList<DataTable>();

        public RenameDialog()
        {
            InitializeComponent();


            dataGridView1.AutoGenerateColumns = false;
            var includeCol = new DataGridViewCheckBoxColumn
            {
                HeaderText = "",
                DataPropertyName = nameof(DataTable.Include),
                Name = nameof(DataTable.Include),
                Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
    
            };

            var originalCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Original",
                DataPropertyName = nameof(DataTable.Original),
                Name = nameof(DataTable.Original),
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    ForeColor = Color.FromArgb(100,100,100)
                }
            };

            var renamedCol = new DataGridViewTextBoxColumn
            {
                HeaderText = "Nouveau nom",
                DataPropertyName = nameof(DataTable.Renamed),
                Name = nameof(DataTable.Renamed),
            };

            dataGridView1.Columns.Add(includeCol);
            dataGridView1.Columns.Add(originalCol);
            dataGridView1.Columns.Add(renamedCol);


            headerCheckBox.Size = new Size(18, 18);
            headerCheckBox.BackColor = Color.Transparent;
            headerCheckBox.Location = new Point(((includeCol.Width - headerCheckBox.Width) / 2) + 2, 0);
            headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;

            this.dataGridView1.Controls.Add(headerCheckBox);


            // Ajoute gestion du clic pour empêcher le déclenchement multiple
            this.dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
            this.dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;







            var dataTableContent = new BindingList<DataTable>();


            dataTableContent.Add(new DataTable(true, "prout", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));
            dataTableContent.Add(new DataTable(true, "caca", "bidule"));




            var source = new BindingSource(dataTableContent, null);
            dataGridView1.DataSource = source;
            dataGridView1.AutoGenerateColumns = true;


        }

        private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var CheckedState = headerCheckBox.Checked;
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                dataGridView1.Rows[i].Cells[nameof(DataTable.Include)].Value = CheckedState;
            }
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns[nameof(DataTable.Include)].Index)
                UpdateHeaderCheckBox();
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell is DataGridViewCheckBoxCell && dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void UpdateHeaderCheckBox()
        {
            bool allChecked = true;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(row.Cells[nameof(DataTable.Include)].Value) == false)
                {
                    allChecked = false;
                    break;
                }
            }

            headerCheckBox.CheckedChanged -= HeaderCheckBox_CheckedChanged;
            headerCheckBox.Checked = allChecked;
            headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;
        }


    }
}
