using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class RegionsExtensions
    {
        /// <summary>
        /// Gets the curves constituting the boundaries of the region.
        /// https://www.theswamp.org/index.php?topic=31865.30
        /// </summary>
        /// <param name="region">The region this method applies to.</param>
        /// <returns>Curve collection.</returns>
        public static IEnumerable<Curve> GetCurves(this Region region)
        {
            using (var brep = new Brep(region))
            {
                var loops = brep.Complexes
                    .SelectMany(complex => complex.Shells)
                    .SelectMany(shell => shell.Faces)
                    .SelectMany(face => face.Loops);
                foreach (var loop in loops)
                {
                    var curves3d = loop.Edges.Select(edge => ((ExternalCurve3d)edge.Curve).NativeCurve);
                    if (1 < curves3d.Count())
                    {
                        if (curves3d.All(curve3d => curve3d is CircularArc3d || curve3d is LineSegment3d))
                        {
                            var pline = (Polyline)Curve.CreateFromGeCurve(new CompositeCurve3d(curves3d.ToOrderedArray()));
                            pline.Closed = true;
                            yield return pline;
                        }
                        else
                        {
                            foreach (Curve3d curve3d in curves3d)
                            {
                                yield return Curve.CreateFromGeCurve(curve3d);
                            }
                        }
                    }
                    else
                    {
                        yield return Curve.CreateFromGeCurve(curves3d.First());
                    }
                }
            }
        }
    }
}
