using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class HatchsExtensions
    {
        public static double GetAssociatedBoundary(this Hatch Hachure, out Polyline Boundary)
        {
            var objectIdCollection = Hachure.GetAssociatedObjectIds();
            Boundary = null;
            if (objectIdCollection.Count >= 1)
            {
                Boundary = objectIdCollection[0].GetDBObject(OpenMode.ForWrite) as Polyline;
            }
            return objectIdCollection.Count;
        }
        public static void ReGenerateBoundaryCommand(this Hatch Hachure)
        {
            Generic.Command("_-HATCHEDIT", Hachure.ObjectId, "_Boundary", "_Polyline", "_YES");
        }



        /// <summary>
        /// Converts hatch to polyline.
        /// </summary>
        /// <param name="hatch">The hatch.</param>
        /// <returns>The result polylines.</returns>
        public static List<Polyline> HatchToPline(Hatch hatch)
        {
            var plines = new List<Polyline>();
            int loopCount = hatch.NumberOfLoops;
            for (int index = 0; index < loopCount;)
            {
                if (hatch.GetLoopAt(index).IsPolyline)
                {
                    var loop = hatch.GetLoopAt(index).Polyline;
                    var p = new Polyline();
                    int i = 0;
                    loop.Cast<BulgeVertex>().ForEach(y =>
                    {
                        p.AddVertexAt(i, y.Vertex, y.Bulge, 0, 0);
                        i++;
                    });
                    plines.Add(p);
                    break;
                }
                else
                {
                    var loop = hatch.GetLoopAt(index).Curves;
                    var p = new Polyline();
                    int i = 0;
                    loop.Cast<Curve2d>().ForEach(y =>
                    {
                        p.AddVertexAt(i, y.StartPoint, 0, 0, 0);
                        i++;
                        if (y == loop.Cast<Curve2d>().Last())
                        {
                            p.AddVertexAt(i, y.EndPoint, 0, 0, 0);
                        }
                    });
                    plines.Add(p);
                    break;
                }
            }
            return plines;
        }
    }
}
