using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKINSEDIT
    {
        public static void MoveBasePoint()
        {
            Editor editor = Generic.GetEditor();
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

            while (true)
            {
                promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
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


            Vector3d FixPosition;
            ObjectIdCollection iter;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ed = Generic.GetEditor();
                ObjectId blockRefId = promptResult.Value.GetObjectIds().First();
                blockRefId.RegisterHighlight();
                PromptPointOptions pointOptions = new PromptPointOptions("Veuillez sélectionner son nouveau point de base : ");
                PromptPointResult pointResult = editor.GetPoint(pointOptions);
                blockRefId.RegisterUnhighlight();
                if (pointResult.Status != PromptStatus.OK)
                {
                    return;
                }

                if (!(tr.GetObject(blockRefId, OpenMode.ForWrite) is BlockReference blockRef))
                {
                    return;
                }
                Point3d selectedPoint = pointResult.Value;
                FixPosition = selectedPoint - blockRef.Position;

                Point3d RotatedSelectedPoint = selectedPoint.TranformToBlockReferenceTransformation(blockRef);

                BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;

                if (blockRef.IsDynamicBlock)
                {
                    string BlockName = blockRef.GetBlockReferenceName();
                    iter = BlockReferences.GetDynamicBlockReferences(BlockName);
                    ed.Command("_-BEDIT", BlockName);
                    SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "BASEPOINTPARAMETERENTITY") });
                    PromptSelectionResult selRes = ed.SelectAll(filter);
                    if (selRes.Status == PromptStatus.OK)
                    {
                        var objId = selRes.Value.GetObjectIds();
                        foreach (ObjectId objectId in objId)
                        {
                            objectId.GetDBObject();
                            objectId.EraseObject();
                        }

                        Point3d OriginSelectedPoint = blockRef.Position.TranformToBlockReferenceTransformation(blockRef);
                        Lines.Draw(Points.Empty, new Points(OriginSelectedPoint));

                    }


                    tr.Commit();
                    ed.Command("_BPARAMETER", "Base", RotatedSelectedPoint * -1);
                    ed.Command("_POINT", RotatedSelectedPoint * -1);
                    ed.Command("_BCLOSE", "E");
                }
                else
                {
                    foreach (ObjectId entId in blockDef)
                    {
                        Entity entity = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                        entity?.TransformBy(Matrix3d.Displacement(RotatedSelectedPoint.GetAsVector()));
                    }
                    blockRef.DowngradeOpen();
                    iter = blockDef.GetBlockReferenceIds(true, false);
                    tr.Commit();
                }
            }

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
}
