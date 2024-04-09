using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static bool Substraction(this PolyHole BasePolygon, IEnumerable<Polyline> SubstractionPolygonsArg, out List<PolyHole> UnionResult)
        {
            List<Curve> NewBoundaryHoles = new List<Curve>();
            List<Polyline> CuttedPolyline = new List<Polyline>() { BasePolygon.Boundary };

            //Add existing hole to the substraction if not present
            var SubstractionPolygons = SubstractionPolygonsArg.ToList();
            SubstractionPolygons.AddRangeUnique(BasePolygon.Holes);
         
            foreach (Curve SubstractionPolygonCurve in SubstractionPolygons.ToArray())
            {
                using (var SimplifiedSubstractionPolygonCurve = SubstractionPolygonCurve.ToPolyline())
                {
                    if (SimplifiedSubstractionPolygonCurve != null)
                    {
                        foreach (Polyline NewBoundary in CuttedPolyline.ToArray())
                        {
                            if (NewBoundary.IsSegmentIntersecting(SimplifiedSubstractionPolygonCurve, out _, Intersect.OnBothOperands))
                            {
                                var Cuts = NewBoundary.Slice(SimplifiedSubstractionPolygonCurve);
                                //if the boundary was cuted 
                                if (Cuts.Count > 0)
                                {
                                    CuttedPolyline.Remove(NewBoundary);
                                }
                                foreach (var CuttedNewBoundary in Cuts)
                                {
                                    //If cutted is inside a substraction polygon, we ignore it
                                    if (CuttedNewBoundary.GetInnerCentroid().IsInsidePolyline(SimplifiedSubstractionPolygonCurve))
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
            UnionResult = PolyHole.CreateFromList(CuttedPolyline, HoleUnionResult.GetBoundaries());
            return true;
        }

    }
}
