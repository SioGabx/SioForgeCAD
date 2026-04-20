using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

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
    }
}
