using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Point3dExtensions
    {
        public static Point2d ToPoint2d(this Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }
        public static Point3d ToPoint3d(this Point2d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }
        public static Point3d Flatten(this Point3d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }

        public static Points ToPoints(this Point3d p)
        {
            return Points.From3DPoint(p);
        }

        public static Point3d GetMiddlePoint(this Point3d A, Point3d B)
        {
            double X = (A.X + B.X) / 2;
            double Z = (A.Z + B.Z) / 2;
            double Y = (A.Y + B.Y) / 2;
            return new Point3d(X, Y, Z);
        }

        public static Point3d GetIntermediatePoint(this Point3d A, Point3d B, double Pourcentage)
        {
            double X = A.X.IntermediatePercentage(B.X, Pourcentage);
            double Z = A.Z.IntermediatePercentage(B.Z, Pourcentage);
            double Y = A.Y.IntermediatePercentage(B.Y, Pourcentage);
            return new Point3d(X, Y, Z);
        }

        public static double GetAngleWith(this Point3d A, Point3d B)
        {
            using (Line line = new Line(A, B))
            {
                return line.Angle;
            }
        }

        public static bool IsEqualTo(this IEnumerable<Point3d> A, IEnumerable<Point3d> B)
        {

            if (A.Count() != B.Count()) { return false; }
            Point3d[] ArrayA = A.ToArray();
            Point3d[] ArrayB = B.ToArray();
            for (int i = 0; i < A.Count(); i++)
            {
                if (!ArrayA[i].IsEqualTo(ArrayB[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool Contains(this IEnumerable<Point3d> points, Point3d Point)
        {
            foreach (var item in points)
            {
                if (item.IsEqualTo(Point)) { return true; }
            }
            return false;
        }

        public static Point3d TranformToBlockReferenceTransformation(this Point3d OriginPoint, BlockReference blkRef)
        {
            Point3d selectedPointInBlockRefSpace = OriginPoint.TransformBy(blkRef.BlockTransform.Inverse());
            // Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
            return selectedPointInBlockRefSpace;//.TransformBy(rotationMatrix);
        }


        public static Point3d Displacement(this Point3d point, Vector3d Vector, double Distance)
        {
            return point.TransformBy(Matrix3d.Displacement(Vector.GetNormal().MultiplyBy(Distance)));
        }

        public static bool IsInsidePolyline(this Point3d point, Polyline polyline)
        {
            try
            {
                using (DBObjectCollection Reg = Region.CreateFromCurves(new DBObjectCollection() { polyline }))
                using (Brep brepEnt = new Brep(Reg[0] as Region))
                {
                    brepEnt.GetPointContainment(point, out PointContainment pointCont);
                    Reg[0].Dispose();
                    return pointCont != PointContainment.Outside;
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static Point3dCollection OrderByDistanceOnLine(this Point3dCollection collection, Polyline poly)
        {
            
            List<(Point3d Point, double Distance)> List = new List<(Point3d, double)>();
            foreach (Point3d point in collection)
            {
                double distance = poly.GetDistAtPoint(point);
                List.Add((point, distance));
            }
            var orderedList = List.OrderBy(item => item.Distance).ToList();
            var newCollection = new Point3dCollection();
            foreach (var item in orderedList)
            {
                newCollection.Add(item.Point);
            }
            return newCollection;
        }

        public static double GetArea(Point2d pt1, Point2d pt2, Point2d pt3)
        {
            return (((pt2.X - pt1.X) * (pt3.Y - pt1.Y)) -
                        ((pt3.X - pt1.X) * (pt2.Y - pt1.Y))) / 2.0;
        }


    }
}
