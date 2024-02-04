using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Extends3dExtensions
    {
        public static Point3d TopLeft(this Extents3d extends)
        {
            return new Point3d(extends.MaxPoint.X, extends.MaxPoint.Y, 0);
        }
        public static Point3d TopRight(this Extents3d extends)
        {
            return new Point3d(extends.MinPoint.X, extends.MaxPoint.Y, 0);
        }
        public static Point3d BottomLeft(this Extents3d extends)
        {
            return new Point3d(extends.MaxPoint.X, extends.MinPoint.Y, 0);
        }
        public static Point3d BottomRight(this Extents3d extends)
        {
            return new Point3d(extends.MinPoint.X, extends.MinPoint.Y, 0);
        }

        public static Size Size(this Extents3d extends)
        {
            var Dim = new Size
            {
                Width = extends.TopLeft().DistanceTo(extends.TopLeft()),
                Height = extends.BottomLeft().DistanceTo(extends.BottomRight())
            };
            return Dim;
        }

        public static Extents3d GetExtents(this IEnumerable<Entity> entities)
        {
            var extent = entities.First().GeometricExtents;
            foreach (var ent in entities)
            {
                extent.AddExtents(ent.GeometricExtents);
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
            return Point3d.Origin + 0.5 * (extents.MinPoint.GetAsVector() + extents.MaxPoint.GetAsVector());
        }
        public static Extents3d Expand(this Extents3d extents, double factor)
        {
            var center = extents.GetCenter();
            return new Extents3d(center + factor * (extents.MinPoint - center), center + factor * (extents.MaxPoint - center));
        }
        public static bool IsPointIn(this Extents3d extents, Point3d point)
        {
            return point.X >= extents.MinPoint.X && point.X <= extents.MaxPoint.X
                && point.Y >= extents.MinPoint.Y && point.Y <= extents.MaxPoint.Y
                && point.Z >= extents.MinPoint.Z && point.Z <= extents.MaxPoint.Z;
        }

        public static Point3d GetCenter(this IEnumerable<ObjectId> entIds)
        {
            return entIds.GetExtents().GetCenter();
        }



    }
}
