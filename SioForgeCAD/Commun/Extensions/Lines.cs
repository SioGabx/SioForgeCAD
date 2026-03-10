using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class LinesExtentions
    {
        /// <summary>
        /// Determines if two line segments intersect.
        /// </summary>
        /// <param name="a1">Line a point 1.</param>
        /// <param name="a2">Line a point 2.</param>
        /// <param name="b1">Line b point 1.</param>
        /// <param name="b2">Line b point 2.</param>
        /// <returns>The result.</returns>
        public static bool IsLineSegIntersect(Point2d a1, Point2d a2, Point2d b1, Point2d b2)
        {
            if ((a1 - a2).CrossProduct(b1 - b2) == 0)
            {
                return false;
            }

            double lambda;
            double miu;

            if (b1.X == b2.X)
            {
                lambda = (b1.X - a1.X) / (a2.X - b1.X);
                double Y = (a1.Y + (lambda * a2.Y)) / (1 + lambda);
                miu = (Y - b1.Y) / (b2.Y - Y);
            }
            else if (a1.X == a2.X)
            {
                miu = (a1.X - b1.X) / (b2.X - a1.X);
                double Y = (b1.Y + (miu * b2.Y)) / (1 + miu);
                lambda = (Y - a1.Y) / (a2.Y - Y);
            }
            else if (b1.Y == b2.Y)
            {
                lambda = (b1.Y - a1.Y) / (a2.Y - b1.Y);
                double X = (a1.X + (lambda * a2.X)) / (1 + lambda);
                miu = (X - b1.X) / (b2.X - X);
            }
            else if (a1.Y == a2.Y)
            {
                miu = (a1.Y - b1.Y) / (b2.Y - a1.Y);
                double X = (b1.X + (miu * b2.X)) / (1 + miu);
                lambda = (X - a1.X) / (a2.X - X);
            }
            else
            {
                lambda = ((b1.X * a1.Y) - (b2.X * a1.Y) - (a1.X * b1.Y) + (b2.X * b1.Y) + (a1.X * b2.Y) - (b1.X * b2.Y)) / ((-b1.X * a2.Y) + (b2.X * a2.Y) + (a2.X * b1.Y) - (b2.X * b1.Y) - (a2.X * b2.Y) + (b1.X * b2.Y));
                miu = ((-a2.X * a1.Y) + (b1.X * a1.Y) + (a1.X * a2.Y) - (b1.X * a2.Y) - (a1.X * b1.Y) + (a2.X * b1.Y)) / ((a2.X * a1.Y) - (b2.X * a1.Y) - (a1.X * a2.Y) + (b2.X * a2.Y) + (a1.X * b2.Y) - (a2.X * b2.Y)); // from Mathematica
            }

            bool result = false;
            if (lambda >= 0 || double.IsInfinity(lambda))
            {
                if (miu >= 0 || double.IsInfinity(miu))
                {
                    result = true;
                }
            }
            return result;
        }

        public static Vector3d GetVector3d(this Line line)
        {
            Vector3d direction = line.EndPoint - line.StartPoint;
            return direction.GetNormal();
        }

        public static bool IsLinePassesThroughPoint(this Line line, Point3d point)
        {
            // Comparer les coordonnées du point avec les extrémités de la ligne
            return point.X >= Math.Min(line.StartPoint.X, line.EndPoint.X) &&
                   point.X <= Math.Max(line.StartPoint.X, line.EndPoint.X) &&
                   point.Y >= Math.Min(line.StartPoint.Y, line.EndPoint.Y) &&
                   point.Y <= Math.Max(line.StartPoint.Y, line.EndPoint.Y);
        }

        public static bool IsCutting(this Line line1, Line line2, out Point3dCollection IntersectionPointsFounds)
        {
            IntersectionPointsFounds = new Point3dCollection();
            line1.IntersectWith(line2, Intersect.OnBothOperands, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count != 0;
        }

        public static bool IsCutting(this Line line1, Line line2)
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







        /// <summary>
        /// Converts line to polyline.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>A polyline.</returns>
        public static Polyline ToPolyline(this Line line)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, line.StartPoint.ToPoint2d(), 0, 0, 0);
            poly.AddVertexAt(1, line.EndPoint.ToPoint2d(), 0, 0, 0);
            return poly;
        }

        /// <summary>
        /// Converts arc to polyline.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <returns>A polyline.</returns>
        public static Polyline ToPolyline(this Arc arc)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, arc.StartPoint.ToPoint2d(), arc.GetArcBulge(arc.StartPoint), 0, 0);
            poly.AddVertexAt(1, arc.EndPoint.ToPoint2d(), 0, 0, 0);
            return poly;
        }
    }
}
