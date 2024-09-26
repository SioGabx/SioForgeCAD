using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class COPYMODELTOPAPER
    {
        public static void ChangeSpace()
        {
            Editor ed = Generic.GetEditor();

            var CurrentViewport = ed.GetViewport();
            if (CurrentViewport == null) { return; }
            if (CurrentViewport.PerspectiveOn)
            {
                Generic.WriteMessage("Perspective views not supported");
                return;
            }
            var SelectedEnts = ed.GetSelectionRedraw();
            if (SelectedEnts.Status != PromptStatus.OK)
            {
                return;
            }
            ed.SwitchToPaperSpace();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // copy selected entites to model space
                var ids = new ObjectIdCollection(SelectedEnts.Value.GetObjectIds());
                var mapping = new IdMapping();
                var modelSpaceId = SymbolUtilityServices.GetBlockPaperSpaceId(db);
                db.DeepCloneObjects(ids, modelSpaceId, mapping, false);

                foreach (IdPair pair in mapping)
                {
                    if (pair.IsCloned && pair.IsPrimary)
                    {
                        var entity = (Entity)tr.GetObject(pair.Value, OpenMode.ForWrite);
                        entity.ModelToPaper(CurrentViewport);
                    }
                }
                tr.Commit();
            }
        }
    }
}
