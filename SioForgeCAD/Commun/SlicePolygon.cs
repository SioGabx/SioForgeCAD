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
        private static bool IsSegmentIntersecting(this Polyline polyline, Polyline CutLine, out Point3dCollection IntersectionPointsFounds)
        {
            IntersectionPointsFounds = new Point3dCollection();
            polyline.IntersectWith(CutLine, Intersect.OnBothOperands, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count > 0;
        }

        private static DBObjectCollection GetSplittedPolyline(this Polyline polyline, Polyline CutLine, out DBObjectCollection InsideCutLines)
        {
            DBObjectCollection CutLines = CutCurveByCurve(CutLine, polyline);
            InsideCutLines = new DBObjectCollection();
            foreach (Polyline line in CutLines)
            {
                bool IsInside = true;
                for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < line.GetReelNumberOfVertices(); PolylineSegmentIndex++)
                {
                    var PolylineSegment = line.GetSegmentAt(PolylineSegmentIndex);
                    if (IsInside)
                    {
                        Point3d MiddlePoint = PolylineSegment.StartPoint.GetMiddlePoint(PolylineSegment.EndPoint);
                        IsInside = MiddlePoint.IsInsidePolyline(polyline);
                    }
                }
                if (IsInside)
                {
                    InsideCutLines.Add(line);
                }
                else
                {
                    line.Dispose();
                }
            }

            return CutCurveByCurve(polyline, CutLine);
        }

        private static DBObjectCollection CutCurveByCurve(this Polyline polyline, Polyline CutLine)
        {
            polyline.IsSegmentIntersecting(CutLine, out Point3dCollection IntersectionPointsFounds);
            if (IntersectionPointsFounds.Count == 0)
            {
                return new DBObjectCollection();
            }

            Point3dCollection OrderedIntersectionPointsFounds = IntersectionPointsFounds.OrderByDistanceOnLine(polyline);

            DoubleCollection DblCollection = new DoubleCollection();
            foreach (Point3d Point in OrderedIntersectionPointsFounds)
            {
                var param = polyline.GetParamAtPointX(Point);
                DblCollection.Add(param);
                DblCollection.Add(param);
            }
            return polyline.GetSplitCurves(DblCollection);
        }


        public static List<Polyline> Cut(this Polyline BasePolyline, Polyline CutLine)
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
                        polyline.JoinEntity(PolySegment);
                        polyline.Closed = true;
                        //polyline.SetBulgeAt(polyline.NumberOfVertices - 1, 0);
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


                    

                    bool CanBeJoin;
                    if (PolyligneA.IsLineCanCloseAPolyline(PolyligneB))
                    {
                        CanBeJoin = false;
                    }
                    else
                    {
                        //Check if the polyline is already joined
                        IEnumerable<Point3d> PAPoint = PolyligneA.GetPoints();
                        IEnumerable<Point3d> PBPoint = PolyligneB.GetPoints();
                        CanBeJoin = PAPoint.ContainsAll(PBPoint);
                    }

                    if (!CanBeJoin)
                    {
                        try
                        {
                            if (PolyligneA.HasAtLeastOnPointInCommun(PolyligneB))
                            {
                                PolyligneA.JoinEntity(PolyligneB);
                                NotClosedPolylines.Remove(PolyligneB);
                                index--;
                            }
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
