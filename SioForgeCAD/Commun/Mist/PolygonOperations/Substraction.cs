using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static bool Substraction(this PolyHole BasePolygon, IEnumerable<Polyline> SubstractionPolygonsArg, out List<PolyHole> UnionResult)
        {
            List<Curve> NewBoundaryHoles = new List<Curve>();
            List<Polyline> CuttedPolyline = new List<Polyline>() { BasePolygon.Boundary };

            var SubstractionPolygons = SubstractionPolygonsArg.ToList();
            SubstractionPolygons.AddRange(BasePolygon.Holes);
            foreach (Curve SubstractionPolygonCurve in SubstractionPolygons.ToArray())
            {
                using (var SubsPoly = SubstractionPolygonCurve.ToPolyline())
                {
                    if (SubsPoly != null)
                    {
                        foreach (Polyline NewBoundary in CuttedPolyline.ToArray())
                        {
                            if (NewBoundary.IsSegmentIntersecting(SubsPoly, out _, Intersect.OnBothOperands))
                            {
                                var Cuts = NewBoundary.Slice(SubsPoly);
                                if (Cuts.Count > 0)
                                {
                                    CuttedPolyline.Remove(NewBoundary);
                                }
                                foreach (var CuttedNewBoundary in Cuts)
                                {
                                    if (CuttedNewBoundary.GetInnerCentroid().IsInsidePolyline(SubsPoly))
                                    {
                                        continue;
                                    }
                                    CuttedPolyline.Add(CuttedNewBoundary);
                                }
                                Cuts.RemoveCommun(CuttedPolyline).DeepDispose();
                            }
                            else
                            {
                                if (SubsPoly.IsInside(NewBoundary, false))
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
