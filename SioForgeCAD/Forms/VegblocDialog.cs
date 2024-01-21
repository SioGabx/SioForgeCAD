using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DataGridCell = System.Windows.Forms.DataGridCell;

namespace SioForgeCAD.Forms
{
    public partial class VegblocDialog : Form
    {
        public VegblocDialog()
        {
            InitializeComponent();
        }
        private bool isCellBeingEdited = false;

        private void DataGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            isCellBeingEdited = true;
        }

        private void DataGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            isCellBeingEdited = false;
        }

        private void DataGrid_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (!isCellBeingEdited)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (e.KeyCode == Keys.C)
                    {
                        CopySelectedCells();
                        e.Handled = true;
                    }
                    if (e.KeyCode == Keys.V)
                    {
                        PasteIntoCells();
                        e.Handled = true;
                    }

                    if (e.KeyCode == Keys.F)
                    {
                        Debug.WriteLine(IsContiguousSelection());
                        e.Handled = true;
                    }
                }
            }

        }

        private bool IsContiguousSelection()
        {
            var selectedCells = DataGrid.SelectedCells;

            if (selectedCells.Count == 0)
            {
                return false;
            }

            int minRow = int.MaxValue;
            int minCol = int.MaxValue;
            int maxRow = int.MinValue;
            int maxCol = int.MinValue;
            foreach (DataGridViewCell cell in selectedCells)
            {
                int cellRow = cell.RowIndex;
                int cellCol = cell.ColumnIndex;
                minRow = Math.Min(minRow, cellRow);
                minCol = Math.Min(minCol, cellCol);
                maxRow = Math.Max(maxRow, cellRow);
                maxCol = Math.Max(maxCol, cellCol);

            }

            // Check if the selected cells form a contiguous block
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    DataGridViewCell cell = DataGrid.Rows[row].Cells[col];

                    if (!selectedCells.Contains(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void CopySelectedCells()
        {
            throw new NotImplementedException();
        }


        //private void PasteIntoCells()
        //{
        //    if (!IsContiguousSelection())
        //    {
        //        MessageBox.Show("La selection n'est pas contigue");
        //        return;
        //    }

        //    string data = Clipboard.GetText();
        //    string[][] clipboardData = ParseCSV(data);

        //    if (clipboardData.Length == 0)
        //    {
        //        return;
        //    }

        //    DataGridViewCell currentCell = DataGrid.CurrentCell;
        //    int startRow = currentCell.RowIndex;
        //    int startCol = currentCell.ColumnIndex;

        //    int rowCountToSelect = Math.Min(DataGrid.RowCount - startRow, clipboardData.Length);
        //    int colCountToSelect = Math.Min(DataGrid.ColumnCount - startCol, clipboardData[0].Length);

        //    // Clear the current selection
        //    DataGrid.ClearSelection();

        //    for (int rowIndex = 0; rowIndex < rowCountToSelect; rowIndex++)
        //    {
        //        DataGridViewRow row = DataGrid.Rows[startRow + rowIndex];
        //        string[] rowContent = clipboardData[rowIndex % clipboardData.Length];

        //        for (int colIndex = 0; colIndex < colCountToSelect; colIndex++)
        //        {
        //            DataGridViewCell cell = row.Cells[startCol + colIndex];
        //            string cellContent = rowContent[colIndex % clipboardData[0].Length];

        //            // Set the cell value
        //            cell.Value = cellContent;

        //            // Select the pasted cell
        //            cell.Selected = true;
        //        }
        //    }
        //}






        private async void PasteIntoCells()
        {
            if (!IsContiguousSelection())
            {
                MessageBox.Show("La selection n'est pas contigue");
                return;
            }
            string data = Clipboard.GetText();
            string[][] clipboardData = ParseCSV(data);
            List<DataGridViewCell> SelectedCells = DataGrid.SelectedCells.ToList();

            int minRow = int.MaxValue;
            int minCol = int.MaxValue;
            foreach (DataGridViewCell cell in SelectedCells)
            {
                int cellRow = cell.RowIndex;
                int cellCol = cell.ColumnIndex;
                minRow = Math.Min(minRow, cellRow);
                minCol = Math.Min(minCol, cellCol);
            }
            DataGridViewCell currentCell = SelectedCells.Where(cell => (cell.RowIndex == minRow && cell.ColumnIndex == minCol)).First();
            int NumberOfColumnsSelected = SelectedCells.Select(s => s.ColumnIndex).Distinct().Count();
            int NumberOfRowSelected = SelectedCells.Select(s => s.RowIndex).Distinct().Count();


            int startRow = currentCell.RowIndex;
            while (startRow + clipboardData.Length > DataGrid.Rows.Count)
            {
                DataGridViewRow newRow = new DataGridViewRow();
                newRow.CreateCells(DataGrid);
                DataGrid.Rows.Insert(DataGrid.Rows.Count - 1, newRow);
            }
            int[] rows = Enumerable.Range((int)startRow, Math.Min(DataGrid.RowCount, clipboardData.Length)).ToArray();

            List<int> columns = new List<int>();
            bool isColumnFound = false;
            for (int i = 0; i < DataGrid.ColumnCount; i++)
            {
                if (!isColumnFound)
                {
                    if (i == currentCell.ColumnIndex)
                    {
                        isColumnFound = true;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (columns.Count < clipboardData.Max(row => row.Length))
                {
                    columns.Add(i);
                }
            }


            List<int> selectedcolumns = new List<int>();
            isColumnFound = false;
            for (int i = 0; i < DataGrid.ColumnCount; i++)
            {
                if (!isColumnFound)
                {
                    if (i == currentCell.ColumnIndex)
                    {
                        isColumnFound = true;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (selectedcolumns.Count < NumberOfColumnsSelected)
                {
                    selectedcolumns.Add(i);
                }
            }

            DataGrid.ClearSelection();
            //}
            // Adjust the selection to accommodate the clipboard content
            int rowCountToSelect = Math.Max(rows.Length, NumberOfRowSelected);
            int colCountToSelect = Math.Max(columns.Count, NumberOfColumnsSelected);


            for (int selectedRowIndex = 0; selectedRowIndex < rowCountToSelect; selectedRowIndex++)
            {
                int RowIndex = startRow + selectedRowIndex;

                DataGridViewRow row = DataGrid.Rows[RowIndex];
                string[] rowContent = clipboardData[selectedRowIndex % clipboardData.Length];

                for (int colIndex = 0; colIndex < colCountToSelect; colIndex++)
                {
                    string cellContent = rowContent[colIndex % columns.Count];
                    DataGridViewColumn column = null;
                    if ((selectedcolumns.Count) > colIndex)
                    {
                        //Selected Columns is greater than selection
                        column = DataGrid.Columns[selectedcolumns[colIndex]];
                    }
                    else
                    {
                        column = DataGrid.Columns[columns[colIndex % columns.Count]];
                    }
                    var newCell = row.Cells[column.Index];
                    Type type = newCell.GetType();
                    if (newCell is DataGridViewTextBoxCell TextBoxCell)
                    {
                        TextBoxCell.Value = cellContent;
                    }
                    if (newCell is DataGridViewComboBoxCell ComboBoxCell)
                    {
                        if (!ComboBoxCell.Items.Contains(cellContent))
                        {
                            (column as DataGridViewComboBoxColumn).Items.Add(cellContent);
                        }
                        ComboBoxCell.Value = cellContent;
                    }

                    if (!DataGrid.SelectedCells.Contains(newCell))
                    {
                        newCell.Selected = true;
                    }

                }
            }
        }









        public static string[][] ParseCSV(string input, string delimiter = "\t")
        {
            var csvStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input));
            var csvReader = new Commun.Mist.CsvReader(new StreamReader(csvStream), delimiter);
            var Lines = new List<string[]>();
            while (csvReader.Read())
            {
                var Field = new List<string>();
                for (int i = 0; i < csvReader.FieldsCount; i++)
                {
                    Field.Add(csvReader[i]);
                }
                Lines.Add(Field.ToArray());
            }
            return Lines.ToArray();
        }

        private void VegblocDialog_Load(object sender, EventArgs e)
        {
            int defaultNumberOfRows = 20; 

            // Add empty rows to the DataGridView
            for (int i = 0; i < defaultNumberOfRows; i++)
            {
                DataGrid.Rows.Add();
            }
        }
    }
}
