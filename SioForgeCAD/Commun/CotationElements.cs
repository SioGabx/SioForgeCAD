using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static class CotationElements
    {

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
                    Generic.ReadResource(BlocName, TempFolderFilePath);

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

        public static DBObjectCollection InitBlocForTransient(string BlocName, Dictionary<string, string> InitAttributesValues)
        {
            Database db = Generic.GetDatabase();
            DBObjectCollection ents = new DBObjectCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //The first block is added for initialising the process and then deleted. Be sure to add a value.
                ObjectId blockRef = CotationElements.InsertBlocFromBlocName(BlocName, Points.Empty, Generic.GetUSCRotation(Generic.AngleUnit.Radians), InitAttributesValues);
                DBObject dBObject = blockRef.GetDBObject();
                blockRef.EraseObject();
                ents.Add(dBObject);
                tr.Commit();
            }
            return ents;
        }


        public static ObjectId InsertBlocFromBlocName(string BlocName, Points BlocLocation, double Angle = 0, Dictionary<string, string> Values = null)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ImportBlocFromBlocNameIfMissing(BlocName);
                //insertion du bloc
                BlockTable bt2 = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                BlockTableRecord blockDef = bt2[BlocName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                //Also open modelspace - we'll be adding our BlockReference to it
                BlockTableRecord ms = bt2[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                //Create new BlockReference, and link it to our block definition
                using (BlockReference blockRef = new BlockReference(BlocLocation.SCG, blockDef.ObjectId))
                {
                    blockRef.ColorIndex = 256;
                    blockRef.Rotation = Angle;
                    ms.AppendEntity(blockRef);
                    tr.AddNewlyCreatedDBObject(blockRef, true);

                    if (Values != null)
                    {                        //Settings legacy block attributes
                        foreach (ObjectId id in blockDef)
                        {
                            DBObject obj = id.GetObject(OpenMode.ForRead);
                            AttributeDefinition attDef = obj as AttributeDefinition;

                            if ((attDef != null) && (!attDef.Constant))
                            {
                                string PropertyName = attDef.Tag.ToUpperInvariant();
                                if (Values.ContainsKey(PropertyName))
                                {
                                    if (Values.TryGetValue(PropertyName, out string AttributeDefinitionTargetValue))
                                    {
                                        using (AttributeReference attRef = new AttributeReference())
                                        {
                                            attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                            attRef.TextString = AttributeDefinitionTargetValue;
                                            //attRef.TextStyleId = Generic.AddFontStyle("Arial");
                                            //Add the AttributeReference to the BlockReference
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
                        foreach (string ValueKey in Values.Keys)
                        {
                            if (BlocPropertyCollectionDictionnary.TryGetValue(ValueKey, out DynamicBlockReferenceProperty BlocProperty))
                            {
                                object Value = ConvertValueToProperty((DwgDataType)BlocProperty.PropertyTypeCode, Values[ValueKey]);
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


        public enum DwgDataType : short
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

        public static object ConvertValueToProperty(DwgDataType dataType, string valueToConvert)
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


        //public static BlockTableRecord InsertBlocFromBlocName(string BlocName, Points Location, double Angle = 0, bool Values = true)
        //{
        //    //public void InsertBloc(string bloc_name, Point3d location, string attribut_text, double angle = 0, List<StringIntClass> stringIntClasses = null)
        //    //{
        //    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        ImportBlocFromBlocNameIfMissing(BlocName);
        //        //insertion du bloc
        //        BlockTable bt2 = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
        //        BlockTableRecord blockDef = bt2[BlocName].GetObject(OpenMode.ForRead) as BlockTableRecord;
        //        //Also open modelspace - we'll be adding our BlockReference to it
        //        BlockTableRecord ms = bt2[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
        //        //Create new BlockReference, and link it to our block definition
        //        using (BlockReference blockRef = new BlockReference(Location.SCU, blockDef.ObjectId))
        //        {
        //            blockRef.ColorIndex = 256;
        //            blockRef.Rotation = Angle;
        //            ms.AppendEntity(blockRef);
        //            tr.AddNewlyCreatedDBObject(blockRef, true);

        //            if (Values)
        //            {
        //                string AttributeName = "cote";
        //                string AttributeValue = "caca";
        //                string AttributeType = "string";
        //                //Generic block
        //                foreach (ObjectId id in blockDef)
        //                {
        //                    DBObject obj = id.GetObject(OpenMode.ForRead);
        //                    AttributeDefinition attDef = obj as AttributeDefinition;

        //                    if ((attDef != null) && (!attDef.Constant))
        //                    {
        //                        if (attDef.Tag.IgnoreCaseEquals(AttributeName))
        //                        {
        //                            using (AttributeReference attRef = new AttributeReference())
        //                            {
        //                                attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
        //                                attRef.TextString = AttributeValue;
        //                                attRef.TextStyleId = Generic.AddFontStyle("Arial");
        //                                //Add the AttributeReference to the BlockReference
        //                                blockRef.AttributeCollection.AppendAttribute(attRef);
        //                                tr.AddNewlyCreatedDBObject(attRef, true);
        //                            }
        //                        }
        //                    }
        //                }

        //                //Dynamic bloc
        //                var props = blockRef.DynamicBlockReferencePropertyCollection;
        //                foreach (var prop in props.OfType<DynamicBlockReferenceProperty>())
        //                {
        //                    if (prop.PropertyName.IgnoreCaseEquals(AttributeName))
        //                    {
        //                        try
        //                        {
        //                            switch (AttributeType)
        //                            {
        //                                case "double":
        //                                    prop.Value = Convert.ToDouble(AttributeValue);
        //                                    break;
        //                                case "string":
        //                                    prop.Value = AttributeValue.ToString();
        //                                    break;
        //                                case "int":
        //                                    prop.Value = (short)Convert.ToInt32(AttributeValue);
        //                                    break;
        //                            }
        //                        }
        //                        catch (System.Exception ex)
        //                        {
        //                            doc.Editor.WriteMessage("Erreur : " + ex.Message);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        tr.Commit();
        //        return blockDef;
        //    }


        //}































    }














































}
