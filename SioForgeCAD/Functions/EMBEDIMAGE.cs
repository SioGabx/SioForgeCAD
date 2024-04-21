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
using System.Drawing.Imaging;

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
                    
                    System.Drawing.Image bitmap = System.Drawing.Image.FromFile(rasterImage.Path);
                    bool ImageHasAlpha = bitmap.PixelFormat.HasFlag(PixelFormat.Alpha);
                    //Todo : warning color if rotate and / or if alpha
                    using (var RotatedImage = bitmap.RotateImage(rasterImage.Rotation, rasterImage.GetSystemColor()))
                    {
                        Clipboard.SetImage(RotatedImage.ToBitmapSource());
                    }

                    //Paste into the drawing because we cannot create a Ole2Frame in NET
                    ed.Command("_pasteclip", rasterImage.Position);

                    //Get last created entity of type Ole2Frame
                    var InsertedOLEObjectId = db.EntLast(typeof(Ole2Frame));
                    Ole2Frame InsertedOLE = InsertedOLEObjectId.GetDBObject(OpenMode.ForWrite) as Ole2Frame;
                    
                    //Move OLE at the right position
                    var rasterImageExtend = rasterImage.GetExtents();
                    TransformToFitBoundingBox(InsertedOLE, rasterImageExtend);
                    InsertedOLE.TransformBy(Matrix3d.Displacement(InsertedOLE.GetExtents().MinPoint.GetVectorTo(rasterImageExtend.MinPoint)));
                    rasterImage.CopyPropertiesTo(InsertedOLE);
                }
                tr.Commit();
            }
        }


        public static void TransformToFitBoundingBox(Ole2Frame ent, Extents3d FitBoundingBox)
        {
            var FitBoundingBoxSize = FitBoundingBox.Size();
            var EntExtendSize = ent.GetExtents().Size();
            double HeightRatio = FitBoundingBoxSize.Height / EntExtendSize.Height;
            double WidthRatio = FitBoundingBoxSize.Width / EntExtendSize.Width;
            ent.LockAspect = Math.Abs(HeightRatio - WidthRatio) <= Generic.LowTolerance.EqualPoint;
            ent.WcsHeight = FitBoundingBoxSize.Height;
            ent.WcsWidth = FitBoundingBoxSize.Width;
        }




    }
}
