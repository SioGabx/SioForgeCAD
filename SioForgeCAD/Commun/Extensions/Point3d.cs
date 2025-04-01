using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
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

        public static ObjectId AddToDrawing(this Point3d pnt, int? ColorIndex = null)
        {
            DBPoint dBPoint = new DBPoint(pnt);
            return dBPoint.AddToDrawing(ColorIndex);
        }

        public static Point3d GetMiddlePoint(this Point3d A, Point3d B)
        {
            double X = (A.X + B.X) / 2;
            double Z = (A.Z + B.Z) / 2;
            double Y = (A.Y + B.Y) / 2;
            return new Point3d(X, Y, Z);
        }
        public static Point3d GetMiddlePoint(this Point2d A, Point2d B)
        {
            double X = (A.X + B.X) / 2;
            double Y = (A.Y + B.Y) / 2;
            return new Point3d(X, Y, 0);
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

        public static bool ContainsAll(this IEnumerable<Point3d> pointsA, IEnumerable<Point3d> pointsB)
        {
            foreach (var item in pointsB)
            {
                if (!pointsA.Contains(item)) { return false; }
            }
            return true;
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
                Polyline NoArcPoly = polyline.ToPolygon(15);
                var Pnts = NoArcPoly.GetPoints().ToPoint3dCollection();
                if (NoArcPoly != polyline) { NoArcPoly.Dispose(); }
                return point.ToPoint2d().IsPointInsidePolygonMcMartin(Pnts);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static Matrix3d GetDisplacementMatrixTo(this Point3d Origin, Point3d Destination)
        {
            Vector3d DisplacementVector = Origin.GetVectorTo(Destination);
            return Matrix3d.Displacement(DisplacementVector);
        }

        public static Point3dCollection OrderByDistanceOnLine(this Point3dCollection collection, Polyline poly)
        {
            List<(Point3d Point, double Distance)> List = new List<(Point3d, double)>();
            foreach (Point3d point in collection)
            {
                double param = poly.GetParamAtPointX(point);
                double distance = poly.GetDistanceAtParameter(param);
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

        public static Point3dCollection OrderByDistanceFromPoint(this Point3dCollection collection, Point3d Origin)
        {
            List<(Point3d Point, double Distance)> List = new List<(Point3d, double)>();
            foreach (Point3d point in collection)
            {
                double distance = point.DistanceTo(Origin);
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

        public static double GetArea(this Point2d pt1, Point2d pt2, Point2d pt3)
        {
            return (((pt2.X - pt1.X) * (pt3.Y - pt1.Y)) -
                        ((pt3.X - pt1.X) * (pt2.Y - pt1.Y))) / 2.0;
        }

        public static double DistanceTo(this Point3d pt, Polyline pl)
        {
            return pl.GetClosestPointTo(pt, false).DistanceTo(pt);
        }

        public static bool IsOnPolyline(this Point3d pt, Polyline pl)
        {
            bool isOn = false;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Curve3d seg = null;
                SegmentType segType = pl.GetSegmentType(i);

                if (segType == SegmentType.Arc)
                {
                    seg = pl.GetArcSegmentAt(i);
                }
                else if (segType == SegmentType.Line)
                {
                    seg = pl.GetLineSegmentAt(i);
                }

                if (seg != null)
                {
                    isOn = seg.IsOn(pt, Generic.MediumTolerance);

                    if (isOn)
                    {
                        break;
                    }
                }
            }
            return isOn;
        }

        /// <summary>
        /// Determines whether a point is inside a polyline
        /// </summary>
        /// <param name="p">The point to check</param>
        /// <param name="verts">The vertices of the polyline</param>
        /// <returns>True if the point is within the polyline, otherwise false</returns>
        public static bool IsPointInsidePolygon(this Point2d p, Point3dCollection verts)
        {
            int counter = 0;
            int VertexCount = verts.Count;
            Point2d p1 = verts[0].ToPoint2d();
            for (int index = 1; (index <= VertexCount); index++)
            {
                Point2d p2 = verts[index % VertexCount].ToPoint2d();
                if (p.Y > Math.Min(p1.Y, p2.Y) && p.Y <= Math.Max(p1.Y, p2.Y) && p.X <= Math.Max(p1.X, p2.X))
                {
                    if (p1.Y != p2.Y)
                    {
                        double xinters = (((p.Y - p1.Y) * ((p2.X - p1.X) / (p2.Y - p1.Y))) + p1.X);
                        if ((p1.X == p2.X) || (p.X <= xinters))
                        {
                            counter++;
                        }
                    }
                }
                p1 = p2;
            }
            return (counter % 2) != 0;
        }

        public static bool IsPointInsidePolygonMcMartin(this Point2d p, Point3dCollection verts)
        {
            //https://erich.realtimerendering.com/ptinpoly/
            //https://www.realtimerendering.com/resources/RTNews/html//rtnv5n3.html#art3
            double tx = p.X;
            double ty = p.Y;

            //get initial test bit for above/below X axis
            var vtx0 = verts[0];
            var yflag0 = (vtx0[1] >= ty);

            bool inside_flag = false;
            for (int i = 1; i < verts.Count; i++)
            {
                var vtx1 = verts[i];

                var yflag1 = (vtx1[1] >= ty);
                // check if endpoints straddle (are on opposite sides) of X axis
                // (i.e. the Y's differ); if so, +X ray could intersect this edge.
                if (yflag0 != yflag1)
                {
                    var xflag0 = (vtx0[0] >= tx);
                    // check if endpoints are on same side of the Y axis (i.e. X's
                    // are the same); if so, it's easy to test if edge hits or misses.
                    if (xflag0 == (vtx1[0] >= tx))
                    {
                        //if edge's X values both right of the point, must hit
                        if (xflag0)
                        {
                            inside_flag = !inside_flag;
                        }
                    }
                    else
                    {
                        // compute intersection of pgon segment with +X ray, note
                        //if >= point's X; if so, the ray hits it.
                        if ((vtx1[0] - ((vtx1[1] - ty) * (vtx0[0] - vtx1[0]) / (vtx0[1] - vtx1[1]))) >= tx)
                        {
                            inside_flag = !inside_flag;
                        }
                    }
                }
                // move to next pair of vertices, retaining info as possible
                yflag0 = yflag1;
                vtx0 = vtx1;
            }
            return inside_flag;
        }
    }
}
