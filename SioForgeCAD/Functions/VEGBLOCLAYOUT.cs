using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System.Collections.Generic;


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
                string layoutName = PromptForLayoutName();
                if (string.IsNullOrEmpty(layoutName)) return;

                var layout = ed.GetLayoutFromName(layoutName);
                if (layout == null) return;

                var viewport = GetViewport(layout);
                var boundary = viewport?.GetBoundary();
                boundary?.PaperToModel(viewport);

                if (boundary == null)
                {
                    Generic.WriteMessage($"Aucun viewport trouvé dans le layout {layoutName}.");
                    tr.Commit();
                    return;
                }

                var vectors = new List<Vector3d>();
                bool Continue = true;
                while (Continue)
                {
                    Continue = false;
                    CollectVectors(boundary, vectors);
                    if (vectors.Count > 0)
                    {
                        var confirm = ed.GetOptions("Voulez-vous terminer et générer les présentations ?", "Générer", "Continuer", "Annuler");
                        if (confirm.Status == PromptStatus.OK)
                        {
                            if (confirm.StringResult == "Générer")
                            {
                                GenerateLayoutFromVectors(layout, vectors);
                            }
                            else if (confirm.StringResult == "Continuer")
                            {
                                Continue = true;
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }

        private static List<Vector3d> CollectVectors(Polyline boundary, List<Vector3d> vectors)
        {
            Editor ed = Generic.GetEditor();
            var centroid = boundary.GetCentroid();
            var toOrigin = centroid.GetVectorTo(Point3d.Origin);
            boundary.TransformBy(Matrix3d.Displacement(toOrigin));

            bool AlwaysTrue(Points pt, GetPointJig gpj) => true;

            using (var jig = new GetPointJig
            {
                Entities = new DBObjectCollection { boundary },
                StaticEntities = new DBObjectCollection(),
                UpdateFunction = AlwaysTrue,
            })
            {
                while (true)
                {
                    var res = jig.GetPoint("Indiquez les emplacements des présentations", "Terminer");

                    if (res.PromptPointResult.Status == PromptStatus.OK)
                    {
                        var target = res.Point.SCG;

                        if (res.PromptPointResult.Status == PromptStatus.Keyword)
                        {
                            return vectors;
                        }

                        var clone = boundary.Clone() as Polyline;
                        clone.TransformBy(Matrix3d.Displacement(Point3d.Origin.GetVectorTo(target)));
                        jig.StaticEntities.Add(clone);

                        vectors.Add(centroid.GetVectorTo(target));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return vectors;
        }


        //public static void Create()
        //{
        //    Editor ed = Generic.GetEditor();
        //    Database db = Generic.GetDatabase();
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        //WE NEED TO MAKE LAYOUT CURRENT !!!
        //        string layoutName = PromptForLayoutName();
        //        if (string.IsNullOrEmpty(layoutName)) return;
        //        var Layout = ed.GetLayoutFromName(layoutName);
        //        if (Layout == null) return;
        //        var ViewPort = GetViewport(Layout);
        //        var ViewportBoundary = ViewPort.GetBoundary();
        //        ViewportBoundary.PaperToModel(ViewPort);

        //        if (ViewportBoundary == null)
        //        {
        //            Generic.WriteMessage($"Aucun viewport rectangulaire trouvé dans le layout {layoutName}.");
        //            tr.Commit();
        //            return;
        //        }
        //        var ViewPortBoundaryCentroid = ViewportBoundary.GetCentroid();
        //        var vect = ViewPortBoundaryCentroid.GetVectorTo(Point3d.Origin);
        //        ViewportBoundary.TransformBy(Matrix3d.Displacement(vect));

        //        List<Vector3d> vectors = new List<Vector3d>();
        //        bool UpdateFunc(Points pt, GetPointJig gpj) { return true; }
        //        using (var GetPointJig = new GetPointJig()
        //        {
        //            Entities = new DBObjectCollection() { ViewportBoundary },
        //            StaticEntities = new DBObjectCollection() { },
        //            UpdateFunction = UpdateFunc,
        //        })
        //        {
        //            while (true)
        //            {
        //                var GetPointTransientResult = GetPointJig.GetPoint("Indiquez les emplacements des présentations", "Terminer");
        //                if (GetPointTransientResult.PromptPointResult.Status == PromptStatus.OK)
        //                {
        //                    if (GetPointTransientResult.PromptPointResult.Status == PromptStatus.Keyword)
        //                    {
        //                        //Generate all
        //                        GenerateLayout(Layout, vectors);
        //                        break;
        //                    }
        //                    else
        //                    {
        //                        var ViewportBoundaryCloned = ViewportBoundary.Clone() as Autodesk.AutoCAD.DatabaseServices.Polyline;
        //                        ViewportBoundaryCloned.TransformBy(Matrix3d.Displacement((Point3d.Origin).GetVectorTo(GetPointTransientResult.Point.SCG)));
        //                        GetPointJig.StaticEntities.Add(ViewportBoundaryCloned);

        //                        var TransformVector = ViewPortBoundaryCentroid.GetVectorTo(GetPointTransientResult.Point.SCG);
        //                        vectors.Add(TransformVector);
        //                    }
        //                }
        //                else if (vectors.Count > 0)
        //                {
        //                    var Generate = ed.GetOptions("Voullez terminer et générer les présentations ?", "Oui", "Non");
        //                    if (Generate.Status == PromptStatus.OK && Generate.StringResult == "Oui")
        //                    {
        //                        GenerateLayout(Layout, vectors);
        //                    }
        //                    break;
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }
        //        }

        //        tr.Commit();
        //    }
        //}

        private static void GenerateLayoutFromVectors(Layout BaseLayout, List<Vector3d> Vectors)
        {
            Editor ed = Generic.GetEditor();

            foreach (var vector in Vectors)
            {
                var CloneName = GetUniqueLayoutName(BaseLayout.LayoutName);
                BaseLayout.CloneLayout(CloneName);
                var Layout = ed.GetLayoutFromName(CloneName);
                var ViewPort = GetViewport(Layout);
                if (!ViewPort.IsWriteEnabled) { ViewPort.UpgradeOpen(); }

                ViewPort.Locked = false;
                ViewPort.ViewCenter = ViewPort.ViewCenter.TransformBy(Matrix2d.Displacement(vector.ToVector2d()));
                ViewPort.Locked = true;
            }
        }

        public static string GetUniqueLayoutName(string baseName)
        {
            Editor ed = Generic.GetEditor();
            var ListOfLayoutNames = ed.GetAllLayout().ConvertAll(ele => ele.LayoutName);

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                string newName = baseName;
                for (int index = 1; ListOfLayoutNames.Contains(newName); index++)
                {
                    newName = string.Format("{0}_{1}", baseName, index.ToString("D2"));
                }
                return SymbolUtilityServices.RepairSymbolName(newName, false);
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

            var res = ed.GetOptions("Sélectionnez le layout cible :", layoutNames.ToArray());
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
