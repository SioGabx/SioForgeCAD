using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

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
                List<ObjectId> SelectionObjId = new List<ObjectId>();
                if (ed.IsInModel()) { return; }
                if (ed.IsInLayoutViewport())
                {
                    SelectionObjId.Add(ed.CurrentViewportObjectId);
                }

                if (SelectionObjId.Count == 0)
                {
                    var Selection = ed.GetSelectionRedraw(
                    "Selectionnez une ou plusieurs Fenetres",
                    false,
                    false,
                    new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "VIEWPORT") }));

                    if (Selection.Status != PromptStatus.OK)
                    {
                        return;
                    }
                    SelectionObjId = Selection.Value.GetObjectIds().ToList();
                }

                foreach (var SelectionItemObjId in SelectionObjId)
                {
                    Viewport viewport = SelectionItemObjId.GetDBObject(OpenMode.ForRead) as Viewport;
                    DrawOutline(viewport);
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
                        Viewport viewport = VpObjId.GetDBObject(OpenMode.ForRead) as Viewport;
                        DrawOutline(viewport);
                    }

                }
                tr.Commit();
            }
        }


        private static void DrawOutline(Viewport viewport)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ViewportBoundary = viewport.GetBoundary();
                ViewportBoundary.PaperToModel(viewport);
                BlockTableRecord blockTableRecord = (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetDBObject(OpenMode.ForWrite);
                blockTableRecord.AppendEntity(ViewportBoundary);
                tr.AddNewlyCreatedDBObject(ViewportBoundary, true);
                tr.Commit();
            }
        }
    }
}
