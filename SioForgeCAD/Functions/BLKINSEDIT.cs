using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKINSEDIT
    {
        public static void MoveBasePoint()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            PromptSelectionResult promptResult;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                while (true)
                {
                    promptResult = ed.GetSelection(selectionOptions, new SelectionFilter(filterList));

                    if (promptResult.Status == PromptStatus.Cancel)
                    {
                        tr.Commit();
                        return;
                    }
                    else if (promptResult.Status == PromptStatus.OK)
                    {
                        if (promptResult.Value.Count > 0)
                        {
                            break;
                        }
                    }
                }
                tr.Commit();
            }

            ObjectIdCollection iter;
            BlockReference blockRef;
            BlockTableRecord blockDef;
            PromptPointResult pointResult;
            bool IsDynamicBlock = false;
            ObjectId blockRefId = promptResult.Value.GetObjectIds().First();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                blockRefId.RegisterHighlight();
                PromptPointOptions pointOptions = new PromptPointOptions("Veuillez sélectionner son nouveau point de base : ");
                pointResult = ed.GetPoint(pointOptions);
                blockRefId.RegisterUnhighlight();
                if (pointResult.Status != PromptStatus.OK)
                {
                    return;
                }

                if (!(tr.GetObject(blockRefId, OpenMode.ForWrite) is BlockReference blockRefOut))
                {
                    return;
                }
                else
                {
                    blockDef = blockRefOut.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
                    IsDynamicBlock = blockRefOut.IsDynamicBlock;
                    blockRef = blockRefOut;
                }
                tr.Commit();
            }

            Point3d selectedPoint = pointResult.Value;
            Vector3d FixPosition = selectedPoint - blockRef.Position;
            Point3d BlockReferenceTransformedPoint = selectedPoint.TranformToBlockReferenceTransformation(blockRef);

            if (IsDynamicBlock)
            {
                iter = ChangeBasePointDynamicBlock(blockRefId, BlockReferenceTransformedPoint, out Point3d ReelBlocBasePoint);
                Leaders.Draw("ReelBlocOriginModelSpace", ReelBlocBasePoint, Point3d.Origin);
                FixPosition = selectedPoint - blockRef.Position;
                Leaders.Draw("blockRef.Position", blockRef.Position, Point3d.Origin);
                Leaders.Draw("selectedPoint", selectedPoint, Point3d.Origin);
                Leaders.Draw("BlockReferenceTransformedPoint", BlockReferenceTransformedPoint, Point3d.Origin);

                //Transform blockReferences to keep position
                using (Transaction tr2 = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId entId in iter)
                    {
                        if (entId.GetDBObject(OpenMode.ForWrite) is BlockReference otherBlockRef)
                        {
                            Vector3d TransformedFixPosition = FixPosition.TransformBy(otherBlockRef.BlockTransform);
                            otherBlockRef.TransformBy(Matrix3d.Displacement(TransformedFixPosition));
                            otherBlockRef.RecordGraphicsModified(true);
                        }
                    }
                    tr2.Commit();
                }


            }
            else
            {
                Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                iter = ChangeBasePointStaticBlock(blockRefId, BlockReferenceTransformedPoint.TransformBy(rotationMatrix));
                //Transform blockReferences to keep position
                using (Transaction tr2 = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId entId in iter)
                    {
                        if (entId.GetDBObject(OpenMode.ForWrite) is BlockReference otherBlockRef)
                        {
                            Vector3d TransformedFixPosition = FixPosition.TransformBy(otherBlockRef.BlockTransform);
                            otherBlockRef.TransformBy(Matrix3d.Displacement(TransformedFixPosition));
                            otherBlockRef.RecordGraphicsModified(true);
                        }
                    }
                    tr2.Commit();
                }
            }


          
        }

        private static ObjectIdCollection ChangeBasePointDynamicBlock(ObjectId blockRefObjId, Point3d BlockReferenceTransformedPoint, out Point3d ReelBlocBasePointOrigin)
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            ReelBlocBasePointOrigin = Functions.BLKINSEDIT.GetOriginalBasePointInDynamicBlockWithBasePoint(blockRefObjId);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!(blockRefObjId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRef))
                {
                    return new ObjectIdCollection();
                }
                string BlockName = blockRef.GetBlockReferenceName();
                ObjectIdCollection iter = BlockReferences.GetDynamicBlockReferences(BlockName);
                ed.Command("_-BEDIT", BlockName);

               // ed.Command("_CIRCLE", BlockReferenceTransformedPoint, .05);
                SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "BASEPOINTPARAMETERENTITY") });
                PromptSelectionResult selRes = ed.SelectAll(filter);
                Point3d ReelBlocOriginInBlocSpace = new Point3d(0, 0, 0);
                if (selRes.Status == PromptStatus.OK)
                {
                    var objId = selRes.Value.GetObjectIds();
                    foreach (ObjectId objectId in objId)
                    {
                        objectId.EraseObject();
                    }

                    ReelBlocOriginInBlocSpace = ReelBlocBasePointOrigin.TranformToBlockReferenceTransformation(blockRef);
                    //ed.Command("_CIRCLE", ReelBlocOriginInBlocSpace, .1);
                }
                
                var ReelBlockReferenceTransformedPoint = BlockReferenceTransformedPoint.TransformBy(Matrix3d.Displacement(ReelBlocOriginInBlocSpace.GetAsVector().MultiplyBy(-1)));
                //ed.Command("_CIRCLE", ReelBlockReferenceTransformedPoint, .02);

                tr.Commit();
                ed.Command("_BPARAMETER", "Base", ReelBlockReferenceTransformedPoint);
                //ed.Command("_POINT", BlockReferenceTransformedPointV2 * -1);
                ed.Command("_BCLOSE", "E");
                return iter;
            }
        }

        private static ObjectIdCollection ChangeBasePointStaticBlock(ObjectId blockRefObjId, Point3d BlockReferenceTransformedPoint)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!(blockRefObjId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRef))
                {
                    return new ObjectIdCollection();
                }
                var blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;


                foreach (ObjectId entId in blockDef)
                {
                    Entity entity = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                    entity?.TransformBy(Matrix3d.Displacement(BlockReferenceTransformedPoint.GetAsVector()));
                }
                blockRef.DowngradeOpen();

                tr.Commit();
                return blockDef.GetBlockReferenceIds(true, false);
            }
        }


        public static Point3d GetOriginalBasePointInDynamicBlockWithBasePoint(ObjectId blockRefId)
        {
            var ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            Extents3d OriginalBounds;
            Extents3d EditedBounds;

            ObjectId newBtrId;
            BlockReference blockRef;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                blockRef = blockRefId.GetEntity() as BlockReference;

                OriginalBounds = blockRef.GeometricExtents;


                string oldName = blockRef.GetBlockReferenceName();
                string newName = BlockReferences.GetUniqueBlockName(oldName);
                newBtrId = BlockReferences.RenameBlockAndInsert(blockRef.ObjectId, oldName, newName);

                ed.Command("_-BEDIT", newName);
                SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "BASEPOINTPARAMETERENTITY") });
                PromptSelectionResult selRes = ed.SelectAll(filter);
                if (selRes.Status == PromptStatus.OK)
                {
                    var objId = selRes.Value.GetObjectIds();
                    foreach (ObjectId objectId in objId)
                    {
                        objectId.GetDBObject();
                        objectId.EraseObject();
                        Debug.WriteLine("Erase BASEPOINTPARAMETERENTITY");
                    }
                }

                tr.Commit();

            }
            using (Transaction tr2 = db.TransactionManager.StartTransaction())
            {
                ed.Command("_BCLOSE", "E");
                EditedBounds = newBtrId.GetEntity().GeometricExtents;
                newBtrId.EraseObject();
                tr2.Commit();
            }
            var Matrix = OriginalBounds.TopLeft() - EditedBounds.TopLeft();
            return blockRef.Position.TransformBy(Matrix3d.Displacement(Matrix));
        }
    }
}
