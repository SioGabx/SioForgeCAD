using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        private static HashSet<(HashSet<Polyline> Splitted, Polyline GeometryOrigin, List<PolyHole> CuttedBy)> GetSplittedCurves(List<PolyHole> Polylines)
        {
            HashSet<(HashSet<Polyline> Splitted, Polyline GeometryOrigin, List<PolyHole> CuttedBy)> SplittedCurvesOrigin = new HashSet<(HashSet<Polyline>, Polyline, List<PolyHole>)>();

            foreach (var PolyBase in Polylines)
            {
                Point3dCollection GlobalIntersectionPointsFounds = new Point3dCollection();
                List<PolyHole> CuttedBy = new List<PolyHole>();
                var PolyBaseExtend = PolyBase.Boundary.GetExtents();
                foreach (var PolyCut in Polylines)
                {
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
                HashSet<(HashSet<Polyline> Splitted, Polyline GeometryOrigin, List<PolyHole> CuttedBy)> SplittedCurvesOrigin = GetSplittedCurves(Polylines);


                Generic.WriteMessage("IsSegmentIntersecting + Split en " + sw.ElapsedMilliseconds);

                HashSet<Polyline> GlobalSplittedCurves = new HashSet<Polyline>();
                foreach (var SplittedCurveOrigin in SplittedCurvesOrigin.ToArray())
                {
                    HashSet<Polyline> SplittedCurves = SplittedCurveOrigin.Splitted;
                    var SplittedGeometryOriginExtend = SplittedCurveOrigin.GeometryOrigin.GetExtents();

                    var PolyBaseCollection = SplittedCurveOrigin.CuttedBy;
                    PolyBaseCollection.AddRange(Polylines.RemoveCommun(SplittedCurveOrigin.CuttedBy));

                    foreach (var PolyBase in PolyBaseCollection)
                    {
                        Polyline NoArcPolyBase = null; 
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
                                NoArcPolyBase = PolyBase.Boundary.ToPolygon(Cleanup:false);
                                //NoArcPolyBase.Cleanup();
                            }
                            if (SplittedCurve.IsInside(NoArcPolyBase, false))
                            {
                                SplittedCurves.Remove(SplittedCurve);
                                SplittedCurve.Dispose();
                            }
                        }
                    }
                    GlobalSplittedCurves.UnionWith(SplittedCurves.ToHashSet());
                }
                Generic.WriteMessage("IsInside en " + sw.ElapsedMilliseconds);

                foreach (var SplittedCurveA in GlobalSplittedCurves.ToArray())
                {
                    if (!GlobalSplittedCurves.Contains(SplittedCurveA))
                    {
                        continue;
                    }
                    foreach (var SplittedCurveB in GlobalSplittedCurves.ToArray())
                    {
                        if (SplittedCurveA != SplittedCurveB)
                        {
                            if (SplittedCurveA.IsOverlaping(SplittedCurveB))
                            {
                                GlobalSplittedCurves.Remove(SplittedCurveA);
                                SplittedCurveA.Dispose();
                                break;
                            }
                        }
                    }
                }
                Generic.WriteMessage("IsOverlaping en " + sw.ElapsedMilliseconds);
                UnionResult = PolyHole.CreateFromList(GlobalSplittedCurves);
            }
            sw.Stop();
            Generic.WriteMessage("Union en " + sw.ElapsedMilliseconds);
            return true;
        }
    }
}
