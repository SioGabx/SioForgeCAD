using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;


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
                double Y = (a1.Y + lambda * a2.Y) / (1 + lambda);
                miu = (Y - b1.Y) / (b2.Y - Y);
            }
            else if (a1.X == a2.X)
            {
                miu = (a1.X - b1.X) / (b2.X - a1.X);
                double Y = (b1.Y + miu * b2.Y) / (1 + miu);
                lambda = (Y - a1.Y) / (a2.Y - Y);
            }
            else if (b1.Y == b2.Y)
            {
                lambda = (b1.Y - a1.Y) / (a2.Y - b1.Y);
                double X = (a1.X + lambda * a2.X) / (1 + lambda);
                miu = (X - b1.X) / (b2.X - X);
            }
            else if (a1.Y == a2.Y)
            {
                miu = (a1.Y - b1.Y) / (b2.Y - a1.Y);
                double X = (b1.X + miu * b2.X) / (1 + miu);
                lambda = (X - a1.X) / (a2.X - X);
            }
            else
            {
                lambda = (b1.X * a1.Y - b2.X * a1.Y - a1.X * b1.Y + b2.X * b1.Y + a1.X * b2.Y - b1.X * b2.Y) / (-b1.X * a2.Y + b2.X * a2.Y + a2.X * b1.Y - b2.X * b1.Y - a2.X * b2.Y + b1.X * b2.Y);
                miu = (-a2.X * a1.Y + b1.X * a1.Y + a1.X * a2.Y - b1.X * a2.Y - a1.X * b1.Y + a2.X * b1.Y) / (a2.X * a1.Y - b2.X * a1.Y - a1.X * a2.Y + b2.X * a2.Y + a1.X * b2.Y - a2.X * b2.Y); // from Mathematica
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
