using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class MERGEPOLYLIGNES
    {
        public static void Merge()
        {
            Editor ed = Generic.GetEditor();
            Document doc = Generic.GetDocument();

            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
            {
                return;
            }

            SelectionSet sel = selRes.Value;
            List<Polyline> Curves = new List<Polyline>();
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId selectedObjectId in sel.GetObjectIds())
                {
                    DBObject ent = selectedObjectId.GetDBObject();
                    if (ent is Polyline)
                    {
                        Polyline curv = ent.Clone() as Polyline;
                        Curves.Add(curv);
                    }
                }
                if (Curves.Count == 0)
                {
                    return;
                }
                PolygonOperation.Union(PolyHole.CreateFromList(Curves), out List<PolyHole> UnionResult, true);

                foreach (var polyh in UnionResult)
                {
                    polyh.Boundary.AddToDrawing(3);
                    polyh.Holes.AddToDrawing(2);
                }
                Curves.DeepDispose();
                tr.Commit();
                return;
            }
        }
    }
}
