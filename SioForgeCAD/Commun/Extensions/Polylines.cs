﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Extensions
{
    public static class PolylinesExtensions
    {
        public static int GetReelNumberOfVertices(this Polyline TargetPolyline)
        {
            int NumberOfVertices = (TargetPolyline.NumberOfVertices - 1);
            if (TargetPolyline.Closed)
            {
                NumberOfVertices++;
            }
            return NumberOfVertices;
        }

        public static (Point3d StartPoint, Point3d EndPoint) GetSegmentAt(this Polyline TargetPolyline, int Index)
        {
            int NumberOfVertices = TargetPolyline.NumberOfVertices;
            var PolylineSegmentStart = TargetPolyline.GetPoint3dAt(Index);
            Index += 1;
            if (Index >= NumberOfVertices)
            {
                Index = 0;
            }
            var PolylineSegmentEnd = TargetPolyline.GetPoint3dAt(Index);
            return (PolylineSegmentStart, PolylineSegmentEnd);
        }

        public static double GetArea(this Polyline pline)
        {
            double area = 0.0;
            if (pline.NumberOfVertices == 0)
            {
                return area;
            }
            int last = pline.NumberOfVertices - 1;
            Point2d p0 = pline.GetPoint2dAt(0);

            if (pline.GetBulgeAt(0) != 0.0)
            {
                area += pline.GetArcSegment2dAt(0).GetArea();
            }
            for (int i = 1; i < last; i++)
            {
                area += Point3dExtensions.GetArea(p0, pline.GetPoint2dAt(i), pline.GetPoint2dAt(i + 1));
                if (pline.GetBulgeAt(i) != 0.0)
                {
                    area += pline.GetArcSegment2dAt(i).GetArea(); ;
                }
            }
            if ((pline.GetBulgeAt(last) != 0.0) && pline.Closed)
            {
                area += pline.GetArcSegment2dAt(last).GetArea();
            }
            return area;
        }


        public static bool HasEndPointOrStartPointInCommun(this Polyline A, Polyline B)
        {
            if (A == null || B == null) return false;

            if (A.EndPoint.IsEqualTo(B.EndPoint)) return true;
            if (A.EndPoint.IsEqualTo(B.StartPoint)) return true;
            if (A.StartPoint.IsEqualTo(B.EndPoint)) return true;
            if (A.StartPoint.IsEqualTo(B.StartPoint)) return true;

            return false;
        }

        public static bool CanBeJoin(this Polyline A, Polyline B)
        {
            if (A == B) { return false; }
            if (!A.IsLineCanCloseAPolyline(B))
            {
                //Check if the polyline is already joined
                IEnumerable<Point3d> PAPoint = A.GetPoints();
                IEnumerable<Point3d> PBPoint = B.GetPoints();

                if (PAPoint.ContainsAll(PBPoint))
                {
                    return false;
                }
            }

            if (!A.HasAtLeastOnPointInCommun(B))
            {
                return false;
            }
            return true;
        }


        public static DBObjectCollection BreakAt(this Polyline poly, params Point3d[] points)
        {
            DoubleCollection DblCollection = new DoubleCollection();
            foreach (Point3d point in points)
            {
                var param = poly.GetParamAtPointX(point);
                DblCollection.Add(param);
                DblCollection.Add(param);
            }
            return poly.GetSplitCurves(DblCollection);
        }
    }
}
