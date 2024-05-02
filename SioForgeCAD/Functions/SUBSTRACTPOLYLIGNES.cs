using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class SUBSTRACTPOLYLIGNES
    {
        public static void Substract()
        {
            Editor ed = Generic.GetEditor();
            Document doc = Generic.GetDocument();

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                using (var BasePolygon = ed.GetPolyline("BasePolygon"))
                {
                    if (BasePolygon == null) { return; }
                    using (var CutPolygon = ed.GetPolyline("CutPolygon"))
                    {
                        if (CutPolygon == null) { return; }
                        PolygonOperation.Substraction(new PolyHole(BasePolygon, Array.Empty<Polyline>()), new Polyline[1] { CutPolygon }, out List<PolyHole> UnionResult);

                        foreach (var polyh in UnionResult)
                        {
                            polyh.Boundary.AddToDrawing(3);
                            polyh.Holes.AddToDrawing(2);
                        }
                    }
                }
                tr.Commit();
                return;
            }
        }
    }
}
