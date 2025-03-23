using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static void AddToDrawing(this Point3dCollection collection, int? ColorIndex = null)
        {
            foreach (Point3d item in collection)
            {
                DBPoint dBPoint = new DBPoint(item);
                if (ColorIndex != null)
                {
                    dBPoint.ColorIndex = (int)ColorIndex;
                }
                dBPoint.AddToDrawing();
            }
        }

        public static Point3dCollection AddRange(this Point3dCollection A, Point3dCollection B)
        {
            foreach (Point3d pt in B)
            {
                if (!A.Contains(pt))
                {
                    A.Add(pt);
                }
            }
            return A;
        }

        public static Point3dCollection ConvertToUCS(this Point3dCollection SCGPoint3DCollection)
        {
            Editor ed = Generic.GetEditor();
            Matrix3d SCGToUCS = ed.CurrentUserCoordinateSystem.Inverse();

            Point3dCollection UCSPoint3dCollection = new Point3dCollection();
            foreach (Point3d point in SCGPoint3DCollection)
            {
                UCSPoint3dCollection.Add(point.TransformBy(SCGToUCS));
            }
            return UCSPoint3dCollection;
        }

        public static Point3dCollection Flatten(this Point3dCollection SCGPoint3DCollection, double Elevation = 0)
        {
            Point3dCollection FlattenPoint3dCollection = new Point3dCollection();
            foreach (Point3d point in SCGPoint3DCollection)
            {
                FlattenPoint3dCollection.Add(new Point3d(point.X, point.Y, Elevation));
            }
            return FlattenPoint3dCollection;
        }

        public static Point3dCollection ToPoint3dCollection(this IEnumerable<Point3d> IEnumCollection)
        {
            return new Point3dCollection(IEnumCollection.ToArray());
        }

        public static bool ContainsTolerance(this Point3dCollection collection, Point3d Point, Tolerance? CustomTolerance = null)
        {
            if (CustomTolerance is null)
            {
                CustomTolerance = Generic.MediumTolerance;
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

        public static bool HasDuplicatePoints(this Point3dCollection points, Tolerance tolerance)
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (points[i].IsEqualTo(points[j], new Tolerance(1e-5, 1e-5)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
