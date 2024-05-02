using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static bool Substraction(PolyHole BasePolygon, IEnumerable<Polyline> SubstractionPolygonsArg, out List<PolyHole> UnionResult)
        {
            List<Curve> NewBoundaryHoles = new List<Curve>();
            List<Polyline> CuttedPolyline = new List<Polyline>() { BasePolygon.Boundary };

            //Add existing hole to the substraction if not present
            var SubstractionPolygons = SubstractionPolygonsArg.AddRangeUnique(BasePolygon.Holes);

            foreach (Curve SubstractionPolygonCurve in SubstractionPolygons.ToArray())
            {
                if (SubstractionPolygonCurve?.IsDisposed == true)
                {
                    Debug.WriteLine("Error : SubstractionPolygonCurve was null or disposed");
                    continue;
                }
                using (var SimplifiedSubstractionPolygonCurve = SubstractionPolygonCurve.ToPolyline())
                {
                    if (SimplifiedSubstractionPolygonCurve != null)
                    {
                        foreach (Polyline NewBoundary in CuttedPolyline.ToArray())
                        {
                            if (NewBoundary.IsSegmentIntersecting(SimplifiedSubstractionPolygonCurve, out var _, Intersect.OnBothOperands))
                            {
                                //pts.AddToDrawing(5);
                                var Cuts = PolygonOperation.Slice(NewBoundary, SimplifiedSubstractionPolygonCurve);
                                //if the boundary was cuted 
                                if (Cuts.Count > 0)
                                {
                                    CuttedPolyline.Remove(NewBoundary);
                                    NewBoundary.Dispose();
                                }
                                foreach (var CuttedNewBoundary in Cuts)
                                {
                                    //If cutted is inside a substraction polygon, we ignore it,
                                    //we check if Cuts.Count > 1, if is inside and Cuts.Count == 1, mean that IsSegmentIntersecting have false result
                                    if (CuttedNewBoundary.GetInnerCentroid().IsInsidePolyline(SimplifiedSubstractionPolygonCurve) && Cuts.Count > 1)
                                    {
                                        continue;
                                    }
                                    CuttedPolyline.Add(CuttedNewBoundary);
                                }
                                Cuts.RemoveCommun(CuttedPolyline).DeepDispose();
                            }
                            else
                            {
                                //If the substraction is not cutting the edge, then the subs is inside hole
                                if (SimplifiedSubstractionPolygonCurve.IsInside(NewBoundary, false))
                                {
                                    NewBoundaryHoles.Add(SubstractionPolygonCurve);
                                }
                            }
                        }
                    }
                }
            }

            //Merge overlaping hole polyline
            Union(PolyHole.CreateFromList(NewBoundaryHoles.Cast<Polyline>()), out var HoleUnionResult);
            NewBoundaryHoles.RemoveCommun(SubstractionPolygonsArg).RemoveCommun(BasePolygon.Holes).DeepDispose();
            UnionResult = PolyHole.CreateFromList(CuttedPolyline, HoleUnionResult.GetBoundaries());
            return true;
        }
    }
}
