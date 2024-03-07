using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKTOSTATICBLOCK
    {

        public static void ConvertExplode()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            if (!GetBlocks(out ObjectId[] ObjectIds))
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                foreach (ObjectId blockRefId in ObjectIds)
                {
                    if (!(blockRefId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRefToConvert))
                    {
                        return;
                    }

                    string UniqueName = BlockReferences.GetUniqueBlockName(blockRefToConvert.GetBlockReferenceName() + "_static");
                    Points BlockRefPosition = blockRefToConvert.Position.ToPoints();
                    Scale3d ScaleFactor = blockRefToConvert.ScaleFactors;
                    double Rotation = blockRefToConvert.Rotation;
                    blockRefToConvert.ScaleFactors = new Scale3d(1, 1, 1);
                    Dictionary<string, string> AttributesValues = blockRefToConvert.GetAttributesValues();
                    blockRefToConvert.Rotation = 0;

                    //https://adndevblog.typepad.com/autocad/2014/11/preserving-draworder-of-entities-while-wblockcloning-the-blocks.html
                    var sourceBTR = blockRefToConvert.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
                    var dotSource = sourceBTR.DrawOrderTableId.GetDBObject(OpenMode.ForRead) as DrawOrderTable;

                    ObjectIdCollection srcDotIds = new ObjectIdCollection();

                    srcDotIds = dotSource.GetFullDrawOrder(0);

                    for (int i = 0; i < srcDotIds.Count; i++)
                    {
                        if (srcDotIds[i].GetDBObject(OpenMode.ForWrite) is Entity ent)
                        {
                            ent.AddXData(i);
                        }
                    }

                    DBObjectCollection ExplodedEntities = new DBObjectCollection();
                    blockRefToConvert.Explode(ExplodedEntities);

                    var ExplodedEntitiesList = ExplodedEntities.ToList();
                    foreach (DBObject ExplodedEntity in ExplodedEntitiesList.ToArray())
                    {
                        if (ExplodedEntity is Entity ent)
                        {
                            if (!ent.Visible)
                            {
                                ExplodedEntitiesList.Remove(ent);
                                ent.Dispose();
                            }
                        }
                    }


                    //Order by drawTable

                    //var targetBTR = BlkDefObjectId.GetDBObject(OpenMode.ForRead) as BlockTableRecord;
                    //var dotTarget = targetBTR.DrawOrderTableId.GetDBObject(OpenMode.ForWrite) as DrawOrderTable;
                    List<(int Index, DBObject Value)> indexValuePairList = new List<(int, DBObject)>();

                    foreach (DBObject oId in ExplodedEntitiesList)
                    {
                        if (oId is Entity ent)
                        {
                            var XData = ent.ReadXData();
                            if (XData.Count > 0 && XData[0] is int Order)
                            {
                                indexValuePairList.Add((Order, ent));
                                continue;
                            }
                        }
                        indexValuePairList.Add((ExplodedEntitiesList.Count + indexValuePairList.Count, oId));
                    }

                    indexValuePairList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    var OrderedObj = indexValuePairList.Select(pair => pair.Value).ToDBObjectCollection();



                    var BlkDefObjectId = BlockReferences.Create(UniqueName, blockRefToConvert.GetDescription(), OrderedObj, BlockRefPosition);

                    ExplodedEntities.DeepDispose();
                    BlockTableRecord ms = Generic.GetCurrentSpaceBlockTableRecord(tr);
                    using (BlockReference blockRef = new BlockReference(BlockRefPosition.SCG, BlkDefObjectId))
                    {
                        blockRefToConvert.CopyPropertiesTo(blockRef);
                        blockRef.ScaleFactors = ScaleFactor;
                        blockRef.Rotation = Rotation;
                        ms.AppendEntity(blockRef);
                        tr.AddNewlyCreatedDBObject(blockRef, true);

                        foreach (ObjectId id in BlkDefObjectId.GetDBObject(OpenMode.ForWrite) as BlockTableRecord)
                        {
                            DBObject obj = id.GetObject(OpenMode.ForRead);
                            AttributeDefinition attDef = obj as AttributeDefinition;

                            if ((attDef != null) && (!attDef.Constant))
                            {
                                using (AttributeReference attRef = new AttributeReference())
                                {
                                    attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                    attRef.TextString = AttributesValues[attDef.Tag];
                                    blockRef.AttributeCollection.AppendAttribute(attRef);
                                    tr.AddNewlyCreatedDBObject(attRef, true);
                                    //
                                    //otherBlockRef.RecordGraphicsModified(true);
                                }
                            }
                        }

                    }


                    blockRefToConvert.EraseObject();
                }

                tr.Commit();

            }


            //if (blockRefNewObjectId != ObjectId.Null && AttributesValues.Count > 0)
            //{
            //    ed.CommandAsync("._ATTSYNC", "_SELECT", blockRefNewObjectId, "_YES");
            //    using (Transaction tr = db.TransactionManager.StartTransaction())
            //    {
            //        (blockRefNewObjectId.GetDBObject(OpenMode.ForWrite) as BlockReference).SetAttributeValues(AttributesValues);
            //    }
            //}
        }

        //public static void Convert()
        //{
        //    Database db = Generic.GetDatabase();
        //    if (!GetBlocks(out ObjectId[] ObjectIds))
        //    {
        //        return;
        //    }

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        var ed = Generic.GetEditor();
        //        foreach (ObjectId blockRefId in ObjectIds)
        //        {
        //            if (!(blockRefId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRef))
        //            {
        //                return;
        //            }
        //            blockRef.ConvertToStaticBlock(BlockReferences.GetUniqueBlockName(blockRef.GetBlockReferenceName() + "_static"));

        //        }
        //        tr.Commit();
        //    }
        //}

        public static bool GetBlocks(out ObjectId[] objectId)
        {
            objectId = new ObjectId[0];
            Editor editor = Generic.GetEditor();
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
                    return false;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count > 0)
                    {
                        objectId = promptResult.Value.GetObjectIds();
                        return true;
                    }
                }
            }
        }







        public static void Convert()
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

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ed = Generic.GetEditor();
                foreach (ObjectId blockRefId in promptResult.Value.GetObjectIds())
                {
                    if (!(tr.GetObject(blockRefId, OpenMode.ForWrite) is BlockReference blockRef))
                    {
                        return;
                    }
                    blockRef.ConvertToStaticBlock(BlockReferences.GetUniqueBlockName(blockRef.GetBlockReferenceName() + "_static"));
                    /*
                      Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                    if (ent != null && ent.Visible == false)
                    {
                        ent.Erase();
                        count++;
                    } 


                    (defun C:PurgeHiddenEntities ( / selset n count entnme entdat bit60) (if (setq selset (ssget "X")) (progn (setq n 0 count 0) (repeat (sslength selset) (setq entdat (entget (setq entnme (ssname selset n)))) (if (and (setq bit60 (assoc 60 entdat))(= 1 (cdr bit60))) (progn (entdel entnme) (setq count (1+ count)) ) ) (setq n (1+ n)) ) (princ (strcat "\n " (itoa count) " hidden entities deleted.")) ) ) (princ) )

                    */
                }
                tr.Commit();
            }
        }
    }
}
