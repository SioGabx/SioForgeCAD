using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;

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
                ObjectId OriginalPoly = ObjectId.Null;
                using (var poly = ed.GetPolyline(out OriginalPoly, "Selectionnez une polyligne", Clone: false))
                {
                    if (poly == null) {
                        tr.Commit();
                        return; }
                    int NumberOfVerticesBefore = poly.NumberOfVertices;

                    if (!poly.IsWriteEnabled) { poly.UpgradeOpen(); }

                    PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Indiquez le nombre minimum de segments par arcs")
                    {
                        DefaultValue = 3
                    };
                    var value = ed.GetDouble(promptDoubleOptions);
                    if (value.Status != PromptStatus.OK) { return; }
                    var Polygon = poly.ToPolygon((uint)value.Value);
                    var OriginalPolyEnt = OriginalPoly.GetEntity();
                    OriginalPolyEnt.CopyPropertiesTo(Polygon);
                    Polygon.AddToDrawing();
                    OriginalPolyEnt.EraseObject();
                    tr.Commit();
                }
            }
        }
    }
}
