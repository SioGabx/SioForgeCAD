using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace SioForgeCAD.Functions
{
    public static class SMARTLEGEND
    {
        private const string LegendBlockName = "_APUd_LEGENDE";

        public class LegendItem
        {
            public ObjectId CellBlockId { get; set; }    // Le bloc généré pour la cellule (si existant)
            public string Description { get; set; }      // Le texte de la légende
        }

        public static void Test()
        {
            var db = Generic.GetDatabase();
            Autodesk.AutoCAD.DatabaseServices.Table Legend = new Autodesk.AutoCAD.DatabaseServices.Table();


            Legend.SetSize(5, 2);

            // Dimensions globales
            Legend.TableStyle = db.Tablestyle;
            // On applique les marges à toutes les cellules du tableau

            var HeaderRow = Legend.Rows[0];
            if (HeaderRow.IsMerged == true)
            {
                Legend.UnmergeCells(HeaderRow);
            }
            
            for (int r = 0; r < Legend.Rows.Count; r++)
            {
                for (int c = -1; c < Legend.Columns.Count; c++)
                {
                    //https://github.com/shichongdong/AcadLib/blob/a5eade1d03ea428b7c46f5cd6bb6592f9925736f/AcadLib/Model/Tables/TableExt.cs#L6
                    var cell = Legend.Cells[r, c] as Autodesk.AutoCAD.DatabaseServices.Cell;
                    cell.DataType = new DataTypeParameter(DataType.General, UnitType.Unitless);
                    cell.IsMergeAllEnabled = false;
                    cell.Style = ""; //To "suppress" the title and header, style of "Title" or "Header" -> set it to ""
                    cell.TextHeight = 0.25; 
                    cell.Borders.Horizontal.Margin = 0.25;
                    cell.Borders.Vertical.Margin = 0.25;

                    cell.Borders.Top.Margin = 0.25;
                    cell.Borders.Bottom.Margin = 0.25;
                    cell.Borders.Left.Margin = 0.25;
                    cell.Borders.Right.Margin = 0.25;
                    cell.Borders.Left.Margin = 0.25;
                    cell.Borders.Right.Margin = 0.25;

                    if (c >= 0)
                    {
                        cell.Contents.Clear();
                        cell.Contents.Add();
                        cell.Contents[0].IsAutoScale = false;
                        cell.TextString = "test";
                        cell.State = CellStates.FormatLocked;
                    }
                }
            }

            Legend.Columns[0].Width = 1.6;
            Legend.Columns[1].Width = 5;
            Legend.Rows[0].Height = 1;
            Legend.Rows[1].Height = 1;
            Legend.Position = new Point3d(0, 0, 0);
            Legend.GenerateLayout();
            Legend.RecomputeTableBlock(true);
            Legend.AddToDrawing();
        }
    }
}
