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

        public static bool GetHatchPolyline(this Hatch Hachure, out Polyline Polyline)
        {
            Polyline = null;

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!Hachure.Associative)
                {
                    Hachure.ReGenerateBoundaryCommand();
                    Hachure.GetAssociatedBoundary(out Polyline);
                    Hachure.CopyPropertiesTo(Polyline);
                }
                else
                {

                    var NumberOfBoundary = Hachure.GetAssociatedBoundary(out Polyline BaseBoundary);
                    if (NumberOfBoundary > 1)
                    {
                        Hachure.ReGenerateBoundaryCommand();
                        double NewNumberOfBoundary = Hachure.GetAssociatedBoundary(out Polyline);
                        if (NewNumberOfBoundary > 1)
                        {
                            var objectIdCollection = Hachure.GetAssociatedObjectIds();
                            Polyline = null;
                            foreach (ObjectId BoundaryElementObjectId in objectIdCollection)
                            {
                                var BoundaryElementEntity = BoundaryElementObjectId.GetEntity() as Polyline;
                                if (Polyline == null)
                                {
                                    Polyline = BoundaryElementEntity;
                                }
                                else
                                {
                                    Polyline.JoinPolyline(BoundaryElementEntity);
                                }
                            }
                            Polyline.Cleanup();
                        }

                        BaseBoundary.CopyPropertiesTo(Polyline);
                    }
                    else
                    {
                        Polyline = BaseBoundary;
                    }
                }
                tr.Commit();

                if (Polyline is null)
                {
                    return false;
                }

            }
            return true;
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
