using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SioForgeCAD.Functions
{
    public static class PURGEALL
    {
        public static void Purge()
        {
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //unlock all layers but keep trace
                List<LayerTableRecord> list = new List<LayerTableRecord>();
                foreach (ObjectId objectId in ((LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead)))
                {
                    LayerTableRecord layerTableRecord = (LayerTableRecord)tr.GetObject(objectId, OpenMode.ForRead);
                    if (layerTableRecord.IsLocked)
                    {
                        tr.GetObject(layerTableRecord.ObjectId, OpenMode.ForWrite);
                        layerTableRecord.IsLocked = false;
                        list.Add(layerTableRecord);
                    }
                }
                int TotalDeletedCount = 0;
                PurgeDatabase(db);
                PurgeCurvesZeroLength(db);
                PurgeEmptyText(db);
                PurgeXREF(db);

                bool NeedPurgeNewPass = true;
                while (NeedPurgeNewPass)
                {
                    int PassDeletedCount = 0;
                    PassDeletedCount += PurgeMLeaderStyle(db);
                    PassDeletedCount += PurgeDWF(db);
                    PassDeletedCount += PurgePDF(db);
                    PassDeletedCount += PurgeDGN(db);
                    PassDeletedCount += PurgeRasterImages(db);
                    PassDeletedCount += PurgeScaleList(db);
                    PassDeletedCount += PurgeVisualStyle(db);
                    PassDeletedCount += PurgeMaterial(db);
                    PassDeletedCount += PurgeTextStyle(db);
                    PassDeletedCount += PurgeGroups(db);

                    TotalDeletedCount += PassDeletedCount;
                    NeedPurgeNewPass = PassDeletedCount > 0;
                }

                //relock all layers
                foreach (LayerTableRecord layerTableRecord2 in list)
                {
                    layerTableRecord2.IsLocked = true;
                }

                tr.Commit();
            }

            VIEWPORTLOCK.DoLockUnlock(true);
        }

        public static int PurgeDatabase(Database db)
        {
            ObjectIdCollection tableIds = new ObjectIdCollection();
            ObjectIdCollection dictIds = new ObjectIdCollection();

            tableIds.Add(db.BlockTableId);
            tableIds.Add(db.LayerTableId);
            tableIds.Add(db.DimStyleTableId);
            tableIds.Add(db.TextStyleTableId);
            tableIds.Add(db.LinetypeTableId);
            tableIds.Add(db.RegAppTableId);

            dictIds.Add(db.MLStyleDictionaryId);
            dictIds.Add(db.TableStyleDictionaryId);
            dictIds.Add(db.PlotStyleNameDictionaryId);

            ObjectIdCollection objectIdCollection = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (object obj in tableIds)
                {
                    ObjectId objectId = (ObjectId)obj;
                    foreach (ObjectId objectId2 in ((SymbolTable)objectId.GetDBObject()))
                    {
                        if (!((SymbolTableRecord)objectId2.GetDBObject()).IsDependent)
                        {
                            objectIdCollection.Add(objectId2);
                        }
                    }
                }
                foreach (object obj2 in dictIds)
                {
                    ObjectId objectId3 = (ObjectId)obj2;
                    foreach (DBDictionaryEntry dbdictionaryEntry in ((DBDictionary)objectId3.GetDBObject()))
                    {
                        if (dbdictionaryEntry.Value.IsValid && !dbdictionaryEntry.Value.GetDBObject().IsAProxy)
                        {
                            objectIdCollection.Add(dbdictionaryEntry.Value);
                        }
                    }
                }

                db.Purge(objectIdCollection);
                foreach (ObjectId objid in objectIdCollection)
                {
                    objid.GetDBObject(OpenMode.ForWrite).Erase();
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeCurvesZeroLength(Database db)
        {
            RXClass CurveRXClass = RXObject.GetClass(typeof(Curve));
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int NumberZeroLengthCurvesDeleted = 0;
                foreach (ObjectId objectId2 in ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)))
                {
                    if (!objectId2.IsErased)
                    {
                        foreach (ObjectId objectId3 in (BlockTableRecord)tr.GetObject(objectId2, OpenMode.ForRead))
                        {
                            if (objectId3.ObjectClass.IsDerivedFrom(CurveRXClass))
                            {
                                Curve curve = (Curve)tr.GetObject(objectId3, OpenMode.ForRead);
                                if (!(curve is Xline) && !(curve is Ray) && curve.GetDistanceAtParameter(curve.EndParam) == 0.0)
                                {
                                    objectId3.GetDBObject(OpenMode.ForWrite).Erase();
                                    NumberZeroLengthCurvesDeleted++;
                                }
                            }
                            else if (objectId3.ObjectClass.Name == "AcDbRegion" && ((Region)tr.GetObject(objectId3, OpenMode.ForRead)).Area == 0.0)
                            {
                                objectId3.GetDBObject(OpenMode.ForWrite).Erase();
                                NumberZeroLengthCurvesDeleted++;
                            }
                        }
                    }
                }
                tr.Commit();
                return NumberZeroLengthCurvesDeleted;
            }
        }

        public static int PurgeEmptyText(Database db)
        {
            RXClass DBTextRXClass = RXObject.GetClass(typeof(DBText));
            RXClass MTextRXClass = RXObject.GetClass(typeof(MText));
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int NumberEmptyTextDeleted = 0;
                foreach (ObjectId objectId2 in ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)))
                {
                    if (!objectId2.IsErased)
                    {
                        foreach (ObjectId objectId3 in (BlockTableRecord)tr.GetObject(objectId2, OpenMode.ForRead))
                        {
                            if (objectId3.ObjectClass == DBTextRXClass)
                            {
                                DBText dbtext = (DBText)tr.GetObject(objectId3, OpenMode.ForRead);
                                if (dbtext.TextString.Trim()?.Length == 0)
                                {
                                    objectId3.GetDBObject(OpenMode.ForWrite).Erase();
                                    dbtext.Erase();
                                    NumberEmptyTextDeleted++;
                                }
                            }
                            else if (objectId3.ObjectClass == MTextRXClass && ((MText)tr.GetObject(objectId3, OpenMode.ForRead)).Text.Trim()?.Length == 0)
                            {
                                objectId3.GetDBObject(OpenMode.ForWrite).Erase();
                                NumberEmptyTextDeleted++;
                            }
                        }
                    }
                }
                tr.Commit();
                return NumberEmptyTextDeleted;
            }
        }

        public static int PurgeXREF(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                int NumberDetachedXREF = 0;
                foreach (ObjectId XrefId in ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)))
                {
                    if (!XrefId.IsErased)
                    {
                        BlockTableRecord blockTableRecord = (BlockTableRecord)tr.GetObject(XrefId, OpenMode.ForRead);
                        if (!blockTableRecord.IsLayout && blockTableRecord.IsFromExternalReference && blockTableRecord.GetBlockReferenceIds(true, false).Count == 0)
                        {
                            db.DetachXref(blockTableRecord.ObjectId);
                            NumberDetachedXREF++;
                        }
                    }
                }
                tr.Commit();
                return NumberDetachedXREF;
            }
        }

        public static int PurgeMLeaderStyle(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                DBDictionary dbdictionary = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (dbdictionary.Contains("ACAD_MLEADERSTYLE"))
                {
                    ObjectIdCollection MLeaderStyleObjectIdCollection = new ObjectIdCollection();
                    foreach (DBDictionaryEntry MLeaderStyleEntry in ((DBDictionary)dbdictionary.GetAt("ACAD_MLEADERSTYLE").GetDBObject()))
                    {
                        MLeaderStyleObjectIdCollection.Add(MLeaderStyleEntry.Value);
                    }
                    int count = MLeaderStyleObjectIdCollection.Count;
                    int[] array = new int[count];
                    db.CountHardReferences(MLeaderStyleObjectIdCollection, array);
                    for (int i = 0; i < count; i++)
                    {
                        if (array[i] == 0)
                        {
                            objectIdCollection.Add(MLeaderStyleObjectIdCollection[i]);
                        }
                    }

                    db.Purge(objectIdCollection);
                    foreach (ObjectId objid in objectIdCollection)
                    {
                        objid.GetDBObject(OpenMode.ForWrite).Erase();
                    }
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeDWF(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                DBDictionary dbdictionary = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (dbdictionary.Contains("ACAD_DWFDEFINITIONS"))
                {
                    ObjectIdCollection DwfDefinitionsObjectIdCollection = new ObjectIdCollection();
                    foreach (DBDictionaryEntry DwfEntry in ((DBDictionary)dbdictionary.GetAt("ACAD_DWFDEFINITIONS").GetDBObject()))
                    {
                        DwfDefinitionsObjectIdCollection.Add(DwfEntry.Value);
                    }
                    int count = DwfDefinitionsObjectIdCollection.Count;
                    int[] array = new int[count];
                    db.CountHardReferences(DwfDefinitionsObjectIdCollection, array);
                    for (int i = 0; i < count; i++)
                    {
                        if (array[i] == 0)
                        {
                            objectIdCollection.Add(DwfDefinitionsObjectIdCollection[i]);
                        }
                    }

                    db.Purge(objectIdCollection);
                    foreach (ObjectId objid in objectIdCollection)
                    {
                        objid.GetDBObject(OpenMode.ForWrite).Erase();
                    }
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgePDF(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                DBDictionary dbdictionary = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (dbdictionary.Contains("ACAD_PDFDEFINITIONS"))
                {
                    ObjectIdCollection PdfDefinitionsObjectIdCollection = new ObjectIdCollection();
                    foreach (DBDictionaryEntry PdfEntry in ((DBDictionary)dbdictionary.GetAt("ACAD_PDFDEFINITIONS").GetDBObject()))
                    {
                        PdfDefinitionsObjectIdCollection.Add(PdfEntry.Value);
                    }
                    int count = PdfDefinitionsObjectIdCollection.Count;
                    int[] array = new int[count];
                    db.CountHardReferences(PdfDefinitionsObjectIdCollection, array);
                    for (int i = 0; i < count; i++)
                    {
                        if (array[i] == 0)
                        {
                            objectIdCollection.Add(PdfDefinitionsObjectIdCollection[i]);
                        }
                    }

                    db.Purge(objectIdCollection);
                    foreach (ObjectId objid in objectIdCollection)
                    {
                        objid.GetDBObject(OpenMode.ForWrite).Erase();
                    }
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeDGN(Database db)
        {
            ObjectIdCollection objectIdCollection = new ObjectIdCollection();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary dbdictionary = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (dbdictionary.Contains("ACAD_DGNDEFINITIONS"))
                {
                    ObjectIdCollection DgnDefinitionsObjectIdCollection = new ObjectIdCollection();
                    foreach (DBDictionaryEntry DgnEntry in ((DBDictionary)dbdictionary.GetAt("ACAD_DGNDEFINITIONS").GetDBObject()))
                    {
                        DgnDefinitionsObjectIdCollection.Add(DgnEntry.Value);
                    }
                    int count = DgnDefinitionsObjectIdCollection.Count;
                    int[] array = new int[count];
                    db.CountHardReferences(DgnDefinitionsObjectIdCollection, array);
                    for (int i = 0; i < count; i++)
                    {
                        if (array[i] == 0)
                        {
                            objectIdCollection.Add(DgnDefinitionsObjectIdCollection[i]);
                        }
                    }

                    db.Purge(objectIdCollection);
                    foreach (ObjectId objid in objectIdCollection)
                    {
                        objid.GetDBObject(OpenMode.ForWrite).Erase();
                    }
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeRasterImages(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                DBDictionary dbdictionary = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (dbdictionary.Contains("ACAD_IMAGE_DICT"))
                {
                    foreach (DBDictionaryEntry ImageEntry in ((DBDictionary)dbdictionary.GetAt("ACAD_IMAGE_DICT").GetDBObject()))
                    {
                        if (ImageEntry.Value.IsValid)
                        {
                            RasterImageDef rasterImageDef = tr.GetObject(ImageEntry.Value, OpenMode.ForRead) as RasterImageDef;
                            if (rasterImageDef?.IsAProxy == false && rasterImageDef.GetEntityCount(out bool _) == 0)
                            {
                                objectIdCollection.Add(ImageEntry.Value);
                            }
                        }
                    }
                    db.Purge(objectIdCollection);
                    foreach (ObjectId objid in objectIdCollection)
                    {
                        objid.GetDBObject(OpenMode.ForWrite).Erase();
                    }
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeScaleList(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                DBDictionary dbdictionary = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (dbdictionary.Contains("ACAD_SCALELIST"))
                {
                    foreach (DBDictionaryEntry ScaleEntry in ((DBDictionary)dbdictionary.GetAt("ACAD_SCALELIST").GetDBObject()))
                    {
                        if (ScaleEntry.Key != "A0" && !tr.GetObject(ScaleEntry.Value, OpenMode.ForRead).IsAProxy)
                        {
                            objectIdCollection.Add(ScaleEntry.Value);
                        }
                    }
                    db.Purge(objectIdCollection);
                    foreach (ObjectId objid in objectIdCollection)
                    {
                        objid.GetDBObject(OpenMode.ForWrite).Erase();
                    }
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeVisualStyle(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                foreach (DBDictionaryEntry VisualStyleEntry in ((DBDictionary)tr.GetObject(db.VisualStyleDictionaryId, OpenMode.ForRead)))
                {
                    if ((tr.GetObject(VisualStyleEntry.Value, OpenMode.ForRead) as DBVisualStyle).Type == VisualStyleType.Custom && !tr.GetObject(VisualStyleEntry.Value, OpenMode.ForRead).IsAProxy)
                    {
                        objectIdCollection.Add(VisualStyleEntry.Value);
                    }
                }
                db.Purge(objectIdCollection);
                foreach (ObjectId objid in objectIdCollection)
                {
                    objid.GetDBObject(OpenMode.ForWrite).Erase();
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeMaterial(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                foreach (DBDictionaryEntry MaterialEntry in ((DBDictionary)tr.GetObject(db.MaterialDictionaryId, OpenMode.ForRead, false)))
                {
                    string key = MaterialEntry.Key;
                    if (key != "ByBlock" && key != "ByLayer" && key != "Global" && !tr.GetObject(MaterialEntry.Value, OpenMode.ForRead).IsAProxy)
                    {
                        objectIdCollection.Add(MaterialEntry.Value);
                    }
                }
                db.Purge(objectIdCollection);
                foreach (ObjectId objid in objectIdCollection)
                {
                    objid.GetDBObject(OpenMode.ForWrite).Erase();
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeTextStyle(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objectIdCollection = new ObjectIdCollection();
                foreach (ObjectId TextStyleTableId in ((TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead)))
                {
                    TextStyleTableRecord textStyleTableRecord = (TextStyleTableRecord)tr.GetObject(TextStyleTableId, OpenMode.ForRead);
                    if (textStyleTableRecord.IsShapeFile && textStyleTableRecord.Name != "" && !textStyleTableRecord.IsAProxy && !textStyleTableRecord.IsDependent)
                    {
                        objectIdCollection.Add(TextStyleTableId);
                    }
                }
                db.Purge(objectIdCollection);
                foreach (ObjectId objid in objectIdCollection)
                {
                    objid.GetDBObject(OpenMode.ForWrite).Erase();
                }
                tr.Commit();
                return objectIdCollection.Count;
            }
        }

        public static int PurgeGroups(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                int CountDeleted = 0;
                foreach (DBDictionaryEntry GroupEntry in ((DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForRead, false)))
                {
                    Group group = (Group)tr.GetObject(GroupEntry.Value, OpenMode.ForRead, false);
                    if (group.NumEntities < 2)
                    {
                        group.ObjectId.GetDBObject(OpenMode.ForWrite).Erase();
                        CountDeleted++;
                    }
                }
                tr.Commit();
                return CountDeleted;
            }
        }
    }
}
