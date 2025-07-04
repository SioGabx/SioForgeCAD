using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ArcsExtensions
    {
        /// <summary>
        /// Gets arc bulge.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <param name="start">The start point.</param>
        /// <returns>The bulge.</returns>
        public static double GetArcBulge(this Arc arc, Point3d start)
        {
            double bulge;
            double angle = arc.EndAngle - arc.StartAngle;
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }
            if (arc.Normal.Z > 0)
            {
                bulge = Math.Tan(angle / 4);
            }
            else
            {
                bulge = -Math.Tan(angle / 4);
            }
            if (start == arc.EndPoint)
            {
                bulge = -bulge;
            }
            return bulge;
        }


        public static CircularArc2d ToCircularArc2d(this Arc arc)
        {
            Point2d start = new Point2d(arc.StartPoint.X, arc.StartPoint.Y);
            Point2d end = new Point2d(arc.EndPoint.X, arc.EndPoint.Y);

            // Clockwise : invert
            bool isClockwise = arc.Normal.Z < 0;

            double deltaAngle = (isClockwise) ? arc.StartAngle - arc.EndAngle : arc.EndAngle - arc.StartAngle;
            if (deltaAngle <= 0) { deltaAngle += 2 * Math.PI; }

            double midAngle = isClockwise
                ? arc.StartAngle - (deltaAngle / 2)
                : arc.StartAngle + (deltaAngle / 2);

            // Convert to [0, 2π] 
            midAngle = (midAngle + (2 * Math.PI)) % (2 * Math.PI);

            // Arc median point
            double midX = arc.Center.X + (arc.Radius * Math.Cos(midAngle));
            double midY = arc.Center.Y + (arc.Radius * Math.Sin(midAngle));
            Point2d mid = new Point2d(midX, midY);

            return new CircularArc2d(start, mid, end);
        }


    }
}
