using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Functions
{
    public static class SCALEFIT
    {
        public static double LastScaleFitTargetSize = 1;
        public static void ScaleFit()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions($"Indiquez la distance que vous souhaitez définir pour la plus grande largeur {(selResult.Value.Count > 1 ? "de l'entité" : "des entités séléctionnées")}")
                {
                    AllowArbitraryInput = true,
                    AllowNegative = false,
                    AllowZero = false,
                    DefaultValue = LastScaleFitTargetSize,
                };
                var AskRatioResult = ed.GetDouble(promptDoubleOptions);
                if (AskRatioResult.Status == PromptStatus.OK)
                {
                    double TargetSize = AskRatioResult.Value;
                    LastScaleFitTargetSize = TargetSize;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject selObj in selResult.Value)
                        {
                            if (selObj?.ObjectId.GetDBObject(OpenMode.ForWrite) is Entity ent)
                            {
                                var EntExtend = ent.GetExtents();
                                var TransformCenter = EntExtend.GetCenter();
                                var EntExtendSize = EntExtend.Size();
                                var MaxSize = Math.Max(EntExtendSize.Width, EntExtendSize.Height);

                                if (ent is BlockReference blkRef)
                                {
                                    TransformCenter = blkRef.Position;
                                }
                                var Ratio = TargetSize / MaxSize;
                                Matrix3d scaleMatrix = Matrix3d.Scaling(Ratio, TransformCenter);
                                ent.TransformBy(scaleMatrix);
                            }
                        }
                        tr.Commit();
                    }
                }
            }
        }
    }
}
