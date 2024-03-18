using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    static public class MERGEPOLYLIGNES
    {
        public static void Merge()
        {
            Editor ed = Generic.GetEditor();

            // ed.TraceBoundary(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0), false);
            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
                return;

            SelectionSet sel = selRes.Value;
            List<Polyline> Curves = new List<Polyline>();

            //ed.GetPoint("Indiquez un point");
            //var CurrentViewSave = ed.GetCurrentView();

            Document doc = Generic.GetDocument();
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
                if (Curves.Count <= 0)
                {
                    return;
                }
                PolyHole.CreateFromList(Curves).Union(out List<PolyHole> UnionResult);

                foreach (var polyh in UnionResult)
                {
                    polyh.Boundary.ColorIndex = 3;
                    polyh.Boundary.AddToDrawing();
                    polyh.Holes.AddToDrawing(2);
                }
                tr.Commit();
                return;
            }
        }
    }
}
