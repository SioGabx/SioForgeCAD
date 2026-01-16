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
                if (!(perObjId.GetDBObject(OpenMode.ForRead) is BlockReference blockRef))
                {
                    return;
                }
                var BlkDef = blockRef.GetBlocDefinition(OpenMode.ForWrite);

                var SelectedEntClone = new Circle(Point3d.Origin, Vector3d.ZAxis, 1);
                SelectedEntClone.ColorIndex = 1;
                BlkDef.AppendEntity(SelectedEntClone);
                tr.AddNewlyCreatedDBObject(SelectedEntClone, true);
                blockRef.RegenAllBlkDefinition();
                tr.Commit();
                
                return;








                //PPPPPPPPPPPPPPPPPPPPPPP
                if (!(perObjId.GetDBObject(OpenMode.ForWrite) is BlockReference br))
                {
                    return;
                }


                if (!IsUniformScaleAllowNegative(br))
                {
                    Generic.WriteMessage("Le bloc n'a pas une échelle uniforme.");
                    return;
                }

                double refScale = Math.Abs(br.ScaleFactors.X);
                
                BlockTableRecord btr = br.GetBlocDefinition(OpenMode.ForWrite);

                if (Math.Abs(refScale - 1.0) < Generic.LowTolerance.EqualVector && btr.Units == db.Insunits)
                {
                    Generic.WriteMessage("Le bloc est déjà à l'échelle 1.");
                    return;
                }

                if (btr.Units != db.Insunits)
                {
                    btr.Units = db.Insunits;
                }
                var Blkname = br.GetBlockReferenceName();

                if (true)
                {
                    Generic.WriteMessage("Opération sur des blocks dynamique");
                    
                    Generic.Command("_-BEDIT", Blkname);
                    PromptSelectionResult selRes = ed.SelectAll();
                    if (selRes.Status == PromptStatus.OK)
                    {
                        foreach (var item in selRes.GetObjectIds())
                        {
                            Generic.Command("_SCALE", item, Point3d.Origin, refScale);
                        }
                    }
                    Generic.Command("_BCLOSE", "_Save");

                }
                else
                {
                    Matrix3d scaleMatrix = Matrix3d.Scaling(refScale, Point3d.Origin);
                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = entId.GetDBObject(OpenMode.ForWrite) as Entity;
                        ent?.TransformBy(scaleMatrix);
                    }
                }

                // Fix all ref blk

                ObjectIdCollection refIds = BlockReferences.GetDynamicBlockReferences(Blkname);
                BlockTableRecord BlockDef = br.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
                refIds.Join(BlockDef.GetBlockReferenceIds(false, false));

                bool differentScalesFound = false;

                foreach (ObjectId id in refIds)
                {
                    if (!(id.GetDBObject(OpenMode.ForWrite) is BlockReference otherBr))
                    {
                        continue;
                    }

                    if (!IsUniformScaleAllowNegative(otherBr))
                    {
                        continue;
                    }

                    double oldScale = otherBr.ScaleFactors.X;
                    double newScale = oldScale / refScale;

                    if (Math.Abs(oldScale - refScale) > Generic.LowTolerance.EqualVector)
                    {
                        differentScalesFound = true;
                    }

                    double signX = Math.Sign(otherBr.ScaleFactors.X);
                    double signY = Math.Sign(otherBr.ScaleFactors.Y);
                    double signZ = Math.Sign(otherBr.ScaleFactors.Z);

                    double oldAbsScale = Math.Abs(otherBr.ScaleFactors.X);
                    double newAbsScale = oldAbsScale / refScale;

                    otherBr.ScaleFactors = new Scale3d(signX * newAbsScale, signY * newAbsScale, signZ * newAbsScale);
                }

                br.ScaleFactors = new Scale3d(Math.Sign(br.ScaleFactors.X), Math.Sign(br.ScaleFactors.Y), Math.Sign(br.ScaleFactors.Z));

                if (differentScalesFound)
                {
                    Generic.WriteMessage("⚠ Certaines références avaient une échelle différente. Les proportions ont été conservées.");
                }
                br.RegenAllBlkDefinition();
                br.RegenParentBlocksRecursive();
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
