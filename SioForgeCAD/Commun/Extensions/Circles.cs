using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class CirclesExtensions
    {
        public static Polyline ToPolyline(this Circle circle)
        {
            return circle.ToPolyline2Pt();
        }

        public static Polyline ToPolyline2Pt(this Circle circle)
        {
            Polyline pline = new Polyline();
            const double bulge = 1.0;
            const double halfWidth = 0.0;

            pline.AddVertexAt(0, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
            pline.AddVertexAt(1, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
            pline.Closed = true;
            return pline;
        }

        public static Polyline ToPolyline4Pt(this Circle circle)
        {
            Polyline pline = new Polyline();
            double bulge = -Math.Tan((90 * Math.PI / 180) / 2); //90 is the angle between points //4 is the number of points
            const double polyWidth = 0.0;

            pline.AddVertexAt(0, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y), bulge, polyWidth, polyWidth);
            pline.AddVertexAt(1, new Point2d(circle.Center.X, circle.Center.Y + circle.Radius), bulge, polyWidth, polyWidth);
            pline.AddVertexAt(2, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y), bulge, polyWidth, polyWidth);
            pline.AddVertexAt(3, new Point2d(circle.Center.X, circle.Center.Y - circle.Radius), bulge, polyWidth, polyWidth);
            pline.Closed = true;
            return pline;
        }
    }
}
