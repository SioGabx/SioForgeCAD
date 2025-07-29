using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Functions
{
    public static class CURVETOPOLYGON
    {
        private static int LastConvertNumberOfSegmentPerArc = 3;
        public static void Convert()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var PromptCurves = ed.GetCurves("Selectionnez des courbes à convertir", false);
                if (PromptCurves.Status == PromptStatus.OK)
                {
                    //Get number of segments per arc
                    PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Indiquez le nombre minimum de segments par arcs")
                    {
                        DefaultValue = LastConvertNumberOfSegmentPerArc
                    };

                    var value = ed.GetDouble(promptDoubleOptions);
                    if (value.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return;
                    }
                    LastConvertNumberOfSegmentPerArc = (int)Math.Floor(value.Value);
                    //Convert all selected
                    foreach (var item in PromptCurves.Value.GetObjectIds())
                    {
                        var Ent = item.GetDBObject(OpenMode.ForWrite);
                        if (Ent is Curve curvEnt)
                        {
                            var EntAsPolyline = curvEnt.ToPolyline();
                            using (EntAsPolyline)
                            {
                                var Polygon = EntAsPolyline.ToPolygon((uint)LastConvertNumberOfSegmentPerArc);
                                curvEnt.CopyPropertiesTo(Polygon);
                                curvEnt.ReplaceInDrawing(Polygon);
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}
