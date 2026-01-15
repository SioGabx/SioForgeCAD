using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;

namespace SioForgeCAD.Functions
{
    //https://www.codeproject.com/articles/Manipulating-colors-in-NET-Part-1#hsb
    public static class SETSELECTEDENTITIESBRIGHTNESS
    {
        static double LastBrightnessValue = 100;
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
                var PromptDouble = new PromptDoubleOptions("Définir le pourcentage de luminosité")
                {
                    AllowNegative = false,
                    AllowZero = true,
                    DefaultValue = LastBrightnessValue,
                    UseDefaultValue = true
                };
                var BrightnessValue = ed.GetDouble(PromptDouble);
                if (BrightnessValue.Status != PromptStatus.OK)
                {
                    return;
                }
                LastBrightnessValue = BrightnessValue.Value;
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
            SelectedEntity.Color = SetBrightness(BaseColor);

            if (SelectedEntity is Hatch SelectedEntityHatch)
            {
                if (SelectedEntityHatch.BackgroundColor.IsByLayer)
                {
                    var LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
                    SelectedEntityHatch.BackgroundColor = SetBrightness(LayerColor);
                }
                else
                {
                    SelectedEntityHatch.BackgroundColor = SetBrightness(SelectedEntityHatch.BackgroundColor);
                }
            }
        }

        public static Color SetBrightness(Color BaseColor)
        {
            var DrawingColor = BaseColor.ColorValue;
            double BrightnessFactor = (LastBrightnessValue / 100.0) - 1;
            var NewRGBColor = Colors.SetBrightness(BrightnessFactor, DrawingColor.R, DrawingColor.G, DrawingColor.B);
            var Red = (byte)NewRGBColor.R;
            var Green = (byte)NewRGBColor.G;
            var Blue = (byte)NewRGBColor.B;
            return Color.FromRgb(Red, Green, Blue);
        }
    }
}
