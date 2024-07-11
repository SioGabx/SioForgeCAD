using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class SCALERANDOM
    {
        public static double LastMinShrinkageRatio = 1;
        public static double LastMaxShrinkageRatio = 1;
        public static Random Random = null;
        public static void Scale()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                var (MinShrinkage, MaxShrinkage) = GetRatios();
                LastMinShrinkageRatio = MinShrinkage;
                LastMaxShrinkageRatio = MaxShrinkage;

                if (Random is null)
                {
                    Random = new Random();
                }
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId selObjId in selResult.Value.GetObjectIds())
                    {
                        double Ratio = MinShrinkage + (Random.NextDouble() * (MaxShrinkage - MinShrinkage));
                        ApplyScale(Ratio, selObjId);
                    }
                    tr.Commit();
                }

            }
        }

        public static (double MinShrinkage, double MaxShrinkage) GetRatios()
        {
            var ed = Generic.GetEditor();

            // Demander la valeur minimum de rétrécissement
            PromptDoubleOptions minOptions = new PromptDoubleOptions("\nEntrez la valeur minimum de rétrécissement: ")
            {
                AllowNegative = false,
                AllowZero = true,
                DefaultValue = LastMinShrinkageRatio,
                UseDefaultValue = true,
            };
            PromptDoubleResult minResult = ed.GetDouble(minOptions);
            if (minResult.Status != PromptStatus.OK)
            {
                return (-1, -1);
            }
            double minShrinkage = minResult.Value;

            // Demander la valeur maximum de rétrécissement
            PromptDoubleOptions maxOptions = new PromptDoubleOptions("\nEntrez la valeur maximum de rétrécissement: ")
            {
                AllowNegative = false,
                AllowZero = true,
                DefaultValue = LastMaxShrinkageRatio,
                UseDefaultValue = true,
            };
            PromptDoubleResult maxResult = ed.GetDouble(maxOptions);
            if (maxResult.Status != PromptStatus.OK)
            {
                return (-1, -1);
            }
            double maxShrinkage = maxResult.Value;

            return (Math.Min(minShrinkage, maxShrinkage), Math.Max(minShrinkage, maxShrinkage));
        }

        public static void ApplyScale(double Ratio, ObjectId selObjId)
        {
            if (selObjId.GetDBObject(OpenMode.ForWrite) is Entity ent)
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



    }
}
