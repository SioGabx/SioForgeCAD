using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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


    }
}
