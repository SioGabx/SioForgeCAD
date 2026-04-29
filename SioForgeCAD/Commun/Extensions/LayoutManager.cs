using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace SioForgeCAD.Commun.Extensions
{
    public static class LayoutManagerExtensions
    {
        public static BitmapSource GetLayoutImage(this LayoutManager lm, string Name)
        {
            var db = Generic.GetDatabase();
            var Doc = Generic.GetDocument();
            if (string.IsNullOrEmpty(Name))
            {
                return null;
            }
            BitmapSource result = null;
            try
            {

                Bitmap bitmap = Utils.GetLayoutThumbnail(Doc, Name);
                if (bitmap == null)
                {
                    Database database = Doc.Database;
                    if (database != null)
                    {
                        try
                        {
                            using (OpenCloseTransaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                            using (DBObject dBObject = transaction.GetObject(database.LayoutDictionaryId, OpenMode.ForRead))
                            {
                                if (!(dBObject is DBDictionary dBDictionary))
                                {
                                    return null;
                                }
                                ObjectId at = dBDictionary.GetAt(Name);
                                Layout layout = (Layout)transaction.GetObject(at, OpenMode.ForRead);
                                bitmap = layout.Thumbnail;
                            }

                        }
                        catch { }

                        if (bitmap == null)
                        {
                            Debug.WriteLine("Manualy generate Thumbnail (alternative to _UPDATETHUMBSNOW)");
                            //Manualy generate Thumbnail (alternative to _UPDATETHUMBSNOW)
                            try
                            {
                                using (Transaction transaction = db.TransactionManager.StartTransaction())
                                {
                                    var id = lm.GetLayoutId(Name);
                                    Layout layout = (Layout)transaction.GetObject(id, OpenMode.ForRead);
                                    bitmap = layout.RenderLayoutSnapshot();
                                }
                            }
                            catch { }
                        }
                    }

                    if (bitmap == null)
                    {
                        bitmap = new Bitmap(100, 100);
                    }
                }
                if (bitmap != null)
                {
                    result = bitmap.CreateBitmapSourceFromBitmap();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
            }
            return result;
        }


        public static Layout GetCurrentLayout(this LayoutManager lm)
        {
            Database db = Generic.GetDatabase();
            string currentLayoutName = lm.CurrentLayout;
            DBDictionary layoutDict = (DBDictionary)db.LayoutDictionaryId.GetDBObject(OpenMode.ForRead);
            return (Layout)layoutDict.GetAt(currentLayoutName).GetDBObject(OpenMode.ForRead);
        }

        public static bool CurrentLayoutIsModel(this LayoutManager lm)
        {
            return lm.CurrentLayout == "Model";
        }



        public static List<string> GetLayoutNamesFromFile(this LayoutManager lm, string filePath)
        {
            try
            {
                var path = Environment.ExpandEnvironmentVariables(filePath);
                if (File.Exists(path))
                {
                    // On lit le fichier sans l'ouvrir visuellement dans AutoCAD
                    using (Database db = new Database(false, true))
                    {

                        db.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, null);
                        return GetLayoutsInDatabase(lm, db);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur lors de la lecture du gabarit : " + ex.Message);
            }
            return new List<string>();
        }

        public static List<string> GetLayoutNames(this LayoutManager lm)
        {
            var db = Generic.GetDatabase();
            return GetLayoutsInDatabase(lm, db);
        }

        private static List<string> GetLayoutsInDatabase(this LayoutManager _, Database db)
        {
            List<string> layoutNames = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (DBDictionaryEntry entry in (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead))
                {
                    Layout layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                    if (!layout.ModelType) // On exclut l'espace Objet
                    {
                        layoutNames.Add(layout.LayoutName);
                    }
                }
                tr.Commit();
            }
            return layoutNames;
        }

        public static HashSet<string> GetSelectedLayoutNames(this LayoutManager _, Database db)
        {
            HashSet<string> selected = new HashSet<string>();
            selected.Add(_.CurrentLayout);
            // OpenCloseTransaction est beaucoup plus rapide et léger qu'une Transaction normale
            // Idéal pour une lecture rapide dans l'événement Idle.
            using (OpenCloseTransaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (DBDictionaryEntry entry in (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead))
                {
                    Layout layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);

                    if (layout.TabSelected)
                    {
                        selected.Add(layout.LayoutName);
                    }
                }
                tr.Commit();
            }
            return selected;
        }

        public static bool CreateLayoutFromTemplate(this LayoutManager lm, string filePath, string layoutName, string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) targetName = layoutName;
            Database destDb = Generic.GetDatabase();

            using (Generic.GetLock())
            {
                try
                {
                    var path = Environment.ExpandEnvironmentVariables(filePath);
                    if (!File.Exists(path)) { return false; }
                    // true en 2ème paramètre = ouverture en mémoire, sans verrouiller le fichier physique
                    using (Database srcDb = new Database(false, true))
                    {
                        srcDb.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, "");
                        ObjectId srcLayoutId = ObjectId.Null;

                        string tempLayoutName = $"TEMP_{Guid.NewGuid():N}";

                        using (Transaction srcTr = srcDb.TransactionManager.StartTransaction())
                        {
                            DBDictionary layoutDict = (DBDictionary)srcTr.GetObject(srcDb.LayoutDictionaryId, OpenMode.ForRead);

                            if (!layoutDict.Contains(layoutName))
                            {
                                return false; // Le gabarit ne contient pas la présentation demandée
                            }

                            srcLayoutId = layoutDict.GetAt(layoutName);
                            Layout lay = (Layout)srcTr.GetObject(srcLayoutId, OpenMode.ForWrite);
                            lay.LayoutName = tempLayoutName;
                            lay.TabOrder = int.MaxValue;
                            srcTr.Commit();
                        }

                        if (srcLayoutId != ObjectId.Null)
                        {
                            using (Transaction destTr = destDb.TransactionManager.StartTransaction())
                            {
                                ObjectIdCollection ids = new ObjectIdCollection { srcLayoutId };
                                IdMapping idMap = new IdMapping();

                                destDb.WblockCloneObjects(ids, destDb.LayoutDictionaryId, idMap, DuplicateRecordCloning.Ignore, false);

                                if (!idMap.Contains(srcLayoutId) || !idMap[srcLayoutId].IsCloned)
                                {
                                    return false;
                                }

                                destTr.Commit();
                            }
                            lm.RenameLayout(tempLayoutName, targetName);

                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Generic.WriteMessage($"Erreur lors de l'import du gabarit : {ex.Message}");
                    return false;
                }
            }
            return false;
        }


    }
}
