using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Lines
    {
        public static double GetLength(Point3d PointA, Point3d PointB)
        {
            return PointA.Flatten().DistanceTo(PointB.Flatten());
        }

        public static double GetLength(Points PointA, Points PointB)
        {
            return GetLength(PointA.SCG, PointB.SCG);
        }

        public static Vector3d GetVector3d(this Line line)
        {
            Vector3d direction = line.EndPoint - line.StartPoint;
            return direction.GetNormal();
        }

        //public static Polyline ToPolyline(this Line line)
        //{
        //    Polyline polyline = new Polyline();
        //    polyline.AddVertexAt(0, line.StartPoint.ToPoint2d(), 0, 0, 0);
        //    polyline.AddVertexAt(1, line.EndPoint.ToPoint2d(), 0, 0, 0);
        //    return polyline;
        //}

        public static bool IsLinePassesThroughPoint(this Line line, Point3d point)
        {
            // Comparer les coordonnées du point avec les extrémités de la ligne
            return point.X >= Math.Min(line.StartPoint.X, line.EndPoint.X) &&
                   point.X <= Math.Max(line.StartPoint.X, line.EndPoint.X) &&
                   point.Y >= Math.Min(line.StartPoint.Y, line.EndPoint.Y) &&
                   point.Y <= Math.Max(line.StartPoint.Y, line.EndPoint.Y);
        }

        public static bool AreLinesCutting(Line line1, Line line2, out Point3dCollection IntersectionPointsFounds)
        {
            IntersectionPointsFounds = new Point3dCollection();
            line1.IntersectWith(line2, Intersect.OnBothOperands, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count != 0;
        }

        public static bool AreLinesCutting(Line line1, Line line2)
        {
            double x1 = line1.StartPoint.X;
            double y1 = line1.StartPoint.Y;
            double x2 = line1.EndPoint.X;
            double y2 = line1.EndPoint.Y;

            double x3 = line2.StartPoint.X;
            double y3 = line2.StartPoint.Y;
            double x4 = line2.EndPoint.X;
            double y4 = line2.EndPoint.Y;

            // Calculate the direction vectors
            double uA = (((x4 - x3) * (y1 - y3)) - ((y4 - y3) * (x1 - x3))) /
                        (((y4 - y3) * (x2 - x1)) - ((x4 - x3) * (y2 - y1)));

            double uB = (((x2 - x1) * (y1 - y3)) - ((y2 - y1) * (x1 - x3))) /
                        (((y4 - y3) * (x2 - x1)) - ((x4 - x3) * (y2 - y1)));

            // If 0 <= uA <= 1 and 0 <= uB <= 1, the lines intersect
            return uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
        }

        public static Line GetFromPoints(Points start, Points end)
        {
            return new Line(start.SCG, end.SCG);
        }

        public static ObjectId Draw(Points start, Points end, int? ColorIndex = null)
        {
            using (Line acLine = GetFromPoints(start, end))
            {
                return Draw(acLine, ColorIndex);
            }
        }

        public static ObjectId Draw(Point3d start, Point3d end, int? ColorIndex = null)
        {
            using (Line acLine = new Line(start, end))
            {
                return Draw(acLine, ColorIndex);
            }
        }

        public static ObjectId Draw(Line acLine, int? ColorIndex = 256)
        {
            if (ColorIndex != null)
            {
                acLine.ColorIndex = ColorIndex ?? 0;
            }
            return acLine.AddToDrawing();
        }
    }
}
