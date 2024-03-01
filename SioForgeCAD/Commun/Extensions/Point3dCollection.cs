using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Point3dCollectionExtensions
    {
        public static Point3d[] ToArray(this Point3dCollection collection)
        {
            Point3d[] array = new Point3d[collection.Count];
            collection.CopyTo(array, 0);
            return array;
        }

        public static void AddToDrawing(this Point3dCollection collection)
        {
            foreach (Point3d item in collection)
            {
                DBPoint dBPoint = new DBPoint(item);
                dBPoint.AddToDrawing();
            }
        }

        public static Point3dCollection ToPoint3dCollection (this IEnumerable<Point3d> IEnumCollection)
        {
            return new Point3dCollection(IEnumCollection.ToArray());
        }

        public static bool ContainsTolerance(this Point3dCollection collection, Point3d Point, Tolerance? CustomTolerance = null)
        {
            if (CustomTolerance is null)
            {
                CustomTolerance = Tolerance.Global;
            }
            foreach (Point3d CollectionPoint in collection)
            {
                if (CollectionPoint.IsEqualTo(Point, (Tolerance)CustomTolerance))
                {
                    return true;
                }
            }
            return false;
        }



    }
}
