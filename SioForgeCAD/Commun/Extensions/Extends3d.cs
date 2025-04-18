using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Extends3dExtensions
    {
        public static double Left(this Extents3d extends)
        {
            return extends.MinPoint.X;
        }
        public static double Right(this Extents3d extends)
        {
            return extends.MaxPoint.X;
        }
        public static double Top(this Extents3d extends)
        {
            return extends.MaxPoint.Y;
        }
        public static double Bottom(this Extents3d extends)
        {
            return extends.MinPoint.Y;
        }
        public static Point3d TopLeft(this Extents3d extends)
        {
            return new Point3d(extends.Left(), extends.Top(), 0);
        }
        public static Point3d TopRight(this Extents3d extends)
        {
            return new Point3d(extends.MaxPoint.X, extends.Top(), 0);
        }
        public static Point3d BottomLeft(this Extents3d extends)
        {
            return new Point3d(extends.Left(), extends.Bottom(), 0);
        }
        public static Point3d BottomRight(this Extents3d extends)
        {
            return new Point3d(extends.Right(), extends.Bottom(), 0);
        }

        public static Size Size(this Extents3d extends)
        {
            return new Size
            {
                Width = extends.TopLeft().DistanceTo(extends.TopRight()),
                Height = extends.TopLeft().DistanceTo(extends.BottomLeft())
            };
        }

        public static bool CollideWith(this Extents3d a, Extents3d b)
        {
            return !(b.Left() > a.Right() || b.Right() < a.Left() || b.Top() < a.Bottom() || b.Bottom() > a.Top());
        }
        public static bool CollideWithOrConnected(this Extents3d a, Extents3d b)
        {
            return !(b.Left() >= a.Right() || b.Right() <= a.Left() || b.Top() <= a.Bottom() || b.Bottom() >= a.Top());
        }

        public static bool IsFullyInside(this Extents3d a, Extents3d b)
        {
            return a.MinPoint.X >= b.MinPoint.X &&
                   a.MaxPoint.X <= b.MaxPoint.X &&
                   a.MinPoint.Y >= b.MinPoint.Y &&
                   a.MaxPoint.Y <= b.MaxPoint.Y;
        }

        static readonly object _GetExtentsLock = new object();
        public static Extents3d GetExtents(this Entity entity)
        {
            //GetExtents is not thread safe
            lock (_GetExtentsLock)
            {
                if (entity != null && entity?.Bounds.HasValue == true)
                {
                    return entity.GeometricExtents;
                }
                return new Extents3d();
            }
        }

        public static Extents3d GetExtents(this IEnumerable<object> entities)
        {
            if (entities.Any())
            {
                var extent = (entities.First(obj => obj is Entity) as Entity).GeometricExtents;
                foreach (var dbobj in entities)
                {
                    if (dbobj is Entity ent)
                    {
                        extent.AddExtents(ent.GetExtents());
                    }
                }
                return extent;
            }
            else
            {
                return new Extents3d();
            }
        }
        public static Extents3d GetExtents(this DBObjectCollection entities)
        {
            return GetExtents(entities.ToArray());
        }

        public static Extents3d GetExtents(this IEnumerable<ObjectId> entities)
        {
            List<Entity> list = new List<Entity>();
            foreach (var ent in entities)
            {
                if (ent.GetEntity(OpenMode.ForRead) is Entity entity)
                {
                    list.Add(entity);
                }
            }
            return list.GetExtents();
        }

        public static Extents3d GetVisualExtents(this Entity ent, out Point3dCollection entPts)
        {
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                entPts = CollectPoints(tr, ent);

                Extents3d extents = new Extents3d();
                foreach (Point3d item in entPts)
                {
                    extents.AddExtents(new Extents3d(item, item));
                }
                tr.Commit();
                return extents;
            }
        }

        private static Point3dCollection CollectPoints(Transaction tr, Entity ent)
        {
            // The collection of points to populate and return
            Point3dCollection pts = new Point3dCollection();

            // We'll start by checking a block reference for
            // attributes, getting their bounds and adding
            // them to the point list. We'll still explode
            // the BlockReference later, to gather points
            // from other geometry, it's just that approach
            // doesn't work for attributes (we only get the
            // AttributeDefinitions, which don't have bounds)

            BlockReference br = ent as BlockReference;
            if (br != null)
            {
                foreach (ObjectId arId in br.AttributeCollection)
                {
                    DBObject obj = tr.GetObject(arId, OpenMode.ForRead);
                    if (obj is AttributeReference)
                    {
                        AttributeReference ar = (AttributeReference)obj;
                        ar.ExtractBounds(pts);
                    }
                }
            }
            // If we have a curve - other than a polyline, which
            // we will want to explode - we'll get points along
            // its length

            Curve cur = ent as Curve;

            if (cur != null && !(cur is Polyline || cur is Polyline2d || cur is Polyline3d))
            {
                // Two points are enough for a line, we'll go with
                // a higher number for other curves
                int segs = (ent is Line ? 2 : 20);
                double param = cur.EndParam - cur.StartParam;

                for (int i = 0; i < segs; i++)
                {
                    try
                    {
                        Point3d pt = cur.GetPointAtParameter(cur.StartParam + (i * param / (segs - 1)));
                        pts.Add(pt);
                    }
                    catch { }
                }
            }
            else if (ent is DBPoint)
            {
                pts.Add(((DBPoint)ent).Position);
            }
            else if (ent is DBText)
            {
                ((DBText)ent).ExtractBounds(pts);
            }
            //else if (ent is MText)
            //{
            //    // MText is also easy - you get all four corners
            //    // returned by a function. That said, the points
            //    // are of the MText's box, so may well be different
            //    // from the bounds of the actual contents
            //    MText txt = (MText)ent;
            //    Point3dCollection pts2 = txt.GetBoundingPoints();
            //    foreach (Point3d pt in pts2)
            //    {
            //        pts.Add(pt);
            //    }
            //}
            else if (ent is Face)
            {
                Face f = (Face)ent;
                try
                {
                    for (short i = 0; i < 4; i++)
                    {
                        pts.Add(f.GetVertexAt(i));
                    }
                }
                catch { }
            }
            else if (ent is Solid)
            {
                Solid sol = (Solid)ent;
                try
                {
                    for (short i = 0; i < 4; i++)
                    {
                        pts.Add(sol.GetPointAt(i));
                    }
                }
                catch { }
            }
            else
            {
                // Here's where we attempt to explode other types
                // of object
                DBObjectCollection oc = new DBObjectCollection();
                try
                {
                    ent.Explode(oc);
                    if (oc.Count > 0)
                    {
                        foreach (DBObject obj in oc)
                        {
                            Entity ent2 = obj as Entity;
                            if (ent2?.Visible == true)
                            {
                                foreach (Point3d pt in CollectPoints(tr, ent2))
                                {
                                    pts.Add(pt);
                                }
                            }
                            obj.Dispose();
                        }
                    }
                }
                catch { }
            }
            return pts;
        }

        public static Point3d GetCenter(this Extents3d extents)
        {
            return new Point3d(
                   (extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                   (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0,
                   (extents.MinPoint.Z + extents.MaxPoint.Z) / 2.0
               );
        }
        public static void Expand(this ref Extents3d extents, double factor)
        {
            var center = extents.GetCenter();
            Point3d Min = center + (factor * (extents.MinPoint - center));
            Point3d Max = center + (factor * (extents.MaxPoint - center));
            try
            {
                extents = new Extents3d(Min, Max);
            }
            catch
            {
                extents = new Extents3d();
            }
        }

        public static bool IsPointIn(this Extents3d extents, Point3d point)
        {
            return point.X >= extents.MinPoint.X && point.X <= extents.MaxPoint.X
                && point.Y >= extents.MinPoint.Y && point.Y <= extents.MaxPoint.Y
                && point.Z >= extents.MinPoint.Z && point.Z <= extents.MaxPoint.Z;
        }

        public static bool IsInside(this Polyline LineB, Extents3d extents, bool CheckEach = true)
        {
            int NumberOfVertices = 1;
            if (CheckEach)
            {
                NumberOfVertices = LineB.GetReelNumberOfVertices();
            }

            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < NumberOfVertices; PolylineSegmentIndex++)
            {
                var PolylineSegment = LineB.GetSegmentAt(PolylineSegmentIndex);
                Point3d MiddlePoint;
                if (LineB.GetSegmentType(PolylineSegmentIndex) == SegmentType.Arc)
                {
                    var Startparam = LineB.GetParameterAtPoint(PolylineSegment.StartPoint);
                    var Endparam = LineB.GetParameterAtPoint(PolylineSegment.EndPoint);
                    MiddlePoint = LineB.GetPointAtParam(Startparam + ((Endparam - Startparam) / 2));
                }
                else
                {
                    MiddlePoint = PolylineSegment.StartPoint.GetMiddlePoint(PolylineSegment.EndPoint);
                }

                if ((PolylineSegment.StartPoint.DistanceTo(PolylineSegment.EndPoint) / 2) > Generic.MediumTolerance.EqualPoint)
                {
                    if (!extents.IsPointIn(MiddlePoint))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static Point3d GetCenter(this IEnumerable<ObjectId> entIds)
        {
            return entIds.GetExtents().GetCenter();
        }

        public static Polyline GetGeometry(this Extents3d extents3D)
        {
            var outline = new Polyline();
            outline.AddVertex(extents3D.TopLeft());
            outline.AddVertex(extents3D.TopRight());
            outline.AddVertex(extents3D.BottomRight());
            outline.AddVertex(extents3D.BottomLeft());
            outline.Closed = true;
            return outline;
        }
        public static Rectangle3d ToRectangle3d(this Extents3d extents3D)
        {
            return new Rectangle3d(extents3D.TopLeft(), extents3D.TopRight(), extents3D.BottomLeft(), extents3D.BottomRight());
        }

        // Lifted from
        // http://docs.autodesk.com/ACD/2010/ENU/AutoCAD%20.NET%20Developer%27s%20Guide/files/WS1a9193826455f5ff2566ffd511ff6f8c7ca-4363.htm
        public static void ZoomExtents(this Extents3d extents)
        {
            var ed = Generic.GetEditor();
            // Get the current view
            using (ViewTableRecord acView = ed.GetCurrentView())
            {
                // Translate WCS coordinates to DCS
                Matrix3d matWCS2DCS = Matrix3d.Rotation(-acView.ViewTwist, acView.ViewDirection, acView.Target) * Matrix3d.Displacement(acView.Target - Point3d.Origin) * Matrix3d.PlaneToWorld(acView.ViewDirection);

                // Calculate the ratio between the width and height of the current view
                double dViewRatio = (acView.Width / acView.Height);

                // Tranform the extents of the view
                extents.TransformBy(matWCS2DCS.Inverse());

                // Calculate the new width and height of the current view
                double dWidth = extents.MaxPoint.X - extents.MinPoint.X;
                double dHeight = extents.MaxPoint.Y - extents.MinPoint.Y;

                // Check to see if the new width fits in current window
                if (dWidth > dHeight * dViewRatio)
                {
                    dHeight = dWidth / dViewRatio;
                }

                // Get the center of the view
                Point2d pNewCentPt = new Point2d((extents.MaxPoint.X + extents.MinPoint.X) * 0.5, (extents.MaxPoint.Y + extents.MinPoint.Y) * 0.5);

                // Resize the view
                acView.Height = dHeight;
                acView.Width = dWidth;

                // Set the center of the view
                acView.CenterPoint = pNewCentPt;

                // Set the current view
                ed.SetCurrentView(acView);
            }
        }
    }
}
