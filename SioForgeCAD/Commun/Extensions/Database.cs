using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
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
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                var imageDict = (DBDictionary)tr.GetObject(NOD.GetAt("ACAD_IMAGE_DICT"), OpenMode.ForRead);
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
