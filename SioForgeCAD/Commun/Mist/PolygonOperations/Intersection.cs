using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        public static bool Intersection(PolyHole PolyHoleA, PolyHole PolyHoleB, out List<PolyHole> IntersectionResult)
        {
            IntersectionResult = new List<PolyHole>();
            List<PolyHole> BoundaryIntersectionResult = new List<PolyHole>();

            if (PolyHoleA.Boundary.IsSegmentIntersecting(PolyHoleB.Boundary, out Point3dCollection _, Intersect.OnBothOperands))
            {
                var SliceResult = Slice(PolyHoleA.Boundary, PolyHoleB.Boundary);
                foreach (var item in SliceResult)
                {
                    if (item.GetInnerCentroid().IsInsidePolyline(PolyHoleA.Boundary) && item.GetInnerCentroid().IsInsidePolyline(PolyHoleB.Boundary))
                    {
                        BoundaryIntersectionResult.Add(new PolyHole(item, null));
                    }
                    else
                    {
                        item.Dispose();
                    }
                }
            }
            else
            {
                if (PolyHoleA.Boundary.IsInside(PolyHoleB.Boundary, false))
                {
                    BoundaryIntersectionResult.Add(PolyHoleA);
                }
                else if (PolyHoleB.Boundary.IsInside(PolyHoleA.Boundary, false))
                {
                    BoundaryIntersectionResult.Add(PolyHoleB);
                }
            }

            var PolyHoleHoles = new List<Polyline>();
            PolyHoleHoles.AddRange(PolyHoleA.Holes);
            PolyHoleHoles.AddRange(PolyHoleB.Holes);

            if (PolyHoleHoles.Count == 0)
            {
                //If there is no hole
                IntersectionResult.AddRange(BoundaryIntersectionResult);
                return true;
            }
            //if there is hole, we substract them from the boundary
            foreach (var boundary in BoundaryIntersectionResult.ToList())
            {
                Substraction(boundary, PolyHoleHoles, out var TempIntersectionResult);
                IntersectionResult.AddRange(TempIntersectionResult);
            }
            return true;
        }
    }
}
