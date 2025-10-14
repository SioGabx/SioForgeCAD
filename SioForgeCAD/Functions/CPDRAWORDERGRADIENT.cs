using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class CPDRAWORDERGRADIENT
    {
        public static void Compute()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 0. Sélection des entités
            PromptSelectionResult selRes = ed.GetSelectionRedraw();
            if (selRes.Status != PromptStatus.OK) return;

            SelectionSet selSet = selRes.Value;

            if (selSet.Count == 0)
            {
                Generic.WriteMessage("Aucune entité sélectionnée.");
                return;
            }

            CoordinateSystem3d ucs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;
            Matrix3d wcsToUcs = ed.CurrentUserCoordinateSystem.Inverse();

            // 1. Vecteur de référence
            PromptPointResult ppr1 = ed.GetPoint("\nPoint de départ du vecteur : ");
            if (ppr1.Status != PromptStatus.OK) return;

            PromptPointOptions ppo = new PromptPointOptions("\nPoint d'arrivée du vecteur : ")
            {
                BasePoint = ppr1.Value,
                UseBasePoint = true
            };
            PromptPointResult ppr2 = ed.GetPoint(ppo);
            if (ppr2.Status != PromptStatus.OK) return;

            Vector3d vecteur = (ppr2.Value - ppr1.Value).GetNormal();
            if (vecteur.Length < Tolerance.Global.EqualPoint)
            {
                Generic.WriteMessage("Vecteur nul, opération annulée.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                DrawOrderTable dot = (DrawOrderTable)tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite);

                List<(Entity ent, double projection)> entitesAvecDistance = new List<(Entity ent, double projection)>();

                foreach (SelectedObject selObj in selSet)
                {
                    if (selObj == null || selObj.ObjectId.IsNull) continue;

                    if (!(selObj.ObjectId.GetDBObject() is Entity ent) || ent.IsErased)
                    {
                        continue;
                    }

                    Point3d pointRef;

                    if (ent is BlockReference br)
                    {
                        pointRef = br.Position;
                    }
                    else
                    {
                        Extents3d ext = ent.GetExtents();
                        pointRef = ext.GetCenter();
                    }

                    Point3d pointUcs = pointRef.TransformBy(wcsToUcs);
                    Point3d ppr1Ucs = ppr1.Value.TransformBy(wcsToUcs);
                    Vector3d vFromOrigin = pointUcs - ppr1Ucs;
                    double projection = vFromOrigin.DotProduct(vecteur);
                    entitesAvecDistance.Add((ent, projection));
                }

                if (entitesAvecDistance.Count == 0)
                {
                    Generic.WriteMessage("Aucune entité valide sélectionnée.");
                    return;
                }

                var sortedIds = entitesAvecDistance
                    .OrderByDescending(e => e.projection)
                    .Select(e => e.ent.ObjectId)
                    .ToArray();

                dot.SetRelativeDrawOrder(sortedIds.ToObjectIdCollection());

                Generic.WriteMessage($"{sortedIds.Length} entités triées par ordre d'affichage.");
                tr.Commit();
                Generic.Regen();
            }
        }
    }
}
