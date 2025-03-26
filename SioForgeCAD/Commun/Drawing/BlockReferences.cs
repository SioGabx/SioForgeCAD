using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun.Drawing
{
    public static class BlockReferences
    {
        public static ObjectId Create(string Name, string Description, DBObjectCollection EntitiesDbObjectCollection, Points Origin)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetDBObject(OpenMode.ForWrite) as BlockTable;
                string BlockName = SymbolUtilityServices.RepairSymbolName(Name, false);

                if (bt.Has(BlockName))
                {
                    Generic.WriteMessage($"Le bloc {Name} existe déja dans le dessin");
                }

                BlockTableRecord btr = new BlockTableRecord
                {
                    Name = BlockName,
                    Comments = Description,
                };
                if (Origin != Points.Null)
                {
                    btr.Origin = Origin.SCG;
                }
                // Add the new block to the block table
                ObjectId btrId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                foreach (Entity ent in EntitiesDbObjectCollection)
                {
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }

                tr.Commit();
                return btrId;
            }
        }

        public static ObjectId RenameBlockAndInsert(ObjectId BlockReferenceObjectId, string OldName, string NewName)
        {
            if (!BlockReferenceObjectId.IsValid)
            {
                return ObjectId.Null;
            }
            ObjectIdCollection acObjIdColl = new ObjectIdCollection { BlockReferenceObjectId };

            Document ActualDocument = Generic.GetDocument();
            Database ActualDatabase = ActualDocument.Database;

            Database MemoryDatabase = new Database(true, false);
            IdMapping acIdMap = new IdMapping();
            using (Transaction MemoryTransaction = MemoryDatabase.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTblNewDoc = MemoryTransaction.GetObject(MemoryDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRecNewDoc = MemoryTransaction.GetObject(acBlkTblNewDoc[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                try
                {
                    MemoryDatabase.WblockCloneObjects(acObjIdColl, acBlkTblRecNewDoc.ObjectId, acIdMap, DuplicateRecordCloning.Replace, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return ObjectId.Null;
                }
                BlockTableRecord btr = (BlockTableRecord)MemoryTransaction.GetObject(acBlkTblNewDoc[OldName], OpenMode.ForWrite);
                btr.Name = NewName;
                MemoryTransaction.Commit();
            }

            ObjectId newBlocRefenceId = acIdMap[BlockReferenceObjectId].Value;
            if (!newBlocRefenceId.IsValid)
            {
                return ObjectId.Null;
            }
            ObjectIdCollection acObjIdColl2 = new ObjectIdCollection { newBlocRefenceId };
            IdMapping acIdMap2 = new IdMapping();

            using (Generic.GetLock())
            using (Transaction ActualTransaction = ActualDatabase.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTblNewDoc2 = ActualTransaction.GetObject(ActualDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRecNewDoc2 = Generic.GetCurrentSpaceBlockTableRecord(ActualTransaction);

                ActualDatabase.WblockCloneObjects(acObjIdColl2, acBlkTblRecNewDoc2.ObjectId, acIdMap2, DuplicateRecordCloning.Replace, false);
                ActualTransaction.Commit();
            }

            return acIdMap2[newBlocRefenceId].Value;
        }

        public static string GetUniqueBlockName(string oldName)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                string newName = oldName;
                for (int index = 1; bt.Has(newName); index++)
                {
                    newName = $"{oldName}_Copy{(index > 1 ? $" ({index})" : "")}";
                }
                return SymbolUtilityServices.RepairSymbolName(newName, false);
            }
        }

        public static bool IsBlockExist(string BlocName)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                return bt.Has(BlocName);
            }
        }
        public static ObjectIdCollection GetDynamicBlockReferences(string BlockName)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                    if (btr.Name != BlockName)
                    {
                        continue;
                    }
                    if (btr.IsDynamicBlock)
                    {
                        //get all anonymous blocks from this dynamic block
                        ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();
                        ObjectIdCollection dynBlockRefs = new ObjectIdCollection();
                        foreach (ObjectId anonymousBtrId in anonymousIds)
                        {
                            BlockTableRecord anonymousBtr = (BlockTableRecord)trans.GetObject(anonymousBtrId, OpenMode.ForRead);
                            ObjectIdCollection blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);
                            dynBlockRefs.Join(blockRefIds);
                        }

                        Debug.WriteLine(String.Format("Dynamic block \"{0}\" found with {1} anonymous block and {2} block references\n", btr.Name, anonymousIds.Count, dynBlockRefs.Count));

                        //anonymousIds is block where attributes ares edited
                        //dynBlockRefs contain all ref
                        return dynBlockRefs;
                    }
                }
            }
            return new ObjectIdCollection();
        }

        public static BlockReference GetBlockReference(string BlocName, Point3d PositionSCG)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                if (!bt.Has(BlocName))
                {
                    throw new Exception($"Le bloc {BlocName} n'existe pas dans le dessin");
                }
                BlockTableRecord blockDef = bt[BlocName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                tr.Commit();
                return new BlockReference(PositionSCG, blockDef.ObjectId);
            }
        }

        public static ObjectId InsertFromName(string BlocName, Points BlocLocation, double Angle = 0, Dictionary<string, string> AttributesValues = null, string Layer = null)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                BlockTableRecord blockDef = bt[BlocName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                BlockTableRecord ms = Generic.GetCurrentSpaceBlockTableRecord(tr);
                //Create new BlockReference, and link it to our block definition
                using (BlockReference blockRef = GetBlockReference(BlocName, BlocLocation.SCG))
                {
                    blockRef.Color = db.Cecolor;
                    blockRef.Rotation = Angle;

                    if (!string.IsNullOrEmpty(Layer) && Layers.CheckIfLayerExist(Layer))
                    {
                        //Debug.WriteLine($"Layer {Layer} exist : {Layers.CheckIfLayerExist(Layer)}");
                        blockRef.Layer = Layer;
                    }
                    ms.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);

                    if (AttributesValues != null)
                    {
                        //Settings legacy block attributes
                        foreach (ObjectId id in blockDef)
                        {
                            DBObject obj = id.GetObject(OpenMode.ForRead);
                            AttributeDefinition attDef = obj as AttributeDefinition;
                            if (attDef?.Constant == false)
                            {
                                string PropertyName = attDef.Tag.ToUpperInvariant();
                                if (AttributesValues.ContainsKey(PropertyName))
                                {
                                    if (AttributesValues.TryGetValue(PropertyName, out string AttributeDefinitionTargetValue))
                                    {
                                        using (AttributeReference attRef = new AttributeReference())
                                        {
                                            attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                            attRef.TextString = AttributeDefinitionTargetValue;
                                            blockRef.AttributeCollection.AppendAttribute(attRef);
                                            tr.AddNewlyCreatedDBObject(attRef, true);
                                        }
                                    }
                                }
                            }
                        }

                        //Settings Dynamic block attributes
                        var BlocPropertyCollection = blockRef.DynamicBlockReferencePropertyCollection;
                        var BlocPropertyCollectionDictionnary = new Dictionary<string, DynamicBlockReferenceProperty>();
                        foreach (DynamicBlockReferenceProperty BlocProperty in BlocPropertyCollection.OfType<DynamicBlockReferenceProperty>())
                        {
                            string PropertyName = BlocProperty.PropertyName.ToUpperInvariant();
                            if (BlocPropertyCollectionDictionnary.ContainsKey(PropertyName))
                            {
                                continue;
                            }
                            BlocPropertyCollectionDictionnary.Add(PropertyName, BlocProperty);
                        }
                        foreach (string ValueKey in AttributesValues.Keys)
                        {
                            if (BlocPropertyCollectionDictionnary.TryGetValue(ValueKey, out DynamicBlockReferenceProperty BlocProperty))
                            {
                                object Value = ConvertValueToProperty((DwgDataType)BlocProperty.PropertyTypeCode, AttributesValues[ValueKey]);
                                if (Value is int ValueAsInt) BlocProperty.Value = (short)ValueAsInt;
                                if (Value is double ValueAsDbl) BlocProperty.Value = ValueAsDbl;
                                if (Value is string ValueAsStr) BlocProperty.Value = ValueAsStr;
                            }
                        }
                    }
                    tr.Commit();
                    return blockRef.ObjectId;
                }
            }
        }

        public static ObjectId InsertFromNameImportIfNotExist(string BlocName, Points BlocLocation, double Angle = 0, Dictionary<string, string> AttributesValues = null, string Layer = null)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ImportBlocFromBlocNameIfMissing(BlocName);
                ObjectId blockRefObjectId = InsertFromName(BlocName, BlocLocation, Angle, AttributesValues, Layer);
                tr.Commit();
                return blockRefObjectId;
            }
        }

        public static void Purge(string BlocName)
        {
            //From https://adndevblog.typepad.com/autocad/2013/01/purging-anonymous-blocks-using-vba.html
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                foreach (ObjectId oid in bt)
                {
                    BlockTableRecord btr = trans.GetObject(oid, OpenMode.ForWrite) as BlockTableRecord;
                    if (btr.Name.Equals(BlocName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (btr.GetBlockReferenceIds(false, false).Count == 0 && !btr.IsLayout)
                        {
                            btr.Erase();
                        }
                    }
                }
                trans.Commit();
            }
        }

        public static DBObjectCollection InitForTransient(string BlocName, Dictionary<string, string> InitAttributesValues, string Layer = null)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            DBObjectCollection ents = new DBObjectCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //The first block is added for initialising the process and then deleted. Be sure to add a value.
                ObjectId blockRef = InsertFromNameImportIfNotExist(BlocName, Points.Empty, ed.GetUSCRotation(AngleUnit.Radians), InitAttributesValues);
                DBObject dBObject = blockRef.GetDBObject();
                if (Layer != null && Layers.CheckIfLayerExist(Layer))
                {
                    (dBObject as Entity).Layer = Layer;
                }
                blockRef.EraseObject();
                ents.Add(dBObject);
                tr.Commit();
            }
            return ents;
        }

        public static void ImportBlocFromBlocNameIfMissing(string BlocName)
        {
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                if (!bt.Has(BlocName))
                {
                    string TempFolderPath = System.IO.Path.GetTempPath();
                    string TempFolderFilePath = System.IO.Path.Combine(TempFolderPath, $"{BlocName}.dwg");
                    Generic.ReadWriteToFileResource(BlocName, TempFolderFilePath);

                    Database sourceDb = new Database(false, true); //Temporary database to hold data for block we want to import
                    try
                    {
                        sourceDb.ReadDwgFile(TempFolderFilePath, System.IO.FileShare.Read, true, ""); //Read the DWG into a side database
                        db.Insert(TempFolderFilePath, sourceDb, false);
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Generic.WriteMessage("\nErreur : " + ex.Message);
                    }
                    finally
                    {
                        sourceDb.Dispose();
                    }
                }
                tr.Commit();
            }
        }

        private enum DwgDataType : short
        {
            Null = 0,
            Real = 1,
            Int32 = 2,
            Int16 = 3,
            Int8 = 4,
            Text = 5,
            BChunk = 6,
            Handle = 7,
            HardOwnershipId = 8,
            SoftOwnershipId = 9,
            HardPointerId = 10,
            SoftPointerId = 11,
            Dwg3Real = 12,
            Int64 = 13,
            NotRecognized = 19
        }

        private static object ConvertValueToProperty(DwgDataType dataType, string valueToConvert)
        {
            switch (dataType)
            {
                case DwgDataType.Real:
                    if (double.TryParse(valueToConvert, out double convertedValueDouble))
                    {
                        return convertedValueDouble;
                    }
                    break;
                case DwgDataType.Int16:
                case DwgDataType.Int32:
                    if (int.TryParse(valueToConvert, out int convertedValueInt))
                    {
                        return convertedValueInt;
                    }
                    break;
                case DwgDataType.Text:
                    return valueToConvert;
            }
            return null;
        }
    }
}
