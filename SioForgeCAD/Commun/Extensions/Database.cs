using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
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
    }
}
