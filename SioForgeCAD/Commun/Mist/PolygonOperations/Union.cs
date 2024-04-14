using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static bool Union(this List<PolyHole> PolyHoleList, out List<PolyHole> UnionResult, bool AllowMarginError = false)
        {
            const double Margin = 0.01;
            var Holes = UnionHoles(PolyHoleList);



            //We cant offset self-intersection curve in autocad, we need to disable this if this is the case
            if (AllowMarginError)
            {
                foreach (var PolyHole in PolyHoleList)
                {
                    if (PolyHole.Boundary.IsSelfIntersecting(out _))
                    {
                        AllowMarginError = false;
                    }
                    if (!AllowMarginError)
                    {
                        //Break if AllowMarginError have been set to false
                        break;
                    }
                }
            }

            if (AllowMarginError)
            {
                //Offset the PolyHole boundary so you can merge a nearly touching polyline
                var PolyHoleListCopy = PolyHoleList.ToList();
                for (int i = 0; i < PolyHoleListCopy.Count; i++)
                {
                    PolyHole PolyHole = PolyHoleListCopy[i];
                    PolyHoleList.Remove(PolyHole);
                    PolyHoleList.AddRange(OffsetPolyHole(ref PolyHole, Margin));
                }
            }


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


            var PossibleBoundary = GlobalSplittedCurves.JoinMerge().Cast<Polyline>().ToList();

            //Check if generated union with boundary may result in hole,
            //only usefull if AllowMarginError is true for the moment because can cause issue with CUTHATCH if cuthole cause an another inner hole
            if (AllowMarginError)
            {
                foreach (var BoundaryA in PossibleBoundary.ToList())
                {
                    foreach (var BoundaryB in PossibleBoundary.ToList())
                    {
                        if (BoundaryA == BoundaryB) { continue; }

                        if (BoundaryA.GetInnerCentroid().IsInsidePolyline(BoundaryB))
                        {
                            Polyline Hole = BoundaryA;
                            PossibleBoundary.Remove(BoundaryA);
                            //Because a hole is generated, the inner hole is reduced, we need to expand it back
                            var OffsetBoundaryA = BoundaryA.OffsetPolyline(Margin);
                            Hole = OffsetBoundaryA.Cast<Polyline>().JoinMerge().First() as Polyline;
                            Holes.Add(Hole);
                            break;
                        }
                    }
                }
            }

            UnionResult = PolyHole.CreateFromList(PossibleBoundary, Holes);


            if (AllowMarginError)
            {
                var UnionResultCopy = UnionResult.ToList();
                //Offset the PolyHole boundary so you can merge a nearly touching polyline
                for (int i = 0; i < UnionResultCopy.Count; i++)
                {
                    PolyHole PolyHole = UnionResultCopy[i];
                    UnionResult.Remove(PolyHole);
                    UnionResult.AddRange(OffsetPolyHole(ref PolyHole, -Margin));
                }
            }



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
        }


        private static List<PolyHole> OffsetPolyHole(ref PolyHole polyHole, double OffsetDistance)
        {
            List<PolyHole> polyHoles = new List<PolyHole>();
            //Cleanup before offset
            polyHole.Boundary.Cleanup();
            var OffsetCurve = polyHole.Boundary.OffsetPolyline(OffsetDistance);
            if (OffsetCurve.Count == 0)
            {
                throw new System.Exception("Impossible de merger les courbes (erreur lors de l'offset des contours).");
            }
            var MergedOffsetCurve = OffsetCurve.Cast<Polyline>().JoinMerge().Cast<Polyline>();

            polyHole.Boundary.Dispose();

            if (MergedOffsetCurve.Count() == 1)
            {
                polyHole.Boundary = MergedOffsetCurve.First();
                polyHoles.Add(polyHole);
                return polyHoles;
            }
            else
            {
                return PolyHole.CreateFromList(MergedOffsetCurve, polyHole.Holes);
            }
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
