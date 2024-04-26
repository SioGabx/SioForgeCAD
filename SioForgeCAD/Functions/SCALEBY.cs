using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class SCALEBY
    {
        public static double LastScaleByRatio = 1;
        public static void ScaleBy()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Indiquez une echelle d'agrandissement ou de réduction")
                {
                    AllowArbitraryInput = true,
                    AllowNegative = false,
                    AllowZero = false,
                    DefaultValue = LastScaleByRatio,
                };
                var AskRatioResult = ed.GetDouble(promptDoubleOptions);
                if (AskRatioResult.Status == PromptStatus.OK)
                {
                    double Ratio = AskRatioResult.Value;
                    LastScaleByRatio = Ratio;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject selObj in selResult.Value)
                        {
                            if (selObj?.ObjectId.GetDBObject(OpenMode.ForWrite) is Entity ent)
                            {
                                var TransformCenter = ent.GetExtents().GetCenter();
                                if (ent is BlockReference blkRef)
                                {
                                    TransformCenter = blkRef.Position;
                                }
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
