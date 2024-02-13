using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static class SlicePolygon
    {
        private static bool IsSegmentIntersecting(this Polyline polyline, Line CutLine, out Point3dCollection IntersectionPointsFounds)
        {
            IntersectionPointsFounds = new Point3dCollection();
            polyline.IntersectWith(CutLine, Intersect.OnBothOperands, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count > 0;
        }

        private static DBObjectCollection GetSplittedPolyline(this Polyline polyline, Line CutLine, out DBObjectCollection InsideCutLines)
        {
            polyline.IsSegmentIntersecting(CutLine, out Point3dCollection IntersectionPointsFounds);
            if (IntersectionPointsFounds.Count == 0)
            {
                InsideCutLines = new DBObjectCollection();
                return new DBObjectCollection();
            }


            Point3dCollection OrderedIntersectionPointsFounds = IntersectionPointsFounds.OrderByDistance(CutLine.StartPoint);

            DoubleCollection DblCollection = new DoubleCollection();
            foreach (Point3d Point in IntersectionPointsFounds)
            {
                var param = polyline.GetParamAtPointX(Point);
                DblCollection.Add(param);
                DblCollection.Add(param);
            }
            DBObjectCollection SplittedPolylines = polyline.GetSplitCurves(DblCollection);

            InsideCutLines = new DBObjectCollection();
            for (var segIndex = 0; segIndex < OrderedIntersectionPointsFounds.Count; segIndex++)
            {
                Point3d StartPoint = OrderedIntersectionPointsFounds[segIndex];
                Point3d EndPoint = OrderedIntersectionPointsFounds[segIndex + 1];
                Point3d MiddlePoint = StartPoint.GetMiddlePoint(EndPoint);
                if (MiddlePoint.IsInsidePolyline(polyline))
                {
                    Polyline PolySegment = new Polyline();
                    PolySegment.AddVertex(StartPoint);
                    PolySegment.AddVertex(EndPoint);
                    InsideCutLines.Add(PolySegment);
                }
            }
            return SplittedPolylines;
        }



        public static List<Polyline> Cut(this Polyline BasePolyline, Line CutLine)
        {
            DBObjectCollection SplittedPolylines = GetSplittedPolyline(BasePolyline, CutLine, out DBObjectCollection InsideCutLines);
            DBObjectCollection SplittedPolylinesWithInsideCutLines = new DBObjectCollection().Join(InsideCutLines).Join(SplittedPolylines);
            //DBObjectCollection ClosedPolyline = new DBObjectCollection();
            foreach (Polyline polyline in SplittedPolylines)
            {
                foreach (Polyline PolySegment in InsideCutLines)
                {
                    if (polyline.IsLineCanCloseAPolyline(PolySegment))
                    {
                        polyline.Closed = true;
                        polyline.SetBulgeAt(polyline.NumberOfVertices - 1, 0);
                    }
                }
            }


            List<DBObject> Polylines = SplittedPolylines.ToList();
            List<DBObject> ClosedPolylines = Polylines.Where((poly) => (poly as Polyline).Closed == true).ToList();
            List<DBObject> NotClosedPolylines = Polylines.Where((poly) => (poly as Polyline).Closed == false).ToList();

            int index = 0;
            while (NotClosedPolylines.Count > index)
            {
                if (!(NotClosedPolylines[Math.Max(index, 0)] is Polyline PolyligneA))
                {
                    continue;
                }

                var AvailableNotClosedEntities = NotClosedPolylines.ToList();
                AvailableNotClosedEntities.AddRange(InsideCutLines.ToList());

                foreach (Polyline PolyligneB in AvailableNotClosedEntities.Cast<Polyline>())
                {
                    if (PolyligneA == PolyligneB) { continue; }
                    if (PolyligneA.HasAtLeastOnPointInCommun(PolyligneB))
                    {
                        try
                        {
                            PolyligneA.JoinEntity(PolyligneB);
                            NotClosedPolylines.Remove(PolyligneB);
                            index--;

                        }
                        catch (System.Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }

                    }
                }

                PolyligneA.Cleanup();
                if (PolyligneA.Closed)
                {
                    ClosedPolylines.Add(PolyligneA);
                }
                index++;

            }

            List<Polyline> CutedClosePolyligne = new List<Polyline>();
            foreach (Polyline polyligne in ClosedPolylines.Cast<Polyline>())
            {
                if (polyligne.Closed && polyligne.Area > 0 && !CutedClosePolyligne.Contains(polyligne))
                {
                    CutedClosePolyligne.Add(polyligne);
                }
            }

            SplittedPolylinesWithInsideCutLines.ToList()
                .Where(polyligne => !(CutedClosePolyligne
                .Contains(polyligne)))
                .ToList()
                .DeepDispose();
            return CutedClosePolyligne;
        }

    }
}
