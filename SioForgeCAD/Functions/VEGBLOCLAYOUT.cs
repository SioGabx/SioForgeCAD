using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace SioForgeCAD.Functions
{
    public static class VEGBLOCLAYOUT
    {
        public static void Create()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //WE NEED TO MAKE LAYOUT CURRENT !!!
                string layoutName = PromptForLayoutName();
                if (string.IsNullOrEmpty(layoutName)) return;
                var Layout = ed.GetLayoutFromName(layoutName);
                if (Layout == null) return; 
                var ViewPort = GetViewport(Layout);
                var ViewportBoundary = ViewPort.GetBoundary();
                ViewportBoundary.PaperToModel(ViewPort);

                if (ViewportBoundary == null)
                {
                    Generic.WriteMessage($"Aucun viewport rectangulaire trouvé dans le layout {layoutName}.");
                    tr.Commit();
                    return;
                }
                var ViewPortBoundaryCentroid = ViewportBoundary.GetCentroid();
                var vect = ViewPortBoundaryCentroid.GetVectorTo(Point3d.Origin);
                ViewportBoundary.TransformBy(Matrix3d.Displacement(vect));

                bool UpdateFunc(Points pt, GetPointJig gpj) { return true; }
                using (var GetPointJig = new GetPointJig()
                {
                    Entities = new DBObjectCollection() { ViewportBoundary },
                    StaticEntities = new DBObjectCollection() { },
                    UpdateFunction = UpdateFunc,
                })
                {
                    var GetPointTransientResult = GetPointJig.GetPoint("Indiquez les emplacements des points cote", "VALIDER");
                    if (GetPointTransientResult.PromptPointResult.Status == PromptStatus.OK)
                    {
                        var ViewportBoundaryCloned = ViewportBoundary.Clone() as Autodesk.AutoCAD.DatabaseServices.Polyline;
                        ViewportBoundaryCloned.TransformBy(Matrix3d.Displacement((Point3d.Origin).GetVectorTo(GetPointTransientResult.Point.SCG)));
                        ViewportBoundaryCloned.AddToDrawing();

                        var TransformVector = ViewPortBoundaryCentroid.GetVectorTo(GetPointTransientResult.Point.SCG);

                        if (!ViewPort.IsWriteEnabled) { ViewPort.UpgradeOpen(); }
                        ViewPort.Locked = false;
                        ViewPort.ViewCenter = ViewPort.ViewCenter.TransformBy(Matrix2d.Displacement(TransformVector.ToVector2d()));
                        ViewPort.Locked = true;
                        var CloneName = ed.GetUniqueLayoutName(Layout.LayoutName);
                        Layout.CloneLayout(CloneName);
                    }
                }

                tr.Commit();
            }
        }

        private static string PromptForLayoutName()
        {
            Editor ed = Generic.GetEditor();
            List<string> layoutNames = ed.GetAllLayout().ConvertAll(ele => ele.LayoutName);
            if (layoutNames.Count == 0)
            {
                ed.WriteMessage("\nAucun layout disponible.");
                return null;
            }

            PromptResult res = ed.GetOptions("Sélectionnez le layout cible :", layoutNames.ToArray());
            return res.Status == PromptStatus.OK ? res.StringResult : null;
        }

        private static Viewport GetViewport(Layout layout)
        {
            Editor ed = Generic.GetEditor();
            var btr = layout.BlockTableRecordId.GetDBObject(OpenMode.ForRead) as BlockTableRecord;
            foreach (ObjectId VpObjId in ed.GetAllViewportsInPaperSpace(btr))
            {
                if (VpObjId.GetDBObject(OpenMode.ForRead) is Viewport viewport)
                {
                    return viewport;
                }
            }
            return null;
        }
    }
}
