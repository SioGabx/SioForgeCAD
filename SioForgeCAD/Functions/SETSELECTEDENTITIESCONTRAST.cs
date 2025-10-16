using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;

namespace SioForgeCAD.Functions
{
    public static class SETSELECTEDENTITIESCONTRAST
    {
        static double LastContrastValue = 100;
        public static void Set()
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

            while (true)
            {
                var PromptDouble = new PromptDoubleOptions("Définir le pourcentage de contraste")
                {
                    AllowNegative = false,
                    AllowZero = true,
                    DefaultValue = LastContrastValue,
                    UseDefaultValue = true
                };
                var ContrastValue = ed.GetDouble(PromptDouble);
                if (ContrastValue.Status != PromptStatus.OK)
                {
                    return;
                }
                LastContrastValue = ContrastValue.Value;
                break;
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
            SelectedEntity.Color = SetContrast(BaseColor);

            if (SelectedEntity is Hatch SelectedEntityHatch)
            {
                if (SelectedEntityHatch.BackgroundColor.IsByLayer)
                {
                    var LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
                    SelectedEntityHatch.BackgroundColor = SetContrast(LayerColor);
                }
                else
                {
                    SelectedEntityHatch.BackgroundColor = SetContrast(SelectedEntityHatch.BackgroundColor);
                }
            }
        }

        public static Color SetContrast(Color BaseColor)
        {
            var DrawingColor = BaseColor.ColorValue;
            double BrightnessFactor = (LastContrastValue / 100.0) - 1;
            var NewRGBColor = Colors.SetContrast(BrightnessFactor, DrawingColor.R, DrawingColor.G, DrawingColor.B);
            var Red = (byte)NewRGBColor.R;
            var Green = (byte)NewRGBColor.G;
            var Blue = (byte)NewRGBColor.B;
            return Color.FromRgb(Red, Green, Blue);
        }
    }
}
