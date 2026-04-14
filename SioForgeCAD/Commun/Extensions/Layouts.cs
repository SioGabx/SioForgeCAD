using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using System;
using System.Drawing;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class LayoutsExtensions
    {
        public static void CloneLayout(this Layout sourceLayout, string newLayoutName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord sourceBtr = (BlockTableRecord)tr.GetObject(sourceLayout.BlockTableRecordId, OpenMode.ForRead);
                LayoutManager lm = LayoutManager.Current;

                ObjectId newLayoutId = lm.CreateLayout(newLayoutName);
                Layout newLayout = (Layout)tr.GetObject(newLayoutId, OpenMode.ForWrite);
                BlockTableRecord newBtr = (BlockTableRecord)tr.GetObject(newLayout.BlockTableRecordId, OpenMode.ForWrite);

                IdMapping mapping = new IdMapping();
                db.DeepCloneObjects(sourceBtr.Cast<ObjectId>().ToObjectIdCollection(), newBtr.ObjectId, mapping, false);

                // 3. Copie les réglages de tracé
                newLayout.CopyFrom(sourceLayout);
                lm.CurrentLayout = newLayoutName;

                tr.Commit();
            }
        }

        public static Bitmap GetLayoutSnapshot(this Layout lay, Extents3d ext, int width, int height)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Manager gsm = doc.GraphicsManager;

            KernelDescriptor descriptor = new KernelDescriptor();
            descriptor.addRequirement(Autodesk.AutoCAD.UniqueString.Intern("3D Drawing"));
            GraphicsKernel kernel = Manager.AcquireGraphicsKernel(descriptor);

            using (Transaction tr = lay.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForRead);

                using (View view = new View())
                {
                    double w = ext.MaxPoint.X - ext.MinPoint.X;
                    double h = ext.MaxPoint.Y - ext.MinPoint.Y;
                    Point3d center = new Point3d(ext.MinPoint.X + (w / 2), ext.MinPoint.Y + (h / 2), 0);

                    // Position de la caméra (Z+1) pour ne pas être "dans" le dessin
                    Point3d eyePosition = new Point3d(center.X, center.Y, center.Z + 1.0);

                    // Cadrage de la vue
                    view.SetView(eyePosition, center, Vector3d.YAxis, w, h);

                    using (Device dev = gsm.CreateAutoCADOffScreenDevice(kernel))
                    {
                        dev.OnSize(new Size(width, height));
                        dev.BackgroundColor = System.Drawing.Color.White;
                        dev.Add(view);

                        using (Model model = gsm.CreateAutoCADModel(kernel))
                        {
                            view.Add(btr, model);
                            dev.Update();
                            view.Update();

                            return view.GetSnapshot(new Rectangle(0, 0, width, height));
                        }
                    }
                }
            }
        }

        public static Bitmap RenderLayoutSnapshot(this Layout layout)
        {
            var db = Generic.GetDatabase();
            int bmpW = 100;
            int bmpH = 100;
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Extents3d ext;
                    if (layout.ModelType)
                    {
                        db.UpdateExt(true);
                        ext = new Extents3d(db.Extmin, db.Extmax);
                    }
                    else
                    {
                        ext = layout.Extents;
                    }

                    double realW = ext.MaxPoint.X - ext.MinPoint.X;
                    double realH = ext.MaxPoint.Y - ext.MinPoint.Y;

                    if (realW <= 0.001 || realH <= 0.001) return null;
                    double ratio = Math.Min(512.0 / realW, 512.0 / realH);

                    bmpW = (int)(realW * ratio);
                    bmpH = (int)(realH * ratio);
                    return GetLayoutSnapshot(layout, ext, bmpW, bmpH);
                }
                catch { }
                finally { transaction.Commit(); }
            }
            return new Bitmap(bmpW, bmpH);
        }

    }
}
