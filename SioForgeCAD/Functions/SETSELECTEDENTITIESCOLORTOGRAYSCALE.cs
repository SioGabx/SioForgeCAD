using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class SETSELECTEDENTITIESCOLORTOGRAYSCALE
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
            //var AllSelectedObject = ed.GetSelection(PromptSelectEntitiesOptions);

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ObjId in AllSelectedObject.Value.GetObjectIds())
                {
                    ForceColor(ObjId);
                }
                ed.SetImpliedSelection(AllSelectedObject.Value.GetObjectIds());
                tr.Commit();
            }
        }

        public static void ForceColor(ObjectId SelectedObjects)
        {
            Entity SelectedEntity = SelectedObjects.GetEntity(OpenMode.ForWrite);
            string EntityLayer = SelectedEntity.Layer;
            ObjectId LayerTableRecordObjId = Layers.GetLayerIdByName(EntityLayer);

            Color BaseColor = SelectedEntity.Color;
            if (SelectedEntity.Color.IsByLayer)
            {
                BaseColor = Layers.GetLayerColor(LayerTableRecordObjId);
            }
            SelectedEntity.Color = BaseColor.ConvertColorToGray();

            if (SelectedEntity is Hatch SelectedEntityHatch)
            {
                if (SelectedEntityHatch.BackgroundColor.IsByLayer)
                {
                    var LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
                    SelectedEntityHatch.BackgroundColor = LayerColor.ConvertColorToGray();
                }
                else
                {
                    SelectedEntityHatch.BackgroundColor = SelectedEntityHatch.BackgroundColor.ConvertColorToGray();
                }
            }
        }
    }
}
