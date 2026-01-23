using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            if (!ed.GetBlocks(out ObjectId[] perObjIds))
            {
                return;
            }

            HashSet<string> AlreadyAppliedScale = new HashSet<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var perObjId in perObjIds)
                {
                    if (!(perObjId.GetDBObject(OpenMode.ForRead) is BlockReference blockRef))
                    {
                        continue;
                    }

                    var BlkName = blockRef.GetBlockReferenceName();

                    if (AlreadyAppliedScale.Contains(BlkName))
                    {
                        //We have multiple instances of the blk in the array, ignore if already parsed
                        continue;
                    }

                    if (!IsUniformScaleAllowNegative(blockRef))
                    {
                        Generic.WriteMessage($"Le bloc \"{BlkName}\" n'a pas une échelle uniforme.");
                        continue;
                    }
                    AlreadyAppliedScale.Add(BlkName);

                    double refScale = Math.Abs(blockRef.ScaleFactors.X);

                    BlockTableRecord btr = blockRef.GetBlocDefinition(OpenMode.ForWrite);

                    if (Math.Abs(refScale - 1.0) < Generic.LowTolerance.EqualVector && btr.Units == db.Insunits)
                    {
                        Generic.WriteMessage($"Le bloc \"{BlkName}\" est déjà à l'échelle 1.");
                        continue;
                    }

                    if (btr.Units != db.Insunits)
                    {
                        btr.Units = db.Insunits;
                    }

                    Matrix3d scaleMatrix = Matrix3d.Scaling(refScale, Point3d.Origin);
                    foreach (ObjectId entId in btr)
                    {
                        try
                        {
                            Entity ent = entId.GetDBObject(OpenMode.ForWrite) as Entity;
                            ent?.TransformBy(scaleMatrix);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }



                    // Fix all ref blk
                    bool differentScalesFound = false;
                    foreach (ObjectId item in blockRef.GetAllBlkDefinition())
                    {
                        BlockReference ent = item.GetDBObject(OpenMode.ForWrite) as BlockReference;

                        var oldScale = ent.ScaleFactors;

                        if (Math.Abs(oldScale.X - refScale) > Generic.LowTolerance.EqualVector)
                        {
                            differentScalesFound = true;
                        }

                        double scaleFactor = 1.0 / refScale;

                        ent.ScaleFactors = new Scale3d(
                            oldScale.X * scaleFactor,
                            oldScale.Y * scaleFactor,
                            oldScale.Z * scaleFactor
                        );
                    }

                    if (differentScalesFound)
                    {
                        Generic.WriteMessage($"⚠ Certaines références du bloc \"{BlkName}\" avaient une échelle différente. Les proportions ont été conservées.");
                    }
                    blockRef.RegenAllBlkDefinition();
                }
                tr.Commit();
            }
        }

        private static bool IsUniformScaleAllowNegative(BlockReference br)
        {
            return Math.Abs(Math.Abs(br.ScaleFactors.X) - Math.Abs(br.ScaleFactors.Y)) < Generic.LowTolerance.EqualVector &&
                   Math.Abs(Math.Abs(br.ScaleFactors.X) - Math.Abs(br.ScaleFactors.Z)) < Generic.LowTolerance.EqualVector;
        }
    }
}
