using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class VIEWPORTOUTLINE
    {
        public static void OutlineSelected()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Layout layout = ed.GetLayoutFromName(LayoutManager.Current.CurrentLayout);
                if (ed.IsInLayout())
                {
                    var btr = layout.BlockTableRecordId.GetDBObject(OpenMode.ForRead) as BlockTableRecord;
                    foreach (ObjectId VpObjId in ed.GetAllViewportsInPaperSpace(btr))
                    {
                        if (VpObjId.GetDBObject(OpenMode.ForRead) is Viewport viewport)
                        {
                            DrawOutline(viewport, layout.LayoutName);
                        }
                    }
                }
                tr.Commit();
            }
        }

        public static void OutlineAll(bool SelectedOnly = true)
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (Layout layout in ed.GetAllLayout())
                {
                    if (SelectedOnly && !layout.TabSelected) { continue; }
                    var btr = layout.BlockTableRecordId.GetDBObject(OpenMode.ForRead) as BlockTableRecord;
                    foreach (ObjectId VpObjId in ed.GetAllViewportsInPaperSpace(btr))
                    {
                        if (VpObjId.GetDBObject(OpenMode.ForRead) is Viewport viewport)
                        {
                            DrawOutline(viewport, layout.LayoutName);
                        }
                    }
                }
                tr.Commit();
            }
        }

        private static void DrawOutline(Viewport viewport, string layoutName)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ViewportBoundary = viewport.GetBoundary();
                if (ViewportBoundary == null) { return; }
                ViewportBoundary.PaperToModel(viewport);
                BlockTableRecord blockTableRecord = (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetDBObject(OpenMode.ForWrite);
                blockTableRecord.AppendEntity(ViewportBoundary);

                tr.AddNewlyCreatedDBObject(ViewportBoundary, true);

                Point3d centroid = ViewportBoundary.GetInnerCentroid();


                // Création du texte centré
                DBText label = new DBText
                {
                    Position = centroid,
                    TextString = layoutName,
                    Height = 1.5, // taille du texte, à adapter selon l’échelle
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    AlignmentPoint = centroid
                };

                blockTableRecord.AppendEntity(label);
                tr.AddNewlyCreatedDBObject(label, true);


                tr.Commit();
            }
        }
    }
}
