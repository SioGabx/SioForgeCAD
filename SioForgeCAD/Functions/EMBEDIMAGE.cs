using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SioForgeCAD.Commun.Extensions;
using Autodesk.AutoCAD.Geometry;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class EMBEDIMAGEASOLE
    {
        public static void EmbedToOle()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            var ent = ed.GetEntity("Selectionnez une image");
            if (ent.Status != PromptStatus.OK) { return; }
            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (ent.ObjectId.GetDBObject() is RasterImage rasterImage)
                {
                    System.Drawing.Image bitmap = System.Drawing.Bitmap.FromFile(rasterImage.Path);
                    Clipboard.SetImage(bitmap.RotateImage(rasterImage.Rotation).ToBitmapSource());

                    var insertionPoint = rasterImage.Position;

                    ed.Command("_pasteclip", insertionPoint);
                    var InsertedOLEObjectId = db.EntLast(typeof(Ole2Frame));
                    Ole2Frame InsertedOLE = InsertedOLEObjectId.GetDBObject(OpenMode.ForWrite) as Ole2Frame;
                    //InsertedOLE.TransformBy(Matrix3d.Scaling(rasterImage.Scale.X, insertionPoint));
                }
                tr.Commit();
            }
        }
    }
}
