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
                iter = ChangeBasePointDynamicBlock(blockRefId, BlockReferenceTransformedPoint, out Point3d OriginalBlocBasePointInModelSpace);
                Leaders.Draw("OriginalBlocBasePointInModelSpace", OriginalBlocBasePointInModelSpace, Point3d.Origin);
                FixPosition = selectedPoint - blockRef.Position;
                Leaders.Draw("selectedPoint", selectedPoint, Point3d.Origin);

                //Transform blockReferences to keep position
                using (Transaction tr2 = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId entId in iter)
                    {
                        if (entId.GetDBObject(OpenMode.ForWrite) is BlockReference otherBlockRef)
                        {
                            //Inverse the Vector (if selected on a transformed block and then for each transform the vector to the current block
                            Vector3d TransformedFixPositionV2 = FixPosition.TransformBy(blockRef.BlockTransform.Inverse()).TransformBy(otherBlockRef.BlockTransform);
                            Leaders.Draw("blockRef.Position", otherBlockRef.Position, Point3d.Origin); 
                            Leaders.Draw("TransformedFixPositionV2", otherBlockRef.Position.TransformBy(Matrix3d.Displacement(TransformedFixPositionV2)), Point3d.Origin); 
                            otherBlockRef.Position = otherBlockRef.Position.TransformBy(Matrix3d.Displacement(TransformedFixPositionV2));
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
                            otherBlockRef.Position  = otherBlockRef.Position.TransformBy(Matrix3d.Displacement(TransformedFixPosition));
                            otherBlockRef.RecordGraphicsModified(true);
                        }
                    }
                    tr2.Commit();
                }
            }



        }

        private static ObjectIdCollection ChangeBasePointDynamicBlock(ObjectId blockRefObjId, Point3d BlockReferenceTransformedPoint, out Point3d OriginalBlocBasePointInModelSpace)
        {
            OriginalBlocBasePointInModelSpace = new Point3d(0, 0, 0);
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            //Get the matrix between the fake point and original
            Vector3d FakeOriginalBasePointMatrix = GetFakeOriginalBasePointInDynamicBlockMatrix(blockRefObjId);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!(blockRefObjId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRef))
                {
                    return new ObjectIdCollection();
                }

                OriginalBlocBasePointInModelSpace = blockRef.Position.TransformBy(Matrix3d.Displacement(FakeOriginalBasePointMatrix));
                Point3d FakeBlocBasePointInBlocSpace = new Point3d(0, 0, 0).TransformBy(Matrix3d.Displacement(FakeOriginalBasePointMatrix * -1));

                string BlockName = blockRef.GetBlockReferenceName();
                //Get all GetDynamicBlockReferences to avoid delay after BEDIT
                ObjectIdCollection iter = BlockReferences.GetDynamicBlockReferences(BlockName);

                //Enter block reference edit mode
                ed.Command("_-BEDIT", BlockName);
                Leaders.Draw("FakeBlocBasePointInBlocSpace", FakeBlocBasePointInBlocSpace, Point3d.Origin);

                //Can only be a single BASEPOINTPARAMETERENTITY : we Erase the basepoint
                SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "BASEPOINTPARAMETERENTITY") });
                PromptSelectionResult selRes = ed.SelectAll(filter);
                if (selRes.Status == PromptStatus.OK)
                {
                    var objId = selRes.Value.GetObjectIds();
                    foreach (ObjectId objectId in objId)
                    {
                        objectId.EraseObject();
                    }
                }

                Leaders.Draw("SelectedBlockReferenceTransformedPoint", BlockReferenceTransformedPoint, Point3d.Origin);
                var ReelBlockReferenceTransformedPoint = BlockReferenceTransformedPoint.TransformBy(Matrix3d.Displacement(FakeBlocBasePointInBlocSpace - new Point3d(0, 0, 0)));
                Leaders.Draw("ReelBlockReferenceTransformedPoint", ReelBlockReferenceTransformedPoint, Point3d.Origin);
                //Commit the delete of the existing BASEPOINTPARAMETERENTITY
                tr.Commit();
                //Add the BASEPOINTPARAMETERENTITY at the new Position
                ed.Command("_BPARAMETER", "Base", ReelBlockReferenceTransformedPoint.Flatten());
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













        public static Vector3d GetFakeOriginalBasePointInDynamicBlockMatrix(ObjectId OriginalBlockObjectId)
        {
            var ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            Extents3d OriginalBounds;
            Extents3d EditedBounds;

            ObjectId insertedBtrId;
            ObjectId insertedCopyBtrId;

            string oldName;
            string newName;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference OriginalBlockRef = OriginalBlockObjectId.GetEntity() as BlockReference;
                oldName = OriginalBlockRef.GetBlockReferenceName();
                newName = BlockReferences.GetUniqueBlockName(oldName);
                insertedBtrId = BlockReferences.InsertFromName(oldName, new Points(new Point3d(0, 0, 0)), 0, null, null);
                BlockReference insertedBlockRef = insertedBtrId.GetEntity() as BlockReference;
                tr.Commit();
                OriginalBounds = insertedBlockRef.GeometricExtents;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                insertedCopyBtrId = BlockReferences.RenameBlockAndInsert(insertedBtrId, oldName, newName);
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
                EditedBounds = insertedCopyBtrId.GetEntity().GeometricExtents;
                //Cleannup
                insertedBtrId.EraseObject();
                insertedCopyBtrId.EraseObject();
                tr2.Commit();
            }
            var Matrix = OriginalBounds.TopLeft() - EditedBounds.TopLeft();
            return Matrix;
        }




        public static Vector3d GetFakeOriginalBasePointInDynamicBlockMatrixOld(ObjectId blockRefId)
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
            return Matrix;
        }
    }
}
