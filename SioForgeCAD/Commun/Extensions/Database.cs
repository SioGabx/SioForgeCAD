using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DatabaseExtensions
    {
        public static void OpenAsNewTab(this Database db)
        {
            DocumentCollection docCol = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager;
            string FilName = Path.Combine(Path.GetTempPath(), $"Memory_{DateTime.Now.Ticks}.dwg");
            db.SaveAs(FilName, DwgVersion.Current);
            Document newDoc = docCol.Open(FilName, false);
            docCol.MdiActiveDocument = newDoc;
        }

        public static Database Duplicate(this Database db)
        {
            string tempFileName = $"DuplicateDatabase_{DateTime.Now.Ticks}_{DateTime.Now.Ticks}_{Guid.NewGuid()}.dwg";
            string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
            db.SaveAs(tempFilePath, DwgVersion.Current);
            Database newDb = new Database(false, true);
            newDb.ReadDwgFile(tempFilePath, FileShare.Read, true, null);
            Files.TryDeleteFile(tempFilePath);
            return newDb;
        }

        public static void MakeXREFPathAbsolute(this Database db, string originalDwgDir = null)
        {
            if (string.IsNullOrEmpty(originalDwgDir) && !string.IsNullOrEmpty(db.Filename))
            {
                originalDwgDir = Path.GetDirectoryName(db.Filename);
            }

            // Ne tenter de corriger que si le fichier original avait un chemin physique
            if (!string.IsNullOrEmpty(originalDwgDir))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    //XREFS
                    foreach (ObjectId btrId in (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                        if (btr.IsFromExternalReference)
                        {
                            string newPath = Files.ResolveAbsolutePath(btr.PathName, originalDwgDir);
                            if (newPath != btr.PathName)
                            {
                                btr.UpgradeOpen();
                                btr.PathName = newPath;
                            }
                        }
                    }

                    DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);

                    //Images
                    if (nod.Contains("ACAD_IMAGE_DICT"))
                    {
                        foreach (DBDictionaryEntry entry in (DBDictionary)tr.GetObject(nod.GetAt("ACAD_IMAGE_DICT"), OpenMode.ForRead))
                        {
                            RasterImageDef imageDef = (RasterImageDef)tr.GetObject(entry.Value, OpenMode.ForRead);

                            string newPath = Files.ResolveAbsolutePath(imageDef.SourceFileName, originalDwgDir);
                            if (newPath != imageDef.SourceFileName)
                            {
                                imageDef.UpgradeOpen();
                                imageDef.SourceFileName = newPath;
                            }
                        }
                    }

                    //DWG, PDF, DWF, DNG
                    foreach (string dictName in new string[] { "ACAD_PDFDEFINITIONS", "ACAD_DWFDEFINITIONS", "ACAD_DGNDEFINITIONS" })
                    {
                        if (nod.Contains(dictName))
                        {
                            foreach (DBDictionaryEntry entry in (DBDictionary)tr.GetObject(nod.GetAt(dictName), OpenMode.ForRead))
                            {
                                // On utilise la classe de base UnderlayDefinition qui englobe Pdf, Dwf et Dgn
                                UnderlayDefinition underlayDef = (UnderlayDefinition)tr.GetObject(entry.Value, OpenMode.ForRead);

                                string newPath = Files.ResolveAbsolutePath(underlayDef.SourceFileName, originalDwgDir);
                                if (newPath != underlayDef.SourceFileName)
                                {
                                    underlayDef.UpgradeOpen();
                                    underlayDef.SourceFileName = newPath;
                                }
                            }
                        }
                    }

                    if (nod.Contains("ACAD_POINTCLOUD_EX_DICT"))
                    {
                        DBDictionary pcDict = (DBDictionary)tr.GetObject(nod.GetAt("ACAD_POINTCLOUD_EX_DICT"), OpenMode.ForRead);
                        foreach (DBDictionaryEntry entry in pcDict)
                        {
                            PointCloudDefEx pcDef = (PointCloudDefEx)tr.GetObject(entry.Value, OpenMode.ForRead);

                            string newPath = Files.ResolveAbsolutePath(pcDef.SourceFileName, originalDwgDir);
                            if (newPath != pcDef.SourceFileName)
                            {
                                pcDef.UpgradeOpen();
                                pcDef.SourceFileName = newPath;
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        public static Dictionary<ObjectId, string> GetAllObjects(this Database db)
        {
            var dict = new Dictionary<ObjectId, string>();
            for (long i = 0; i < db.Handseed.Value; i++)
            {
                if (db.TryGetObjectId(new Handle(i), out ObjectId id))
                {
                    dict.Add(id, id.ObjectClass.Name);
                }
            }
            return dict;
        }

        public static Dictionary<ObjectId, string> GetAllEntities(this Database db)
        {
            var dict = new Dictionary<ObjectId, string>();
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (var btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (btr.IsLayout)
                    {
                        foreach (var id in btr)
                        {
                            dict.Add(id, id.ObjectClass.Name);
                        }
                    }
                }
                tr.Commit();
            }
            return dict;
        }

        public static ObjectId EntLast(this Database db, Type type = null)
        {
            // Autodesk.AutoCAD.Internal.Utils.EntLast();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = Generic.GetCurrentSpaceBlockTableRecord(tr);
                RXClass RXClassType = type == null ? null : RXObject.GetClass(type);
                ObjectId EntLastObjectId = ObjectId.Null;
                foreach (ObjectId objId in btr)
                {
                    if (RXClassType == null || objId.ObjectClass == RXClassType)
                    {
                        EntLastObjectId = objId;
                    }
                }
                tr.Commit();
                return EntLastObjectId;
            }
        }

        public static void SetAnnotativeScale(this Database db, string Name, double PaperUnits, double DrawingUnits)
        {
            Editor ed = Generic.GetEditor();
            if (db.Cannoscale.Name != Name)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectContextManager ocm = db.ObjectContextManager;
                    ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                    if (occ != null)
                    {
                        AnnotationScale scale = null;
                        foreach (ObjectContext obj in occ)
                        {
                            if (obj is AnnotationScale annoScale && annoScale.Name == Name)
                            {
                                scale = annoScale;
                                break;
                            }
                        }

                        if (scale == null)
                        {
                            scale = new AnnotationScale
                            {
                                Name = Name,
                                PaperUnits = PaperUnits,
                                DrawingUnits = DrawingUnits
                            };
                            occ.AddContext(scale);
                        }

                        db.Cannoscale = scale;
                        Generic.WriteMessage($"Échelle annotative définie sur {Name}.");
                    }
                    else
                    {
                        Generic.WriteMessage("Impossible d'accéder aux échelles annotatives.");
                    }
                    ed.Regen();
                    tr.Commit();
                }
            }
        }


        public static ObjectId GetObjectIdFromAppDictionary(this Database db, Transaction tr, string appDictName, string keyName)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);

            if (!nod.Contains(appDictName))
            {
                return ObjectId.Null;
            }

            var appDict = (DBDictionary)tr.GetObject(nod.GetAt(appDictName), OpenMode.ForRead);

            if (!appDict.Contains(keyName))
            {
                return ObjectId.Null;
            }

            var xrec = (Xrecord)tr.GetObject(appDict.GetAt(keyName), OpenMode.ForRead);
            var data = xrec.Data.AsArray();

            if (data.Length == 0 || !(data[0].Value is ObjectId))
            {
                return ObjectId.Null;
            }

            return (ObjectId)data[0].Value;
        }


        public static void StoreObjectIdInAppDictionary(this Database db, Transaction tr, string appDictName, string keyName, ObjectId objectId)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);

            DBDictionary appDict;
            if (!nod.Contains(appDictName))
            {
                nod.UpgradeOpen();
                appDict = new DBDictionary();
                nod.SetAt(appDictName, appDict);
                tr.AddNewlyCreatedDBObject(appDict, true);
            }
            else
            {
                appDict = (DBDictionary)tr.GetObject(nod.GetAt(appDictName), OpenMode.ForRead);
            }

            if (appDict.Contains(keyName))
            {
                return;
            }

            appDict.UpgradeOpen();
            var xrec = new Xrecord
            {
                Data = new ResultBuffer(new TypedValue((int)DxfCode.SoftPointerId, objectId))
            };
            appDict.SetAt(keyName, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }


        public static Dictionary<string, string> GetCustomProperties(this Database db)
        {
            var propsDict = new Dictionary<string, string>();

            var summaryInfo = db.SummaryInfo;
            {
                var props = summaryInfo.CustomProperties;
                if (props != null)
                {
                    var enumerator = db.SummaryInfo.CustomProperties;
                    while (enumerator.MoveNext())
                    {
                        var entry = (KeyValuePair<string, string>)enumerator.Current;
                        string key = entry.Key;
                        string value = entry.Value;
                        propsDict.Add(key, value);
                    }
                }
            }
            return propsDict;
        }
        public static void SetCustomProperties(this Database db, Dictionary<string, string> props)
        {
            var summaryInfoBuilder = new DatabaseSummaryInfoBuilder(db.SummaryInfo);
            var table = summaryInfoBuilder.CustomPropertyTable;
            int count = 0;

            foreach (var kvp in props)
            {
                table[kvp.Key] = kvp.Value;
                count++;
            }

            db.SummaryInfo = summaryInfoBuilder.ToDatabaseSummaryInfo();
        }


        public static DwgVersion GetDwgVersion(this Database db)
        {
            DwgVersion LastSaved = db.LastSavedAsVersion;
            if (LastSaved == DwgVersion.MC0To0) //Not saved
            {
                return DwgVersion.Current;
            }
            else
            {
                return LastSaved;
            }
        }

        public static long GetSize(this Database db, DwgVersion version)
        {
            string tempFileName = $"SioForgeCAD_SizeCheck_{DateTime.Now:yyMMdd_HHmmss}_{Guid.NewGuid()}.dwg";
            string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
            long sizeBytes = 0;

            try
            {
                db.SaveAs(tempFilePath, version);

                FileInfo fi = new FileInfo(tempFilePath);
                sizeBytes = fi.Length;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Erreur GetSize: " + ex.Message);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine("Impossible de supprimer le temp : " + ex.Message);
                }
            }

            return sizeBytes;
        }




    }
}
