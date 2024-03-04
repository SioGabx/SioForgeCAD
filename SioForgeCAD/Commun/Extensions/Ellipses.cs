using System;
using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
/*
namespace SioForgeCAD.Commun.Extensions
{
    public static class EllipsesExtensions
    {
        public PolylineSegmentCollection(Ellipse ellipse)
        {
            Assert.IsNotNull(ellipse, nameof(ellipse));
            double pi = Math.PI;
            Plane plane = new Plane(Point3d.Origin, ellipse.Normal);
            Point3d cen3d = ellipse.Center;
            Point3d pt3d0 = cen3d + ellipse.MajorAxis;
            Point3d pt3d4 = cen3d + ellipse.MinorAxis;
            Point3d pt3d2 = ellipse.GetPointAtParameter(pi / 4.0);
            Point2d cen = cen3d.Convert2d(plane);
            Point2d pt0 = pt3d0.Convert2d(plane);
            Point2d pt2 = pt3d2.Convert2d(plane);
            Point2d pt4 = pt3d4.Convert2d(plane);
            Line2d line01 = new Line2d(pt0, (pt4 - cen).GetNormal() + (pt2 - pt0).GetNormal());
            Line2d line21 = new Line2d(pt2, (pt0 - pt4).GetNormal() + (pt0 - pt2).GetNormal());
            Line2d line23 = new Line2d(pt2, (pt4 - pt0).GetNormal() + (pt4 - pt2).GetNormal());
            Line2d line43 = new Line2d(pt4, (pt0 - cen).GetNormal() + (pt2 - pt4).GetNormal());
            Line2d majAx = new Line2d(cen, pt0);
            Line2d minAx = new Line2d(cen, pt4);
            Point2d pt1 = line01.IntersectWith(line21)[0];
            Point2d pt3 = line23.IntersectWith(line43)[0];
            Point2d pt5 = pt3.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt6 = pt2.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt7 = pt1.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt8 = pt0.TransformBy(Matrix2d.Mirroring(minAx));
            Point2d pt9 = pt7.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt10 = pt6.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt11 = pt5.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt12 = pt4.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt13 = pt3.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt14 = pt2.TransformBy(Matrix2d.Mirroring(majAx));
            Point2d pt15 = pt1.TransformBy(Matrix2d.Mirroring(majAx));
            double bulge1 = Math.Tan((pt4 - cen).GetAngleTo(pt1 - pt0) / 2.0);
            double bulge2 = Math.Tan((pt1 - pt2).GetAngleTo(pt0 - pt4) / 2.0);
            double bulge3 = Math.Tan((pt4 - pt0).GetAngleTo(pt3 - pt2) / 2.0);
            double bulge4 = Math.Tan((pt3 - pt4).GetAngleTo(pt0 - cen) / 2.0);
            Add(new PolylineSegment(pt0, pt1, bulge1));
            Add(new PolylineSegment(pt1, pt2, bulge2));
            Add(new PolylineSegment(pt2, pt3, bulge3));
            Add(new PolylineSegment(pt3, pt4, bulge4));
            Add(new PolylineSegment(pt4, pt5, bulge4));
            Add(new PolylineSegment(pt5, pt6, bulge3));
            Add(new PolylineSegment(pt6, pt7, bulge2));
            Add(new PolylineSegment(pt7, pt8, bulge1));
            Add(new PolylineSegment(pt8, pt9, bulge1));
            Add(new PolylineSegment(pt9, pt10, bulge2));
            Add(new PolylineSegment(pt10, pt11, bulge3));
            Add(new PolylineSegment(pt11, pt12, bulge4));
            Add(new PolylineSegment(pt12, pt13, bulge4));
            Add(new PolylineSegment(pt13, pt14, bulge3));
            Add(new PolylineSegment(pt14, pt15, bulge2));
            Add(new PolylineSegment(pt15, pt0, bulge1));

            // if elliptical arc:
            if (!ellipse.Closed)
            {
                double startParam, endParam;
                Point2d startPoint = ellipse.StartPoint.Convert2d(plane);
                Point2d endPoint = ellipse.EndPoint.Convert2d(plane);

                int startIndex = GetClosestSegmentIndexTo(startPoint);
                startPoint = this[startIndex].ToCurve2d().GetClosestPointTo(startPoint).Point;
                if (startPoint.IsEqualTo(this[startIndex].EndPoint))
                {
                    if (startIndex == 15)
                        startIndex = 0;
                    else
                        startIndex++;
                    startParam = 0.0;
                }
                else
                {
                    startParam = this[startIndex].GetParameterOf(startPoint);
                }

                int endIndex = GetClosestSegmentIndexTo(endPoint);
                endPoint = this[endIndex].ToCurve2d().GetClosestPointTo(endPoint).Point;
                if (endPoint.IsEqualTo(this[endIndex].StartPoint))
                {
                    if (endIndex == 0)
                        endIndex = 15;
                    else
                        endIndex--;
                    endParam = 1.0;
                }
                else
                {
                    endParam = this[endIndex].GetParameterOf(endPoint);
                }

                if (startParam != 0.0)
                {
                    this[startIndex].StartPoint = startPoint;
                    this[startIndex].Bulge = this[startIndex].Bulge * (1.0 - startParam);
                }

                if (endParam != 1.0)
                {
                    this[endIndex].EndPoint = endPoint;
                    this[endIndex].Bulge = this[endIndex].Bulge * endParam;
                }

                if (startIndex == endIndex)
                {
                    PolylineSegment segment = this[startIndex];
                    Clear();
                    Add(segment);
                }

                else if (startIndex < endIndex)
                {
                    RemoveRange(endIndex + 1, 15 - endIndex);
                    RemoveRange(0, startIndex);
                }
                else
                {
                    AddRange(GetRange(0, endIndex + 1));
                    RemoveRange(0, startIndex);
                }
            }
        }
        public static Polyline ToPolyline(this Ellipse ellipse)
        {
            if (ellipse == null)
            {
                throw new ArgumentNullException("ellipse est null");
            }
            Polyline pline = new PolylineSegmentCollection(ellipse).ToPolyline();
            pline.Closed = ellipse.Closed;
            pline.Normal = ellipse.Normal;
            pline.Elevation = ellipse.Center.TransformBy(Matrix3d.WorldToPlane(new Plane(Point3d.Origin, ellipse.Normal))).Z;
            return pline;
        }
    }
}
*/