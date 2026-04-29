using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System.Collections.Generic;
using System.Windows;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ViewportsExtensions
    {
        public static IntegerCollection GetViewPortsNumbers(this TransientManager _)
        {
            //https://www.keanw.com/2011/03/drawing-transient-graphics-appropriately-in-autocad-within-multiple-paperspace-viewports-using-net.html
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            // Are we in model space outside floating viewports?
            // Then we'll initalize an empty IntegerCollection

            if (db.TileMode)
            {
                return new IntegerCollection();
            }

            List<int> vps = new List<int>();

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                Viewport vp = tr.GetObject(ed.ActiveViewportId, OpenMode.ForRead) as Viewport;

                // Are we in paper space and not inside a floating
                // viewport? Then only the paper space viewport itself
                // is of interest
                if (vp?.Number == 1)
                {
                    vps.Add(1);
                }
                else
                {
                    // Now we're inside a floating viewport and
                    // will display transients in active viewports
                    foreach (ObjectId vpId in db.GetViewports(false))
                    {
                        vp = (Viewport)tr.GetObject(vpId, OpenMode.ForRead);
                        vps.Add(vp.Number);
                    }
                }
                tr.Commit();
            }
            int[] ints = new int[vps.Count];
            vps.CopyTo(ints, 0);
            return ints.ToIntegerCollection();
        }

        public static Matrix3d GetModelToPaperTransform(this Viewport vport)
        {
            //https://www.theswamp.org/index.php?action=post;quote=477118;topic=42503.0;last_msg=596197
            Point3d center = new Point3d(vport.ViewCenter.X, vport.ViewCenter.Y, 0.0);
            return Matrix3d.Displacement(new Vector3d(vport.CenterPoint.X - center.X, vport.CenterPoint.Y - center.Y, 0.0))
               * Matrix3d.Scaling(vport.CustomScale, center)
               * Matrix3d.Rotation(vport.TwistAngle, Vector3d.ZAxis, Point3d.Origin)
               * Matrix3d.WorldToPlane(new Plane(vport.ViewTarget, vport.ViewDirection));
        }

        public static Matrix3d GetPaperToModelTransform(this Viewport vport)
        {
            return GetModelToPaperTransform(vport).Inverse();
        }
        public static void PaperToModel(this Entity entity, Viewport vport)
        {
            entity.TransformBy(GetModelToPaperTransform(vport).Inverse());
        }

        public static void ModelToPaper(this Entity entity, Viewport viewport)
        {
            entity.TransformBy(GetModelToPaperTransform(viewport));
        }

        public static void PaperToModel(this IEnumerable<Entity> src, Viewport viewport)
        {
            Matrix3d xform = GetPaperToModelTransform(viewport);
            foreach (Entity ent in src)
            {
                ent.TransformBy(xform);
            }
        }

        public static void ModelToPaper(this IEnumerable<Entity> src, Viewport viewport)
        {
            Matrix3d xform = GetModelToPaperTransform(viewport);
            foreach (Entity ent in src)
            {
                ent.TransformBy(xform);
            }
        }

        public static bool IsInModel(this Editor _)
        {
            return Generic.GetDatabase().TileMode;
        }

        public static bool IsInLayout(this Editor ed)
        {
            return !ed.IsInModel();
        }

        public static bool IsInLayoutPaper(this Editor ed)
        {
            Database db = ed.Document.Database;

            if (db.TileMode ||
                db.PaperSpaceVportId == ObjectId.Null ||
                ed.CurrentViewportObjectId == ObjectId.Null)
            {
                return false;
            }
            else if (ed.CurrentViewportObjectId == db.PaperSpaceVportId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsInLayoutViewport(this Editor ed)
        {
            return ed.IsInLayout() && !ed.IsInLayoutPaper();
        }


        public static List<ObjectId> GetAllViewportsInPaperSpace(this Editor _, BlockTableRecord btr)
        {
            Database db = Generic.GetDatabase();

            List<ObjectId> ListOfViewPorts = new List<ObjectId>();

            foreach (ObjectId objId in btr)
            {
                Entity entity = objId.GetEntity();
                if (entity != null && entity is Viewport && db.GetViewports(false).Contains(entity.ObjectId))
                {
                    ListOfViewPorts.Add(entity.ObjectId);
                }
            }
            return ListOfViewPorts;
        }

        /// <summary>
        /// Vérifie si la Bounding Box de l'entité (projetée en espace papier) croise la Bounding Box du Viewport.
        /// </summary>
        public static bool IsEntityVisibleInViewport(this Viewport vp, Entity ent)
        {
            try
            {
                if (ent is null) return false;

                Extents3d vpExtents = vp.GetExtents();
                Extents3d extents = ent.GetExtents();
                if (ent?.Bounds.HasValue == false) return true;

                Point3d min = extents.MinPoint;
                Point3d max = extents.MaxPoint;

                Point3d[] corners = new Point3d[]
                {
                    new Point3d(min.X, min.Y, min.Z),
                    new Point3d(max.X, min.Y, min.Z),
                    new Point3d(max.X, max.Y, min.Z),
                    new Point3d(min.X, max.Y, min.Z),
                    new Point3d(min.X, min.Y, max.Z),
                    new Point3d(max.X, min.Y, max.Z),
                    new Point3d(max.X, max.Y, max.Z),
                    new Point3d(min.X, max.Y, max.Z)
                };

                double eMinX = double.MaxValue, eMinY = double.MaxValue;
                double eMaxX = double.MinValue, eMaxY = double.MinValue;

                Matrix3d modelToPaper = vp.GetModelToPaperTransform();
                foreach (Point3d corner in corners)
                {
                    Point3d pt = corner.TransformBy(modelToPaper);
                    if (pt.X < eMinX) eMinX = pt.X;
                    if (pt.Y < eMinY) eMinY = pt.Y;
                    if (pt.X > eMaxX) eMaxX = pt.X;
                    if (pt.Y > eMaxY) eMaxY = pt.Y;
                }

                return eMaxX >= vpExtents.MinPoint.X && eMinX <= vpExtents.MaxPoint.X &&
                       eMaxY >= vpExtents.MinPoint.Y && eMinY <= vpExtents.MaxPoint.Y;
            }
            catch
            {
                return true;
            }
        }

        public static Polyline GetBoundary(this Viewport viewport)
        {
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    if (viewport == null) { return null; }
                    if (viewport.NonRectClipEntityId != ObjectId.Null)
                    {
                        // Get the non-rectangular clipping boundary
                        Entity clipEntity = viewport.NonRectClipEntityId.GetEntity(OpenMode.ForRead);
                        if (clipEntity is Curve clipEntCurve)
                        {
                            var clipPoly = clipEntCurve.ToPolyline();
                            clipPoly.ResetStyle();
                            return clipPoly;

                        }
                        return null;
                    }
                    else
                    {
                        // Get the standard rectangular boundary
                        Point3d center = viewport.CenterPoint;
                        double width = viewport.Width;
                        double height = viewport.Height;

                        Point3d lowerLeft = new Point3d(center.X - (width / 2), center.Y - (height / 2), center.Z);
                        Point3d lowerRight = new Point3d(center.X + (width / 2), center.Y - (height / 2), center.Z);
                        Point3d upperRight = new Point3d(center.X + (width / 2), center.Y + (height / 2), center.Z);
                        Point3d upperLeft = new Point3d(center.X - (width / 2), center.Y + (height / 2), center.Z);

                        Polyline polyline = new Polyline();
                        polyline.AddVertexAt(0, lowerLeft.ToPoint2d(), 0, 0, 0);
                        polyline.AddVertexAt(1, lowerRight.ToPoint2d(), 0, 0, 0);
                        polyline.AddVertexAt(2, upperRight.ToPoint2d(), 0, 0, 0);
                        polyline.AddVertexAt(3, upperLeft.ToPoint2d(), 0, 0, 0);
                        polyline.Closed = true;
                        polyline.ResetStyle();
                        return polyline;
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }
        }
    }
}
