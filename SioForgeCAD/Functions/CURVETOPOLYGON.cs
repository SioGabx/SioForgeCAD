using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class CURVETOPOLYGON
    {
        public static void Convert()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                using (var poly = ed.GetPolyline("Selectionnez une polyligne", Clone: false))
                {
                    int NumberOfVerticesBefore = poly.NumberOfVertices;
                    poly.UpgradeOpen();
                    PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Indiquez le nombre minimum de segments par arcs")
                    {
                        DefaultValue = 3
                    };
                    var value = ed.GetDouble(promptDoubleOptions);
                    if (value.Status != PromptStatus.OK) { return; }
                    var Polygon = poly.ToPolygon((uint)value.Value);
                    poly.CopyPropertiesTo(Polygon);
                    Polygon.AddToDrawing();
                    poly.EraseObject();
                    tr.Commit();
                }
            }
        }
    }
}
