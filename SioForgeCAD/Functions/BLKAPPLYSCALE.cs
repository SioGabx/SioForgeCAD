using Autodesk.AutoCAD.ApplicationServices;
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
    public static class BLKAPPLYSCALE
    {
        public static void ApplyBlockScale()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetBlock(out ObjectId perObjId))
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!(perObjId.GetDBObject(OpenMode.ForWrite) is BlockReference br))
                {
                    return;
                }

                if (!IsUniformScale(br))
                {
                    Generic.WriteMessage("Le bloc n'a pas une échelle uniforme.");
                    return;
                }

                double refScale = br.ScaleFactors.X;

                if (Math.Abs(refScale - 1.0) < Generic.LowTolerance.EqualVector)
                {
                    Generic.WriteMessage("Le bloc est déjà à l'échelle 1.");
                    return;
                }

                BlockTableRecord btr = br.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;

                Matrix3d scaleMatrix = Matrix3d.Scaling(refScale, Point3d.Origin);
                foreach (ObjectId entId in btr)
                {
                    Entity ent = entId.GetDBObject(OpenMode.ForWrite) as Entity;
                    ent?.TransformBy(scaleMatrix);
                }

                // Fix all ref blk
                ObjectIdCollection refIds = btr.GetBlockReferenceIds(true, true); // get all including nested

                bool differentScalesFound = false;

                foreach (ObjectId id in refIds)
                {
                    if (!(id.GetDBObject(OpenMode.ForWrite) is BlockReference otherBr))
                    {
                        continue;
                    }

                    if (!IsUniformScale(otherBr))
                    {
                        continue;
                    }

                    double oldScale = otherBr.ScaleFactors.X;
                    double newScale = oldScale / refScale;

                    if (Math.Abs(oldScale - refScale) > Generic.LowTolerance.EqualVector)
                    {
                        differentScalesFound = true;
                    }

                    otherBr.ScaleFactors = new Scale3d(newScale);
                }

                if (differentScalesFound)
                {
                    Generic.WriteMessage("⚠ Certaines références avaient une échelle différente. Les proportions ont été conservées.");
                }
                br.RegenAllBlkDefinition();
                br.RegenParentBlocksRecursive();
                tr.Commit();
            }
        }

        private static bool IsUniformScale(BlockReference br)
        {
            return Math.Abs(br.ScaleFactors.X - br.ScaleFactors.Y) < Generic.LowTolerance.EqualVector &&
                   Math.Abs(br.ScaleFactors.X - br.ScaleFactors.Z) < Generic.LowTolerance.EqualVector;
        }
    }
}
