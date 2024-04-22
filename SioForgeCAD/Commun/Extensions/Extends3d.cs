using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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

        public static Extents3d GetExtents(this IEnumerable<Entity> entities)
        {
            if (entities.Any()) { 
            var extent = entities.First().GeometricExtents;
            foreach (var ent in entities)
            {
                extent.AddExtents(ent.GetExtents());
            }
            return extent;
            }
            else
            {
                return new Extents3d();
            }
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

        public static Point3d GetCenter(this Extents3d extents)
        {
            Point3d TopLeft = new Point3d(extents.MinPoint.X, extents.MaxPoint.Y, 0);
            Point3d BottomRight = new Point3d(extents.MaxPoint.X, extents.MinPoint.Y, 0);

            return TopLeft.GetMiddlePoint(BottomRight);
        }
        public static void Expand(this ref Extents3d extents, double factor)
        {
            var center = extents.GetCenter();
            extents = new Extents3d(center + (factor * (extents.MinPoint - center)), center + (factor * (extents.MaxPoint - center)));
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
