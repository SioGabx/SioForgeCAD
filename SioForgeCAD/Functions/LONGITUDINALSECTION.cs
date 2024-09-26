using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class LONGITUDINALSECTION
    {
        public static void DrawSectionFromSelectedPoints()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Polyline TerrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne definissant la section", false))
            {
                if (TerrainBasePolyline == null)
                {
                    return;
                }
                TypedValue[] EntitiesGroupCodesList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                SelectionFilter SelectionEntitiesFilter = new SelectionFilter(EntitiesGroupCodesList);
                PromptSelectionOptions PromptBlocSelectionOptions = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelectionnez des côtes à projeter",
                    RejectObjectsOnLockedLayers = false
                };
                var BlockRefSelection = ed.GetSelection(PromptBlocSelectionOptions, SelectionEntitiesFilter);
                if (BlockRefSelection.Status != PromptStatus.OK) { return; }

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    ObjectId[] SelectedCoteBloc = BlockRefSelection.Value.GetObjectIds();

                    Polyline profilePolyline = new Polyline();

                    //TODO





                    HightLighter.UnhighlightAll(SelectedCoteBloc);
                    trans.Commit();
                }
            }
        }
    }
}
