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
            var Dim = new Size
            {
                Width = extends.TopLeft().DistanceTo(extends.TopRight()),
                Height = extends.BottomLeft().DistanceTo(extends.BottomRight())
            };
            return Dim;
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

        public static Extents3d GetExtents(this Entity entity)
        {
            if (entity != null && entity.Bounds.HasValue)
            {
                return entity.GeometricExtents;
            }
            return new Extents3d();
        }

        public static Extents3d GetExtents(this IEnumerable<Entity> entities)
        {
            var extent = entities.First().GeometricExtents;
            foreach (var ent in entities)
            {
                extent.AddExtents(ent.GetExtents());
            }
            return extent;
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
            extents = new Extents3d(center + factor * (extents.MinPoint - center), center + factor * (extents.MaxPoint - center));
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

    }
}
