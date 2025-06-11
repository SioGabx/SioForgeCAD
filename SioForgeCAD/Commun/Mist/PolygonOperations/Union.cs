using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public const double Margin = 0.01;
        public static bool Union(List<PolyHole> PolyHoleList, out List<PolyHole> UnionResult, bool RequestAllowMarginError = false)
        {
            //Don't run if we have no element to union
            if (PolyHoleList.Count == 0)
            {
                UnionResult = new List<PolyHole>();
                return false;
            }
            //We cant offset self-intersection curve in autocad, we need to disable this if this is the case
            bool AllowMarginError = RequestAllowMarginError && CheckAllowMarginError(PolyHoleList);

            List<Polyline> Holes = UnionHoles(PolyHoleList, AllowMarginError);

            Extents3d ExtendBeforeUnion = PolyHoleList.GetBoundaries().GetExtents();

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
            //SplittedCurvesOrigin.ForEach(curve => curve.Splitted.AddToDrawing(5, true));

            //Check if Cutted line IsInside -> if true remove
            List<Polyline> GlobalSplittedCurves = RemoveInsideCutLine(PolyHoleList, SplittedCurvesOrigin);


            List<Polyline> PossibleBoundary = GlobalSplittedCurves.JoinMerge().Cast<Polyline>().ToList();
            if (!(PossibleBoundary.Count == 1 && PossibleBoundary.First().Closed == true))
            {

                PossibleBoundary.DeepDispose();
                List<Polyline> FilteredSplittedCurves = RemoveOverlaping(GlobalSplittedCurves);
                FilteredSplittedCurves.CleanupPolylines();

                PossibleBoundary = FilteredSplittedCurves.JoinMerge().Cast<Polyline>().ToList();

                GlobalSplittedCurves.RemoveCommun(FilteredSplittedCurves).DeepDispose();
                foreach (var item in PossibleBoundary.ToList())
                {
                    if (item.TryGetArea() == 0)
                    {
                        PossibleBoundary.Remove(item);
                        item.Dispose();
                    }
                }
                ///Dispose unused

                if (AllowMarginError)
                {

                    FilteredSplittedCurves.DeepDispose();
                }
                else
                {
                    FilteredSplittedCurves.RemoveCommun(PolyHoleList.GetBoundaries()).DeepDispose();
                }
            }
            else
            {
                GlobalSplittedCurves.RemoveCommun(PolyHoleList.GetBoundaries()).DeepDispose();
            }



            if (RequestAllowMarginError)
            {
                ///Check if generated union with boundary may result in hole,
                ///only usefull if RequireAllowMarginError is true for the moment because can cause issue with CUTHATCH if cuthole cause an another inner hole
                CheckBoundaryUnionResultInHole(PossibleBoundary, Holes, AllowMarginError);
            }

            if (AllowMarginError)
            {
                //PossibleBoundary.AddToDrawing(4, true);
                /// This block allow to try deleting last and first segment
                /// if overlapping with previous on (usefull when JoinMerge has merged wrong Margin segments
                var clonedBoundaries = PossibleBoundary.ConvertAll(pe => (Polyline)pe.Clone());
                clonedBoundaries.ForEach(CleanPolylineSegments);
                //clonedBoundaries.AddToDrawing(5, true);
                var mergedBoundaries = clonedBoundaries.JoinMerge().Cast<Polyline>().ToList();
                //mergedBoundaries.AddToDrawing(6, true);

                clonedBoundaries.RemoveCommun(mergedBoundaries).DeepDispose();
                if (mergedBoundaries.Count < PossibleBoundary.Count)
                {
                    PossibleBoundary.DeepDispose();
                    PossibleBoundary = mergedBoundaries;
                }
                else
                {
                    mergedBoundaries.DeepDispose();
                }
            }


            UnionResult = PolyHole.CreateFromList(PossibleBoundary, Holes);

            if (AllowMarginError)
            {
                var UnionResultCopy = UnionResult.ToList();
                //UnionResultCopy.GetBoundaries().AddToDrawing(1, true);

                if (UnionResultCopy.Count == 0)
                {
                    return false;
                }

                //Undo offset PolyHole boundary 
                for (int i = 0; i < UnionResultCopy.Count; i++)
                {
                    PolyHole PolyHole = UnionResultCopy[i];
                    UnionResult.Remove(PolyHole);
                    var UndoMargin = OffsetPolyHole(ref PolyHole, -Margin);
                    if (UndoMargin.Count == 0)
                    {
                        return false;
                    }
                    UnionResult.AddRange(UndoMargin);
                }
            }
            //UnionResult.GetBoundaries().AddToDrawing(1);
            Extents3d ExtendAfterUnion = UnionResult.GetBoundaries().GetExtents();
            var ExtendBeforeUnionSize = ExtendBeforeUnion.Size();
            var ExtendAfterUnionSize = ExtendAfterUnion.Size();

            //If size of the extend is different, that mean the union failled at some point
            return Math.Abs(ExtendBeforeUnionSize.Width - ExtendAfterUnionSize.Width) < Generic.LowTolerance.EqualPoint
                && Math.Abs(ExtendBeforeUnionSize.Height - ExtendAfterUnionSize.Height) < Generic.LowTolerance.EqualPoint;
        }


        public static void CleanPolylineSegments(Polyline pline)
        {

            if (pline == null || pline.NumberOfVertices < 3)
            {
                Debug.WriteLine("Polyligne invalide ou pas assez de sommets.");
                return;
            }

            // === DEBUT : vérifier segments [0] et [1]
            if (pline.GetSegmentType(0) == SegmentType.Line &&
                pline.GetSegmentType(1) == SegmentType.Line)
            {
                var seg0 = pline.GetLineSegment2dAt(0);
                var seg1 = pline.GetLineSegment2dAt(1);

                var dir0 = seg0.EndPoint - seg0.StartPoint;
                var dir1 = seg1.EndPoint - seg1.StartPoint;

                var IsParallelTo = dir0.IsParallelTo(dir1, Generic.MediumTolerance);
                if (IsParallelTo)
                {
                    pline.RemoveVertexAt(0);
                    Debug.WriteLine("Segment du début supprimé (superposition détectée).");
                }
                seg0.Dispose();
                seg1.Dispose();
            }

            // === FIN : vérifier segments [N-2] et [N-1]
            int count = pline.NumberOfVertices;
            if (count >= 3 &&
                pline.GetSegmentType(count - 2) == SegmentType.Line &&
                pline.GetSegmentType(count - 1) == SegmentType.Line)
            {
                var segA = pline.GetLineSegment2dAt(count - 2);
                var segB = pline.GetLineSegment2dAt(count - 1);

                var dirA = segA.EndPoint - segA.StartPoint;
                var dirB = segB.EndPoint - segB.StartPoint;

                var IsParallelTo = dirA.IsParallelTo(dirB, Generic.MediumTolerance);
                if (IsParallelTo)
                {
                    pline.RemoveVertexAt(count - 1);
                    Debug.WriteLine("Segment de fin supprimé (superposition détectée).");
                }
                segA.Dispose();
                segB.Dispose();
            }

        }


        private static void CheckBoundaryUnionResultInHole(List<Polyline> PossibleBoundary, List<Polyline> Holes, bool AllowMarginError)
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
                            var MergedOffsetBoundaryA = OffsetBoundaryA.JoinMerge().Cast<Polyline>();
                            Holes.AddRange(MergedOffsetBoundaryA);
                            OffsetBoundaryA.DeepDispose();
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

        private static List<Polyline> RemoveOverlaping(List<Polyline> Curves)
        {
            object _lock = new object();
            var NoOverlapingCurves = new List<Polyline>(Curves);
            Parallel.ForEach(Curves, new ParallelOptions { MaxDegreeOfParallelism = Settings.MultithreadingMaxNumberOfThread }, SplittedCurveA =>
            {
                foreach (var SplittedCurveB in Curves.ToArray())
                {
                    if (SplittedCurveB != null && SplittedCurveA != SplittedCurveB)
                    {
                        if (SplittedCurveA.IsSameAs(SplittedCurveB))
                        {
                            lock (_lock)
                            {
                                if (NoOverlapingCurves.Contains(SplittedCurveB))
                                {
                                    NoOverlapingCurves.Remove(SplittedCurveA);
                                }
                            }
                            break;
                        }
                    }
                }
            });

            return NoOverlapingCurves;
        }

        private static List<Polyline> RemoveInsideCutLine(List<PolyHole> PolyHoleList, ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin)> SplittedCurvesOrigin)
        {
            ConcurrentBag<Polyline> GlobalSplittedCurves = new ConcurrentBag<Polyline>();
            ConcurrentDictionary<Polyline, Polyline> NoArcPolygonCache = new ConcurrentDictionary<Polyline, Polyline>();
            Parallel.ForEach(SplittedCurvesOrigin.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = Settings.MultithreadingMaxNumberOfThread }, SplittedCurveOrigin =>
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
                            NoArcPolyBase = PolyBase.Boundary.ToPolygon(15);
                            NoArcPolygonCache.TryAdd(PolyBase.Boundary, NoArcPolyBase);
                        }
                        if (SplittedCurve.IsInside(NoArcPolyBase, false) && !SplittedCurve.IsOverlaping(PolyBase.Boundary)) // need to add a check if it overlaping an another, we should remove it anyway
                        {
                            SplittedCurves.Remove(SplittedCurve);
                            SplittedCurve.Dispose();
                        }
                    }
                }
                GlobalSplittedCurves.AddRange(SplittedCurves);
            });

            foreach (var item in NoArcPolygonCache)
            {
                if (item.Key != item.Value)
                {
                    item.Value?.Dispose();
                }
            }

            return GlobalSplittedCurves.ToList();
        }

        private static bool CheckAllowMarginError(List<PolyHole> PolyHoleList)
        {
            foreach (var PolyHole in PolyHoleList)
            {
                PolyHole.Boundary.Cleanup();
                if (PolyHole.Boundary.IsSelfIntersecting(out _))
                {
                    Generic.WriteMessage("Self Intersecting detected. AllowMarginError is disabled");
                    return false;
                }
            }
            return true;
        }

        private static List<Polyline> UnionHoles(List<PolyHole> PolyHoleList, bool RequestAllowMarginError = false)
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

            //Substract Boundary from each hole if they intersect
            for (int PolyHoleListIndex = 0; PolyHoleListIndex < PolyHoleList.Count; PolyHoleListIndex++)
            {
                var polyHole = PolyHoleList[PolyHoleListIndex];

                Polyline PolyHoleBoundary = null;
                if (RequestAllowMarginError)
                {
                    var offseted = polyHole.Boundary.SmartOffset(Margin);
                    if (offseted.Any())
                    {
                        PolyHoleBoundary = offseted.First();
                        offseted.Skip(1).ForEach(el => el.Dispose());
                    }
                    else
                    {
                        PolyHoleBoundary = polyHole.Boundary.Clone() as Polyline;
                    }
                }
                else
                {
                    PolyHoleBoundary = polyHole.Boundary.Clone() as Polyline;
                }

                using (PolyHoleBoundary)
                {
                    List<Polyline> HoleUnionResultList = HoleUnionResult.ToList();
                    for (int i = 0; i < HoleUnionResultList.Count; i++)
                    {
                        Polyline ParsedHole = HoleUnionResultList[i];
                        if (RequestAllowMarginError)
                        {
                            var OffsetParsedHole = ParsedHole.SmartOffset(-Margin).ToList();
                            if (OffsetParsedHole.Count > 0)
                            {
                                ParsedHole = OffsetParsedHole.First();
                                OffsetParsedHole.Remove(ParsedHole);
                                OffsetParsedHole.DeepDispose();
                            }
                        }
                        if (PolyHoleBoundary.IsDisposed || ParsedHole.IsDisposed)
                        {
                            continue;
                        }
                        if (ParsedHole.IsSegmentIntersecting(PolyHoleBoundary, out Point3dCollection _, Intersect.OnBothOperands) || ParsedHole.IsInside(polyHole.Boundary, false))
                        {
                            HoleUnionResult.Remove(HoleUnionResultList[i]);
                            if (Substraction(new PolyHole(ParsedHole, null), new Polyline[] { PolyHoleBoundary }, out var SubResult))
                            {
                                foreach (var item in SubResult.GetBoundaries())
                                {
                                    HoleUnionResult.AddRange(item.SmartOffset(Margin));
                                }
                                SubResult.DeepDispose();
                            }
                        }
                        ParsedHole.Dispose();
                    }
                    HoleUnionResultList.RemoveCommun(HoleUnionResult).DeepDispose();
                }
            }

            //Remove part that is leaving inside 2 polygon, they will be calculated after. 
            foreach (var Hole in HoleUnionResult.ToList())
            {
                int MaxNumberOfContainPolygon = 2;
                foreach (var polyHole in PolyHoleList.GetBoundaries())
                {
                    if (Hole?.IsDisposed != false)
                    {
                        continue;
                    }
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
                            Intersection(new PolyHole(HoleA, null), new PolyHole(HoleB, null), out var IntersectResult);
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
            if (polyHole.Boundary.Area <= Generic.MediumTolerance.EqualPoint)
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
                Generic.WriteMessage($"Impossible de merger les courbes (erreur lors de l'offset des contours). Offset value : {OffsetDistance}.");
                return polyHoles;
                throw new System.Exception("Impossible de merger les courbes (erreur lors de l'offset des contours).");
            }

            polyHole.Boundary.Dispose();

            if (OffsetCurve.Count == 1)
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
                    var OnLineIntersectionPointsFounds = new Point3dCollection(GlobalIntersectionPointsFounds.ToArray());
                    foreach (Point3d item in GlobalIntersectionPointsFounds)
                    {
                        var newPt = PolyBase.GetClosestPointTo(item, false);
                        if (!OnLineIntersectionPointsFounds.Contains(newPt))
                        {
                            OnLineIntersectionPointsFounds.Add(newPt);
                        }
                    }

                    var SplitDouble = PolyBase.GetSplitPoints(OnLineIntersectionPointsFounds);
                    var Splitted = PolyBase.TryGetSplitCurves(SplitDouble).Cast<Polyline>().ToHashSet();

                    //Remove zero length line
                    foreach (var curv in Splitted.ToList())
                    {
                        if (curv.Length <= Generic.LowTolerance.EqualPoint)
                        {
                            Splitted.Remove(curv);
                            curv.Dispose();
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
