using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static bool Union(this List<PolyHole> PolyHoleList, out List<PolyHole> UnionResult)
        {

            var Holes = UnionHoles(PolyHoleList);

            ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin)> SplittedCurvesOrigin = GetSplittedCurves(PolyHoleList.GetBoundaries());

            //Check if Cutted line IsInside -> if true remove
            ConcurrentBag<Polyline> ConcurrentBagGlobalSplittedCurves = new ConcurrentBag<Polyline>();
            ConcurrentDictionary<Polyline, Polyline> NoArcPolygonCache = new ConcurrentDictionary<Polyline, Polyline>();
            Parallel.ForEach(SplittedCurvesOrigin.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = -1 }, SplittedCurveOrigin =>
            {
                HashSet<Polyline> SplittedCurves = SplittedCurveOrigin.Splitted;

                foreach (var PolyBase in PolyHoleList)
                {
                    if (PolyBase.Boundary.IsDisposed)
                    {
                        continue;
                    }
                    NoArcPolygonCache.TryGetValue(PolyBase.Boundary, out Polyline NoArcPolyBase);
                    var PolyBaseExtend = PolyBase.Boundary.GetExtents();
                    foreach (var SplittedCurve in SplittedCurves.ToArray())
                    {
                        if (PolyBase.Boundary == SplittedCurveOrigin.GeometryOrigin)
                        {
                            continue;
                        }
                        if (!SplittedCurve.IsInside(PolyBaseExtend, false))
                        {
                            continue;
                        }
                        if (NoArcPolyBase == null)
                        {
                            NoArcPolyBase = PolyBase.Boundary.ToPolygon(Cleanup: false);
                            NoArcPolygonCache.TryAdd(PolyBase.Boundary, NoArcPolyBase);
                        }
                        if (SplittedCurve.IsInside(NoArcPolyBase, false) && !SplittedCurve.IsOverlaping(PolyBase.Boundary))
                        {
                            SplittedCurves.Remove(SplittedCurve);
                            SplittedCurve.Dispose();
                        }
                    }

                }
                ConcurrentBagGlobalSplittedCurves.AddRange(SplittedCurves);
            });

            List<Polyline> GlobalSplittedCurves = ConcurrentBagGlobalSplittedCurves.ToList();



            foreach (var item in NoArcPolygonCache)
            {
                if (item.Key != item.Value)
                {
                    item.Value?.Dispose();
                }
            }

            //Remove IsOverlaping line
            object _lock = new object();
            Parallel.ForEach(GlobalSplittedCurves.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = -1 }, SplittedCurveA =>
            {

                foreach (var SplittedCurveB in GlobalSplittedCurves.ToArray())
                {
                    if (SplittedCurveB != null && SplittedCurveA != SplittedCurveB)
                    {
                        if (SplittedCurveA.IsSameAs(SplittedCurveB))
                        {
                            lock (_lock)
                            {
                                GlobalSplittedCurves.Remove(SplittedCurveA);
                            }
                            break;
                        }
                    }
                }
            });

            foreach (var item in ConcurrentBagGlobalSplittedCurves.RemoveCommun(GlobalSplittedCurves))
            {
                item.Dispose();
            }

            UnionResult = PolyHole.CreateFromList(GlobalSplittedCurves.Join().Cast<Polyline>(), Holes);
            return true;
        }

        private static List<Polyline> UnionHoles(List<PolyHole> PolyHoleList)
        {
            List<Polyline> HoleUnionResult = new List<Polyline>();
            if (PolyHoleList.Count == 0)
            {
                return new List<Polyline>();
            }

            foreach (var Hole in PolyHoleList.GetAllHoles())
            {
                HoleUnionResult.Add(Hole.Clone() as Polyline);
            }

            //Skip the first one
            for (int PolyHoleListIndex = 0; PolyHoleListIndex < PolyHoleList.Count; PolyHoleListIndex++)
            {
                var polyHole = PolyHoleList[PolyHoleListIndex];
                foreach (var ParsedHole in HoleUnionResult.ToList())
                {
                    if (ParsedHole.IsSegmentIntersecting(polyHole.Boundary, out Point3dCollection IntersectionPointsFounds, Intersect.OnBothOperands) || ParsedHole.IsInside(polyHole.Boundary, false))
                    {
                        HoleUnionResult.Remove(ParsedHole);
                        if (PolygonOperation.Substraction(new PolyHole(ParsedHole as Polyline, null), new Polyline[] { polyHole.Boundary }, out var SubResult))
                        {
                            HoleUnionResult.AddRange(SubResult.GetBoundaries());
                        }
                        if (!HoleUnionResult.Contains(ParsedHole))
                        {
                            ParsedHole.Dispose();
                        }
                    }
                }
            }

            //Remove part that is leaving inside 2 polygon, they will be calculated after. 
            foreach (var Hole in HoleUnionResult.ToList())
            {
                int MaxNumberOfContainPolygon = 2;
                foreach (var polyHole in PolyHoleList.GetBoundaries())
                {
                    if (Hole.GetInnerCentroid().IsInsidePolyline(polyHole))
                    {
                        MaxNumberOfContainPolygon--;
                    }

                    //if MaxNumberOfContainPolygon reach 0, that mean the hole is inside two or more boundary
                    if (MaxNumberOfContainPolygon <= 0)
                    {
                        HoleUnionResult.Remove(Hole);
                        Hole.Dispose();
                    }
                }
            }


            //Inner hole, get intersection
            foreach (var PolyHoleA in PolyHoleList)
            {
                foreach (var PolyHoleB in PolyHoleList)
                {
                    if (PolyHoleB == PolyHoleA) { continue; }
                    foreach (var HoleA in PolyHoleA.Holes)
                    {
                        foreach (var HoleB in PolyHoleB.Holes)
                        {
                            PolygonOperation.Intersection(new PolyHole(HoleA, null), new PolyHole(HoleB, null), out var IntersectResult);
                            HoleUnionResult.AddRange(IntersectResult.GetBoundaries());
                        }
                    }
                }
            }

            return HoleUnionResult;



            //    foreach (var PolyHole in PolyHoleList)
            //{
            //    HoleUnionResult.AddRange(PolyHole.Holes);
            //    foreach (var OtherPolyHole in PolyHoleList)
            //    {
            //        if (OtherPolyHole != PolyHole) { continue; }
            //        foreach (var Hole in HoleUnionResult)
            //        {
            //            if (Hole.IsSegmentIntersecting(OtherPolyHole.Boundary, out Point3dCollection IntersectionPointsFounds, Intersect.OnBothOperands))
            //            {
            //                new PolyHole(Hole, null).Substraction(new Polyline[] { OtherPolyHole.Boundary }, out var SubstractionResult);
            //                HoleUnionResult.AddRange(SubstractionResult.GetBoundaries());
            //            }
            //        }
            //    }

            //}
            return null;
        }




        private static ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin)> GetSplittedCurves(List<Polyline> Polylines)
        {
            //This function split each polygon by other polygon intersection points
            ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin)> SplittedCurvesOrigin = new ConcurrentBag<(HashSet<Polyline>, Polyline)>();
            foreach (var PolyBase in Polylines)
            {
                if (PolyBase.IsDisposed) { continue; }
                Point3dCollection GlobalIntersectionPointsFounds = new Point3dCollection();
                var PolyBaseExtend = PolyBase.GetExtents();
                foreach (var PolyCut in Polylines)
                {
                    if (PolyCut == PolyBase) { continue; }
                    if (PolyCut.IsDisposed) { continue; }
                    if (PolyCut.GetExtents().CollideWithOrConnected(PolyBaseExtend))
                    {
                        PolyBase.IsSegmentIntersecting(PolyCut, out Point3dCollection IntersectionPointsFounds, Intersect.OnBothOperands);
                        GlobalIntersectionPointsFounds.AddRange(IntersectionPointsFounds);
                    }
                }

                if (GlobalIntersectionPointsFounds.Count > 0)
                {
                    var SplitDouble = PolyBase.GetSplitPoints(GlobalIntersectionPointsFounds);
                    SplittedCurvesOrigin.Add((PolyBase.TryGetSplitCurves(SplitDouble).Cast<Polyline>().ToHashSet(), PolyBase));
                }
                else
                {
                    SplittedCurvesOrigin.Add((new HashSet<Polyline>() { PolyBase }, PolyBase));
                }
            }
            return SplittedCurvesOrigin;
        }




    }
}
