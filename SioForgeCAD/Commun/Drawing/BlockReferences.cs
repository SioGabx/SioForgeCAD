using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using System.Linq;


namespace SioForgeCAD.Commun.Drawing
{
    public static class BlockReferences
    {
        public static ObjectId Create(string Name, string Description, DBObjectCollection EntitiesDbObjectCollection)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetDBObject(OpenMode.ForWrite) as BlockTable;
                string BlockName = SymbolUtilityServices.RepairSymbolName(Name, false); ;

                if (bt.Has(BlockName))
                {
                    throw new System.Exception($"Le bloc {Name} existe déja dans le dessin");
                }

                BlockTableRecord btr = new BlockTableRecord
                {
                    Name = BlockName,
                    Comments = Description
                };
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


        public static ObjectId InsertFromName(string BlocName, Points BlocLocation, double Angle = 0, Dictionary<string, string> AttributesValues = null)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                if (!bt.Has(BlocName))
                {
                    throw new System.Exception($"Le bloc {BlocName} n'existe pas dans le dessin");
                }
                BlockTableRecord blockDef = bt[BlocName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                //Also open modelspace - we'll be adding our BlockReference to it
                BlockTableRecord ms = bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                //Create new BlockReference, and link it to our block definition
                using(BlockReference blockRef = new BlockReference(BlocLocation.SCG, blockDef.ObjectId))
                {
                    blockRef.ColorIndex = 256;
                    blockRef.Rotation = Angle;
                    ms.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);

                    if (AttributesValues != null)
                    {                        
                        //Settings legacy block attributes
                        foreach (ObjectId id in blockDef)
                        {
                            DBObject obj = id.GetObject(OpenMode.ForRead);
                            AttributeDefinition attDef = obj as AttributeDefinition;

                            if ((attDef != null) && (!attDef.Constant))
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

        public static ObjectId InsertFromNameImportIfNotExist(string BlocName, Points BlocLocation, double Angle = 0, Dictionary<string, string> AttributesValues = null)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ImportBlocFromBlocNameIfMissing(BlocName);
                ObjectId blockRefObjectId = InsertFromName(BlocName, BlocLocation, Angle, AttributesValues);
                tr.Commit();
                return blockRefObjectId;
            }
        }

        public static DBObjectCollection InitForTransient(string BlocName, Dictionary<string, string> InitAttributesValues)
        {
            Database db = Generic.GetDatabase();
            DBObjectCollection ents = new DBObjectCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //The first block is added for initialising the process and then deleted. Be sure to add a value.
                ObjectId blockRef = InsertFromNameImportIfNotExist(BlocName, Points.Empty, Generic.GetUSCRotation(Generic.AngleUnit.Radians), InitAttributesValues);
                DBObject dBObject = blockRef.GetDBObject();
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
