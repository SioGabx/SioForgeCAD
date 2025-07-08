using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System.Collections.Generic;
using System.Linq;


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
            List<Layout> AllLayouts = ed.GetAllLayout();
            List<string> layoutNames = AllLayouts.ConvertAll(ele => $"{ele.TabOrder}_{ele.LayoutName}");
            if (layoutNames.Count == 0)
            {
                ed.WriteMessage("\nAucun layout disponible.");
                return null;
            }

            var res = ed.GetOptions("Sélectionnez le layout cible :", layoutNames.ToArray());
            string SelectedTabIndex = (res.StringResult ?? string.Empty).Split('_')?.First();
            string SelectedLayoutName = AllLayouts.FirstOrDefault(ele => ele.TabOrder.ToString() == SelectedTabIndex)?.LayoutName;

            return res.Status == PromptStatus.OK ? SelectedLayoutName : null;
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
