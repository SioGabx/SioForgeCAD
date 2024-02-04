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
    }
}
