using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Windows;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        private static ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin, List<PolyHole> CuttedBy)> GetSplittedCurves(List<PolyHole> Polylines)
        {
            ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin, List<PolyHole> CuttedBy)> SplittedCurvesOrigin = new ConcurrentBag<(HashSet<Polyline>, Polyline, List<PolyHole>)>();
            foreach (var PolyBase in Polylines)
            {
                Point3dCollection GlobalIntersectionPointsFounds = new Point3dCollection();
                List<PolyHole> CuttedBy = new List<PolyHole>();
                var PolyBaseExtend = PolyBase.Boundary.GetExtents();
                foreach (var PolyCut in Polylines)
                {
                    if (PolyCut == PolyBase) { continue; }
                    if (PolyCut.Boundary.GetExtents().CollideWithOrConnected(PolyBaseExtend))
                    {
                        if (PolyBase.Boundary.IsSegmentIntersecting(PolyCut.Boundary, out Point3dCollection IntersectionPointsFounds, Intersect.OnBothOperands))
                        {
                            CuttedBy.Add(PolyCut);
                        }
                        GlobalIntersectionPointsFounds.AddRange(IntersectionPointsFounds);
                    }
                }

                if (GlobalIntersectionPointsFounds.Count > 0)
                {
                    var SplitDouble = PolyBase.Boundary.GetSplitPoints(GlobalIntersectionPointsFounds);
                    SplittedCurvesOrigin.Add((PolyBase.Boundary.TryGetSplitCurves(SplitDouble).Cast<Polyline>().ToHashSet(), PolyBase.Boundary, CuttedBy));
                }
                else
                {
                    SplittedCurvesOrigin.Add((new HashSet<Polyline>() { PolyBase.Boundary }, PolyBase.Boundary, CuttedBy));
                }
            }
            return SplittedCurvesOrigin;
        }


        public static bool Union(this List<PolyHole> Polylines, out List<PolyHole> UnionResult)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            {
                ConcurrentBag<(HashSet<Polyline> Splitted, Polyline GeometryOrigin, List<PolyHole> CuttedBy)> SplittedCurvesOrigin = GetSplittedCurves(Polylines);
                Generic.WriteMessage("IsSegmentIntersecting + Split en " + sw.ElapsedMilliseconds);

                ConcurrentBag<Polyline> ConcurrentBagGlobalSplittedCurves = new ConcurrentBag<Polyline>();
                ConcurrentDictionary<Polyline, Polyline> NoArcPolygonCache = new ConcurrentDictionary<Polyline, Polyline>();


                Parallel.ForEach(SplittedCurvesOrigin.ToArray(), new ParallelOptions { MaxDegreeOfParallelism = -1 }, SplittedCurveOrigin =>
                {
                    HashSet<Polyline> SplittedCurves = SplittedCurveOrigin.Splitted;

                    foreach (var PolyBase in Polylines)
                    {
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

                var GlobalSplittedCurves = ConcurrentBagGlobalSplittedCurves.ToList();

                foreach (var item in NoArcPolygonCache)
                {
                    if (item.Key != item.Value)
                    {
                        item.Value?.Dispose();
                    }
                }

                Generic.WriteMessage("IsInside en " + sw.ElapsedMilliseconds);
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
                Generic.WriteMessage("IsOverlaping en " + sw.ElapsedMilliseconds);
                UnionResult = PolyHole.CreateFromList(GlobalSplittedCurves.Join().Cast<Polyline>());
            }
            sw.Stop();
            Generic.WriteMessage("Union en " + sw.ElapsedMilliseconds);
            return true;
        }
    }
}
