using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;

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

            var AllSelectedObject = ed.GetSelection(PromptSelectEntitiesOptions);

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
            SelectedEntity.Color = ConvertColorToGray(BaseColor);

            if (SelectedEntity is Hatch SelectedEntityHatch)
            {
                if (SelectedEntityHatch.BackgroundColor.IsByLayer)
                {
                    var LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
                    SelectedEntityHatch.BackgroundColor = ConvertColorToGray(LayerColor);
                }
                else
                {
                    SelectedEntityHatch.BackgroundColor = ConvertColorToGray(SelectedEntityHatch.BackgroundColor);
                }
            }
        }

        public static Color ConvertColorToGray(Color BaseColor)
        {
            var DrawingColor = BaseColor.ColorValue;
            byte Gray = (byte)((0.2989 * DrawingColor.R) + (0.5870 * DrawingColor.G) + (0.1140 * DrawingColor.B));
            return Color.FromRgb(Gray, Gray, Gray);
        }
    }
}
