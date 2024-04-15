using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public const double Margin = 0.01;
        public static bool Union(this List<PolyHole> PolyHoleList, out List<PolyHole> UnionResult, bool RequireAllowMarginError = false)
        {
            var Holes = UnionHoles(PolyHoleList);
            bool AllowMarginError = RequireAllowMarginError;
            //We cant offset self-intersection curve in autocad, we need to disable this if this is the case
            if (AllowMarginError)
            {
                foreach (var PolyHole in PolyHoleList)
                {
                    PolyHole.Boundary.Cleanup();
                    if (PolyHole.Boundary.IsSelfIntersecting(out var Points))
                    {
                        Generic.WriteMessage("Self Intersecting detected. AllowMarginError is disabled");
                        //Points.AddToDrawing();
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

            //foreach (var curveItem in SplittedCurvesOrigin)
            //{
            //    curveItem.Splitted.AddToDrawing(5);
            //}

            //Check if Cutted line IsInside -> if true remove
            ConcurrentBag<Polyline> ConcurrentBagGlobalSplittedCurves = new ConcurrentBag<Polyline>();
            ConcurrentDictionary<Polyline, Polyline> NoArcPolygonCache = new ConcurrentDictionary<Polyline, Polyline>();
            Parallel.ForEach(SplittedCurvesOrigin.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = 1 }, SplittedCurveOrigin =>
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
                            bool IsInsideHole = false;
                            if (SplittedCurve.Closed)
                            {
                                //If the geometry was not splitted, that mean the curve do not cross the boundary -> if it is inside a hole, we should keep it
                                foreach (var PolyHole in PolyBase.Holes)
                                {
                                    if (SplittedCurve.GetInnerCentroid().IsInsidePolyline(PolyHole))
                                    {
                                        IsInsideHole = true;
                                        break;
                                    }
                                }
                            }

                            if (!IsInsideHole)
                            {
                                //SplittedCurve.AddToDrawing(1, true);
                                //var SplitObjId = SplittedCurve.AddToDrawing(1, true);
                                //var NoArcPolyBaseObjId = NoArcPolyBase.AddToDrawing(2, true);
                                //var PolyBaseBoundaryObjId = PolyBase.Boundary.AddToDrawing(3, true);
                                //Groups.Create("Debug", "", new ObjectIdCollection() { SplitObjId, NoArcPolyBaseObjId, PolyBaseBoundaryObjId });

                                SplittedCurves.Remove(SplittedCurve);
                                SplittedCurve.Dispose();
                            }
                        }
                    }

                }
                ConcurrentBagGlobalSplittedCurves.AddRange(SplittedCurves);
            });

            List<Polyline> GlobalSplittedCurves = ConcurrentBagGlobalSplittedCurves.ToList();

            //GlobalSplittedCurves.AddToDrawing(6, true);

            foreach (var item in NoArcPolygonCache)
            {
                if (item.Key != item.Value)
                {
                    item.Value?.Dispose();
                }
            }

            //Groups.Create("hh", "", GlobalSplittedCurves.AddToDrawing(6, true).ToObjectIdCollection());
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

            ConcurrentBagGlobalSplittedCurves.RemoveCommun(GlobalSplittedCurves).DeepDispose();


            var PossibleBoundary = GlobalSplittedCurves.JoinMerge().Cast<Polyline>().ToList();
            GlobalSplittedCurves.DeepDispose();

            //Check if generated union with boundary may result in hole,
            //only usefull if RequireAllowMarginError is true for the moment because can cause issue with CUTHATCH if cuthole cause an another inner hole
            //PossibleBoundary.AddToDrawing(6, true);
            if (RequireAllowMarginError)
            {
                foreach (var BoundaryA in PossibleBoundary.ToList())
                {
                    foreach (var BoundaryB in PossibleBoundary.ToList())
                    {
                        if (BoundaryA == BoundaryB) { continue; }

                        if (BoundaryA.IsInside(BoundaryB))
                        {
                            PossibleBoundary.Remove(BoundaryA);
                            if (AllowMarginError)
                            {
                                //Because a hole is generated, the inner hole is reduced, we need to expand it back
                                var OffsetBoundaryA = BoundaryA.SmartOffset(Margin);
                                BoundaryA.Dispose();
                                var MergedOffsetBoundaryA = OffsetBoundaryA.Cast<Polyline>().JoinMerge().Cast<Polyline>();
                                Holes.AddRange(MergedOffsetBoundaryA);
                                OffsetBoundaryA.ToList().RemoveCommun(MergedOffsetBoundaryA).DeepDispose();
                            }
                            else
                            {
                                Holes.Add(BoundaryA);
                            }

                            break;
                        }
                    }
                }
            }

            PossibleBoundary.AddToDrawing(3, true);
            //Holes.AddToDrawing(5);
            UnionResult = PolyHole.CreateFromList(PossibleBoundary, Holes);

            if (AllowMarginError)
            {
                var UnionResultCopy = UnionResult.ToList();
                //Offset the PolyHole boundary so you can merge a nearly touching polyline
                for (int i = 0; i < UnionResultCopy.Count; i++)
                {
                    PolyHole PolyHole = UnionResultCopy[i];
                    UnionResult.Remove(PolyHole);
                    var UndoMargin = OffsetPolyHole(ref PolyHole, -Margin);

                    UnionResult.AddRange(UndoMargin);
                }
                if (UnionResultCopy.Count == 0)
                {
                    return false;
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
            List<Polyline> OffsetCurve;
            if (polyHole.Boundary.NumberOfVertices < 3 || polyHole.Boundary.Area <= Generic.MediumTolerance.EqualPoint)
            {
                //degenrated geometry
                return polyHoles;
            }
            if (OffsetDistance < 0)
            {
                OffsetCurve = polyHole.Boundary.SmartOffset(OffsetDistance).ToList();
            }
            else
            {
                OffsetCurve = polyHole.Boundary.SmartOffset(OffsetDistance).Cast<Polyline>().ToList();
            }
            if (OffsetCurve.Count == 0)
            {
                (polyHole.Boundary.Clone() as Entity).AddToDrawing(6);
                Generic.WriteMessage("Impossible de merger les courbes (erreur lors de l'offset des contours). Offset value : " + OffsetDistance);
                return polyHoles;
                throw new System.Exception("Impossible de merger les courbes (erreur lors de l'offset des contours).");
            }
            //var MergedOffsetCurve = OffsetCurve.Cast<Polyline>().JoinMerge().Cast<Polyline>();

            polyHole.Boundary.Dispose();

            if (OffsetCurve.Count() == 1)
            {
                polyHole.Boundary = OffsetCurve.First();
                polyHoles.Add(polyHole);
                return polyHoles;
            }
            else
            {
                return PolyHole.CreateFromList(OffsetCurve, polyHole.Holes);
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
                    //Make sure all points are on the line because IntersectWith give not egnouht precise value (0.0001). This fix some cut
                    var OnLineIntersectionPointsFounds = new Point3dCollection();
                    foreach (Point3d item in GlobalIntersectionPointsFounds)
                    {
                        OnLineIntersectionPointsFounds.Add(PolyBase.GetClosestPointTo(item, false));
                    }

                    var SplitDouble = PolyBase.GetSplitPoints(OnLineIntersectionPointsFounds);
                    var Splitted = PolyBase.TryGetSplitCurves(SplitDouble).Cast<Polyline>().ToHashSet();
                    //Splitted.AddToDrawing(color);

                    //Remove zero length line
                    foreach (var curv in Splitted.ToList())
                    {
                        if (curv.Length <= Generic.LowTolerance.EqualPoint)
                        {
                            Splitted.Remove(curv);
                        }
                    }
                    SplittedCurvesOrigin.Add((Splitted, PolyBase));
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
