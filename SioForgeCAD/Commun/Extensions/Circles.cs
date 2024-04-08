using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SioForgeCAD.Commun.Extensions
{
    public static class CirclesExtensions
    {
        public static Polyline ToPolyline(this Circle circle)
        {
            Polyline pline = new Polyline();
            double bulge = 1.0;
            double halfWidth = 0.0;

            pline.AddVertexAt(0, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
            pline.AddVertexAt(1, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
            pline.Closed = true;
            return pline;
        }



    }
}
