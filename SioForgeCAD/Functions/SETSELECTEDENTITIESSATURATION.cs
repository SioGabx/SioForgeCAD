using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;

namespace SioForgeCAD.Functions
{
    public static class SETSELECTEDENTITIESSATURATION
    {
        static double LastSaturationValue = 100;

        public static void Set()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            var PromptSelectEntitiesOptions = new PromptSelectionOptions()
            {
                MessageForAdding = "Sélectionnez les entités"
            };

            var AllSelectedObject = ed.GetSelectionRedraw(PromptSelectEntitiesOptions);
            //var AllSelectedObject = ed.GetSelection(PromptSelectEntitiesOptions);

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }

            while (true)
            {
                var PromptDouble = new PromptDoubleOptions("\nDéfinir le pourcentage de saturation")
                {
                    AllowNegative = false,
                    AllowZero = true,
                    DefaultValue = LastSaturationValue,
                    UseDefaultValue = true
                };

                var SaturationValue = ed.GetDouble(PromptDouble);
                if (SaturationValue.Status != PromptStatus.OK)
                {
                    return;
                }
                LastSaturationValue = SaturationValue.Value;
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
            SelectedEntity.Color = SetSaturation(BaseColor);

            if (SelectedEntity is Hatch SelectedEntityHatch)
            {
                if (SelectedEntityHatch.BackgroundColor.IsByLayer)
                {
                    var LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
                    SelectedEntityHatch.BackgroundColor = SetSaturation(LayerColor);
                }
                else
                {
                    SelectedEntityHatch.BackgroundColor = SetSaturation(SelectedEntityHatch.BackgroundColor);
                }
            }
        }

        public static Color SetSaturation(Color BaseColor)
        {
            var hsv = Colors.ColorToHSV(BaseColor);
            // Multiplicateur basé sur le pourcentage (100 = 1.0, 50 = 0.5, 200 = 2.0)
            double SaturationFactor = LastSaturationValue / 100.0;
            double newSaturation = hsv.saturation * SaturationFactor;
            newSaturation = Math.Max(0.0, Math.Min(1.0, newSaturation));
            return Colors.FromHSV(hsv.hue, newSaturation, hsv.value);
        }
    }
}