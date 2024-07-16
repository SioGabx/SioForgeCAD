using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
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
            DocumentCollection docCol = Application.DocumentManager;
            string FilName = Path.Combine(Path.GetTempPath(), $"Memory_{DateTime.Now.Ticks}.dwg");
            db.SaveAs(FilName, DwgVersion.Current);
            Document newDoc = docCol.Open(FilName, false);
            docCol.MdiActiveDocument = newDoc;
        }

        public static void Purge(this Database _)
        {
            Generic.Command("_-PURGE", "_ALL", "*", "N");
        }

        public static void PurgeRegisteredApplication(this Database _)
        {
            Generic.Command("_-PURGE", "_REGAPPS", "*", "N");
        }
        public static void PurgeRasterImages(this Database db)
        {
            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                    var NODKey = NOD.GetAt("ACAD_IMAGE_DICT");
                    var imageDict = (DBDictionary)NODKey.GetDBObject(OpenMode.ForRead);
                    var imageIds = new ObjectIdCollection();
                    foreach (var entry in imageDict)
                    {
                        imageIds.Add(entry.Value);
                    }
                    db.Purge(imageIds);
                    foreach (ObjectId id in imageIds)
                    {
                        tr.GetObject(id, OpenMode.ForWrite).Erase();
                    }
                    tr.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                //Catch eKeyNotFound
                Debug.WriteLine(ex.ToString());
            }
        }

        public static Dictionary<ObjectId, string> GetAllObjects(this Database db)
        {
            var dict = new Dictionary<ObjectId, string>();
            for (long i = 0; i < db.Handseed.Value; i++)
            {
                if (db.TryGetObjectId(new Handle(i), out ObjectId id))
                    dict.Add(id, id.ObjectClass.Name);
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
                RXClass RXClassType = type == null ? null : RXClass.GetClass(type);
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

    }

}
