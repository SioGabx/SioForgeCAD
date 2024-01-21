using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DataGridViewExtensions
    {
        public static List<DataGridViewCell> ToList(this DataGridViewSelectedCellCollection dataGridViewSelectedCellCollection)
        {
            List<DataGridViewCell> list = new List<DataGridViewCell>();
            foreach (DataGridViewCell cell in dataGridViewSelectedCellCollection)
            {
                list.Add(cell);
            }
            return list;
        }
    }
}
