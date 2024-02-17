using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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




        public static List<Polyline> OffsetPolyline(this IEnumerable<Polyline> Curves, double OffsetDistance, bool OffsetToInside)
        {
            List<Polyline> OffsetCurves = new List<Polyline>();
            if (OffsetToInside)
            {
                OffsetDistance = -Math.Abs(OffsetDistance);
            }
            foreach (var ent in Curves)
            {
                var OffsetCurve = ent.GetOffsetCurves(ent.GetArea() < 0.0 ? -OffsetDistance : OffsetDistance);
                OffsetCurves.AddRange(OffsetCurve.ToList().Cast<Polyline>());
            }
            Curves.DeepDispose();
            return OffsetCurves;
        }

        public static List<Polyline> Merge(this IEnumerable<Polyline> Curves)
        {
            var reg = Region.CreateFromCurves(Curves.ToDBObjectCollection());
            if (reg.Count > 0)
            {
                Region RegionZero = reg[0] as Region;
                for (int i = 1; i < reg.Count; i++)
                {
                    RegionZero.BooleanOperation(BooleanOperationType.BoolUnite, reg[i] as Region);
                }
                var MergedCurves = RegionZero.GetPolylines();
                return MergedCurves.Cast<Polyline>().ToList();
            }
            else
            {
                return Curves.ToList();
            }
        }
    }




}

