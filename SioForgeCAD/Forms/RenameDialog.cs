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
            public event PropertyChangedEventHandler PropertyChanged;

            // This method is called by the Set accessor of each property.
            // The CallerMemberName attribute that is applied to the optional propertyName
            // parameter causes the property name of the caller to be substituted as an argument.
            private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
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
                Renamed = renamed; NotifyPropertyChanged();
            }

            public bool Include { get => include; set
                {
                    include = value;
                    NotifyPropertyChanged();
                }
            }
            public string Original
            {
                get => original; private set
                {
                    original = value;
                    NotifyPropertyChanged();
                }
            }
            public string Renamed
            {
                get => renamed; private set
                {
                    renamed = value;
                    NotifyPropertyChanged();
                }
            }
        }

        List<DataTable> DataTableList = new List<DataTable>();


        public RenameDialog()
        {
            InitializeComponent();

            BindingList<DataTable> DataTableContent = new BindingList<DataTable>(DataTableList);
            var source = new BindingSource(DataTableContent, null);
            dataGridView1.DataSource = source; dataGridView1.AutoGenerateColumns = true;
            DataTableList.Add(new DataTable(true, "caca", "bidule") );
        }
    }
}
