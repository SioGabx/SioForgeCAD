using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class FORCELAYERCOLORTOENTITY
    {
        public static void Convert()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            var PromptSelectEntitiesOptions = new PromptSelectionOptions()
            {
                MessageForAdding = "Selectionnez les entités"
            };

            var AllSelectedObject = ed.GetSelectionRedraw(PromptSelectEntitiesOptions);

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }
            var AllSelectedObjectIds = AllSelectedObject.Value.GetObjectIds();

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ObjId in AllSelectedObjectIds)
                {
                    ForceColor(ObjId);
                }
                ed.SetImpliedSelection(AllSelectedObjectIds);
                tr.Commit();
            }
        }

        public static void ForceColor(ObjectId SelectedObjects)
        {
            Entity SelectedEntity = SelectedObjects.GetEntity(OpenMode.ForWrite);
            string EntityLayer = SelectedEntity.Layer;
            ObjectId LayerTableRecordObjId = Layers.GetLayerIdByName(EntityLayer);
            Autodesk.AutoCAD.Colors.Color LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
            if (SelectedEntity.Color.IsByLayer)
            {
                SelectedEntity.Color = LayerColor;
            }
            if (SelectedEntity is Hatch SelectedEntityHatch)
            {
                if (SelectedEntityHatch.BackgroundColor.IsByLayer)
                {
                    SelectedEntityHatch.BackgroundColor = LayerColor;
                }
            }
        }
    }
}
