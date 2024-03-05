using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static class SlicePolygon
    {

        public static List<Polyline> Cut(this Polyline BasePolyline, Polyline BaseCutLine)
        {
            BaseCutLine.Elevation = BasePolyline.Elevation;
            //BasePolyline.Cleanup();
            DBObjectCollection InsideCutLines = GetInsideCutLines(BasePolyline, BaseCutLine);
            //InsideCutLines.AddToDrawing(5, true);
            List<Polyline> Polygon = new List<Polyline>() { BasePolyline };
            foreach (Polyline CutLine in InsideCutLines)
            {
                if (CutLine == null)
                {
                    continue;
                }
                foreach (Polyline Poly in Polygon.ToArray())
                {
                    DBObjectCollection SplittedPolylines = CutCurveByCurve(Poly, CutLine);

                    //SplittedPolylines.AddToDrawing(4, true);
                    if (SplittedPolylines.Count > 1)
                    {
                        Polygon.Remove(Poly);
                        var TempsResult = RecreateClosedPolyline(SplittedPolylines, CutLine);
                        if (Poly != BasePolyline)
                        {
                            Poly.Dispose();
                        }
                        foreach (var TempPoly in TempsResult)
                        {
                            if (TempPoly.TryGetArea() > 0.01)
                            {
                                TempPoly.Cleanup();
                                Polygon.Add(TempPoly);
                            }
                        }
                    }
                }
            }
            InsideCutLines.DeepDispose();
            return Polygon;
        }


        public static List<Polyline> RecreateClosedPolyline(DBObjectCollection SplittedPolylines, Polyline CutLine)
        {
            DBObjectCollection SplittedPolylinesWithInsideCutLines = new DBObjectCollection() { CutLine }.Join(SplittedPolylines);

            foreach (Polyline polyline in SplittedPolylines)
            {
                if (polyline.IsLineCanCloseAPolyline(CutLine))
                {
                    polyline.JoinEntity(CutLine);
                    polyline.Closed = true;
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
                AvailableNotClosedEntities.Add(CutLine);
                foreach (Polyline PolyligneB in AvailableNotClosedEntities.Cast<Polyline>())
                {
                    if (!PolyligneA.CanBeJoinWith(PolyligneB))
                    {
                        continue;
                    }

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


                //PolyligneA.Cleanup();
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

            SplittedPolylines.ToList()
                .Where(polyligne => !(CutedClosePolyligne
                .Contains(polyligne)))
                .ToList()
                .DeepDispose();
            return CutedClosePolyligne;
        }

        private static bool IsSegmentIntersecting(this Polyline polyline, Polyline CutLine, out Point3dCollection IntersectionPointsFounds, Intersect intersect = Intersect.OnBothOperands)
        {
            IntersectionPointsFounds = new Point3dCollection();
            polyline.IntersectWith(CutLine, intersect, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count > 0;
        }

        private static DBObjectCollection GetInsideCutLines(this Polyline polyline, Polyline CutLine)
        {
            DBObjectCollection CutLines = CutCurveByCurve(CutLine, polyline, Intersect.ExtendThis);
            if (CutLines.Count == 0)
            {
                CutLines.Add(CutLine.Clone() as Polyline);
            }
            DBObjectCollection InsideCutLines = new DBObjectCollection();
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
                    // line.AddToDrawing();
                    line.Dispose();
                }
            }

            //Fix splitted lines
            foreach (Polyline InsideCutLine_A in InsideCutLines.ToList())
            {
                foreach (Polyline InsideCutLine_B in InsideCutLines.ToList())
                {
                    if (InsideCutLines.Contains(InsideCutLine_A) && InsideCutLines.Contains(InsideCutLine_B))
                    {
                        if (InsideCutLine_A.CanBeJoinWith(InsideCutLine_B))
                        {
                            InsideCutLines.Remove(InsideCutLine_B);
                            InsideCutLine_A.JoinEntity(InsideCutLine_B);
                            InsideCutLine_B.Dispose();
                        }
                    }
                }
            }

            //InsideCutLines.AddToDrawing(2, true);

            //Extend line to boundary intersection
            foreach (Autodesk.AutoCAD.DatabaseServices.Polyline InsideCutLine in InsideCutLines.ToList())
            {
                polyline.IsSegmentIntersecting(InsideCutLine, out Point3dCollection Intersection, Intersect.ExtendArgument);
                if (Intersection.Count > 0)
                {
                    if (!Intersection.ContainsTolerance(InsideCutLine.StartPoint))
                    {
                        Point3dCollection OrderedIntersectionPointsFounds = Intersection.OrderByDistanceFromPoint(InsideCutLine.StartPoint);
                        var NewStartPoint = OrderedIntersectionPointsFounds[0];
                        if (NewStartPoint.DistanceTo(InsideCutLine.StartPoint) < CutLine.Length / 2)
                        {
                            InsideCutLine.SetPointAt(0, NewStartPoint.ToPoint2d());
                        }
                    }

                    if (!Intersection.ContainsTolerance(InsideCutLine.EndPoint))
                    {
                        Point3dCollection OrderedIntersectionPointsFounds = Intersection.OrderByDistanceFromPoint(InsideCutLine.EndPoint);
                        var NewEndPoint = OrderedIntersectionPointsFounds[0];
                        if (NewEndPoint.DistanceTo(InsideCutLine.EndPoint) < CutLine.Length / 2)
                        {
                            InsideCutLine.SetPointAt(InsideCutLine.NumberOfVertices - 1, NewEndPoint.ToPoint2d());
                        }
                    }
                }

            }

            return InsideCutLines;
        }

        public static DBObjectCollection CutCurveByCurve(this Polyline polyline, Polyline CutLine, Intersect intersect = Intersect.OnBothOperands)
        {
            polyline.IsSegmentIntersecting(CutLine, out Point3dCollection IntersectionPointsFounds, intersect);

            if (IntersectionPointsFounds.Count == 0)
            {
                return new DBObjectCollection();
            }

            Point3dCollection OrderedIntersectionPointsFounds = IntersectionPointsFounds.OrderByDistanceOnLine(polyline);
            DoubleCollection DblCollection = new DoubleCollection();
            foreach (Point3d Point in OrderedIntersectionPointsFounds)
            {
                if (Point.IsOnPolyline(polyline))
                {
                    var param = polyline.GetParamAtPointX(Point);
                    if (!DblCollection.Contains(param))
                    {
                        DblCollection.Add(param);
                        DblCollection.Add(param);
                    }
                }
            }
            try
            {
                var SplittedCurves = polyline.GetSplitCurves(DblCollection);
                return SplittedCurves;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return new DBObjectCollection();
        }
    }
}
