﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static List<Polyline> LastSliceResult = null;
        public static List<Polyline> Slice(Polyline BasePolyline, Polyline BaseCutLine)
        {
            BaseCutLine.Elevation = BasePolyline.Elevation;
            //BasePolyline.Cleanup();
            DBObjectCollection InsideCutLines = GetInsideCutLines(BasePolyline, BaseCutLine);

            //InsideCutLines.AddToDrawing(5, true);
            List<Polyline> Polygon = new List<Polyline>() { BasePolyline.Clone() as Polyline };
            foreach (Polyline CutLine in InsideCutLines)
            {
                if (CutLine == null)
                {
                    continue;
                }
                foreach (Polyline Poly in Polygon.ToList())
                {
                    DBObjectCollection SplittedPolylines = CutCurveByCurve(Poly, CutLine, Intersect.OnBothOperands);

                    //SplittedPolylines.AddToDrawing(5, true);
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
                            else
                            {
                                TempPoly.Dispose();
                            }
                        }
                    }
                    else
                    {
                        SplittedPolylines.DeepDispose();
                    }
                }
            }
            //Polygon.AddToDrawing(5, true);
            InsideCutLines.DeepDispose();
            SetSliceCache(Polygon, BasePolyline);
            return Polygon;
        }

        public static void SetSliceCache(List<Polyline> CachePolygon, Polyline BasePolyline)
        {
            if (CachePolygon != null && CachePolygon != null)
            {
                foreach (var item in CachePolygon)
                {
                    if (item != null)
                    {
                        LastSliceResult?.Remove(item);
                    }
                }
            }
            if (BasePolyline != null)
            {
                LastSliceResult?.Remove(BasePolyline);
            }
            LastSliceResult = CachePolygon;
        }

        public static void TryDetectWrongCut(List<Polyline> Polylines, Polyline CutLine)
        {
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{            
            //List<Polyline> Cleanned = new List<Polyline>();
            Polyline[] array = Polylines.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                Polyline item = array[i];
                Polylines.Remove(item);

                GetConnectingPolylineInList(item.StartPoint, ref item, ref CutLine, ref Polylines);
                GetConnectingPolylineInList(item.EndPoint, ref item, ref CutLine, ref Polylines);
                //Cleanned.Add(item);
            }

            //using (Transaction tr = Generic.GetDatabase().TransactionManager.StartTransaction())
            //{
            //    Cleanned.AddToDrawing(2, true);
            //    tr.Commit();
            //}

            void GetConnectingPolylineInList(Point3d Origin, ref Polyline SubPoly, ref Polyline SubCutLine, ref List<Polyline> SubPolylines)
            {
                int itineration = 0;
                while (itineration <= SubPolylines.Count && Origin.DistanceTo(SubCutLine) > Generic.MediumTolerance.EqualPoint)
                {
                    itineration++;
                    foreach (var item1 in SubPolylines.ToArray())
                    {
                        if (item1.StartPoint.IsEqualTo(Origin) || item1.EndPoint.IsEqualTo(Origin))
                        {
                            //Index++;
                            //poly.AddToDrawing(Index, true);
                            //item1.AddToDrawing(Index, true);
                            SubPoly.JoinEntity(item1);
                            SubPolylines.Remove(item1);
                        }
                    }
                }
            }
        }

        public static List<Polyline> RecreateClosedPolyline(DBObjectCollection SplittedPolylines, Polyline CutLine)
        {
            DBObjectCollection SplittedPolylinesWithInsideCutLines = new DBObjectCollection() { CutLine }.AddRange(SplittedPolylines);
            TryDetectWrongCut(SplittedPolylines.Cast<Polyline>().ToList(), CutLine);
            foreach (Polyline polyline in SplittedPolylines)
            {
                if (polyline.IsCurveCanClose(CutLine))
                {
                    //polyline.AddToDrawing(2, true);
                    //CutLine.AddToDrawing(2, true);

                    polyline.JoinEntity(CutLine);
                    polyline.Closed = true;
                }
            }
            List<DBObject> Polylines = SplittedPolylines.ToList();
            List<DBObject> ClosedPolylines = Polylines.Where((poly) => (poly as Polyline).Closed).ToList();
            List<DBObject> NotClosedPolylines = Polylines.Where((poly) => !(poly as Polyline).Closed).ToList();

            int index = 0;
            int LastOperationNotClosedPolylinesCount = -1;
            int SameCountRedo = 3;
            while (NotClosedPolylines.Count > index)
            {
                if (LastOperationNotClosedPolylinesCount == NotClosedPolylines.Count)
                {
                    SameCountRedo--;
                    //If the number are the same, that mean we have not successfuly close any polyline
                    if (SameCountRedo == 0)
                    {
                        break;
                    }
                }
                else
                {
                    SameCountRedo = 3;
                }
                LastOperationNotClosedPolylinesCount = NotClosedPolylines.Count;
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RecreateClosedPolyline :" + ex);
                    }
                }

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

            SplittedPolylines.ToList().Cast<Polyline>()
                .Where(polyligne => !CutedClosePolyligne
                .Contains(polyligne))
                .ToList()
                .DeepDispose();
            return CutedClosePolyligne;
        }

        private static DBObjectCollection GetInsideCutLines(this Polyline BoundaryPolyline, Polyline CutLine)
        {
            DBObjectCollection CutLines = CutCurveByCurve(CutLine, BoundaryPolyline, Intersect.ExtendBoth);
            if (CutLines.Count == 0)
            {
                CutLines.Add(CutLine.Clone() as Polyline);
            }
            //BoundaryPolyline.AddToDrawing(3, true);
            //CutLines.AddToDrawing(3, true);
            DBObjectCollection InsideCutLines = new DBObjectCollection();
            foreach (Polyline line in CutLines)
            {
                bool IsInside = line.IsInside(BoundaryPolyline);
                bool IsOverlaping = line.IsOverlaping(BoundaryPolyline);
                if (IsInside && !IsOverlaping)
                {
                    InsideCutLines.Add(line);
                }
                else
                {
                    // line.AddToDrawing();
                    line.Dispose();
                }
            }
            //InsideCutLines.AddToDrawing(3, true);
            //Fix splitted lines

            bool SuccessfulllyJoinACutLine = true;
            while (SuccessfulllyJoinACutLine)
            {
                SuccessfulllyJoinACutLine = false;
                foreach (Polyline InsideCutLine_A in InsideCutLines.ToList().Cast<Polyline>())
                {
                    foreach (Polyline InsideCutLine_B in InsideCutLines.ToList().Cast<Polyline>())
                    {
                        if (InsideCutLines.Contains(InsideCutLine_A) && InsideCutLines.Contains(InsideCutLine_B))
                        {
                            if (InsideCutLine_A.CanBeJoinWith(InsideCutLine_B))
                            {
                                SuccessfulllyJoinACutLine = true;
                                InsideCutLine_A.JoinEntity(InsideCutLine_B);
                                InsideCutLines.Remove(InsideCutLine_B);
                                InsideCutLine_B.Dispose();
                            }
                        }
                    }
                }
            }

            //InsideCutLines.AddToDrawing(2, true);

            //Extend line to boundary intersection
            foreach (Polyline InsideCutLine in InsideCutLines.ToList().Cast<Polyline>())
            {
                BoundaryPolyline.IsSegmentIntersecting(InsideCutLine, out Point3dCollection Intersection, Intersect.ExtendArgument);
                if (Intersection.Count > 0)
                {
                    if (!Intersection.ContainsTolerance(InsideCutLine.StartPoint))
                    {
                        Point3dCollection OrderedIntersectionPointsFounds = Intersection.OrderByDistanceFromPoint(InsideCutLine.StartPoint);
                        var NewStartPoint = OrderedIntersectionPointsFounds[0];
                        if (NewStartPoint.DistanceTo(InsideCutLine.StartPoint) < CutLine.Length / 2)
                        {
                            if (InsideCutLine.EndPoint.DistanceTo(NewStartPoint) >= InsideCutLine.EndPoint.DistanceTo(InsideCutLine.StartPoint))
                            {
                                InsideCutLine.SetPointAt(0, NewStartPoint.ToPoint2d());
                            }
                        }
                    }

                    if (!Intersection.ContainsTolerance(InsideCutLine.EndPoint))
                    {
                        Point3dCollection OrderedIntersectionPointsFounds = Intersection.OrderByDistanceFromPoint(InsideCutLine.EndPoint);
                        var NewEndPoint = OrderedIntersectionPointsFounds[0];
                        if (NewEndPoint.DistanceTo(InsideCutLine.EndPoint) < CutLine.Length / 2)
                        {
                            if (InsideCutLine.StartPoint.DistanceTo(NewEndPoint) >= InsideCutLine.StartPoint.DistanceTo(InsideCutLine.EndPoint))
                            {
                                InsideCutLine.SetPointAt(InsideCutLine.NumberOfVertices - 1, NewEndPoint.ToPoint2d());
                            }
                        }
                    }
                }
            }

            return InsideCutLines;
        }

        public static DoubleCollection GetSplitPoints(this Polyline polyline, Point3dCollection IntersectionPointsFounds)
        {
            Point3dCollection OrderedIntersectionPointsFounds = IntersectionPointsFounds.OrderByDistanceOnLine(polyline);
            DoubleCollection DblCollection = new DoubleCollection();
            foreach (Point3d Point in OrderedIntersectionPointsFounds)
            {
                if (Point.IsOnPolyline(polyline))
                {
                    var param = polyline.GetParamAtPointX(Point);
                    if (!ContainsTolerance(DblCollection, param))
                    {
                        DblCollection.Add(param);
                        DblCollection.Add(param);
                    }
                }
            }
            return DblCollection;
        }

        private static bool ContainsTolerance(DoubleCollection doubles, double Value)
        {
            foreach (var item in doubles)
            {
                if (Math.Abs(item - Value) < Generic.MediumTolerance.EqualPoint)
                {
                    return true;
                }
            }
            return false;
        }

        public static DBObjectCollection TryGetSplitCurves(this Polyline polyline, DoubleCollection DblCollection)
        {
            if (DblCollection.Count == 0)
            {
                //If this is here, that mean maybe we found point near but not on the curve
                return new DBObjectCollection
                {
                    polyline.Clone() as DBObject
                };
            }
            try
            {
                var SplittedCurves = polyline.GetSplitCurves(DblCollection);
                return SplittedCurves;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TryGetSplitCurves" + ex);
            }
            return new DBObjectCollection();
        }

        public static DBObjectCollection CutCurveByCurve(this Polyline polyline, Polyline CutLine, Intersect intersect = Intersect.OnBothOperands)
        {
            polyline.IsSegmentIntersecting(CutLine, out Point3dCollection IntersectionPointsFounds, intersect);
            IntersectionPointsFounds.Add(CutLine.StartPoint);
            IntersectionPointsFounds.Add(CutLine.EndPoint);
            DoubleCollection DblCollection = GetSplitPoints(polyline, IntersectionPointsFounds);
            return TryGetSplitCurves(polyline, DblCollection);
        }
    }
}
