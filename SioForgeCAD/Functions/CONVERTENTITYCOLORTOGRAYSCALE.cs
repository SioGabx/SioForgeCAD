using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Colors;
using SioForgeCAD.Commun.Mist;

namespace SioForgeCAD.Functions
{
    public static class CONVERTENTITYCOLORTOGRAYSCALE
    {
        static double LastLumSettingValue = 100;
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

            while (true)
            {
                var PromptDouble = new PromptDoubleOptions("Définir le pourcentage de luminosité entre %")
                {
                    AllowNegative = false,
                    AllowZero = true,
                    DefaultValue = LastLumSettingValue,
                    UseDefaultValue = true
                };
                var LumSettings = ed.GetDouble(PromptDouble);
                if (LumSettings.Status != PromptStatus.OK)
                {
                    return;
                }
                LastLumSettingValue = LumSettings.Value;
                break;
            }
            var AllSelectedObjectIds = AllSelectedObject.Value.GetObjectIds();

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ObjId in AllSelectedObjectIds)
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

            var HSL = HSLColors.RGB2HSL(Gray, Gray, Gray);

            double NewLum = (HSL.L * (LastLumSettingValue / 100));
            double ClampedNewLum = Math.Min(Math.Max(NewLum, 0), 1);
            var NewRGBColor = HSLColors.HSL2RGB(HSL.H, HSL.S, ClampedNewLum);

            var Red = NewRGBColor.R;
            var Green = NewRGBColor.G;
            var Blue = NewRGBColor.B;
            return Color.FromRgb(Red, Green, Blue);
        }





    }
}
