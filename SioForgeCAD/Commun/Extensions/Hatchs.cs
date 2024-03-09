using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using SioForgeCAD.Commun.Extensions;
using System.Linq;
using System.Diagnostics;

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
                        if (BaseBoundary.Closed)
                        {
                            tr.Commit();
                            if (Hachure.HatchStyle != HatchStyle.Ignore)
                            {
                                Generic.WriteMessage("\nImpossible de découper une hachure qui contient des trous");
                                return false;
                            }
                            Generic.WriteMessage("\nAvertissement : La polyligne contient des trous mais ceux ci seront ignorés");
                            Polyline = BaseBoundary;
                            return true;
                        }
                        Hachure.ReGenerateBoundaryCommand();
                        double NewNumberOfBoundary = Hachure.GetAssociatedBoundary(out Polyline);
                        if (NewNumberOfBoundary > 1 && !Polyline.Closed)
                        {
                            var objectIdCollection = Hachure.GetAssociatedObjectIds();
                            Polyline = null;
                            foreach (ObjectId BoundaryElementObjectId in objectIdCollection)
                            {
                                var BoundaryElementEntity = BoundaryElementObjectId.GetEntity(OpenMode.ForRead) as Polyline;
                                if (Polyline == null)
                                {
                                    Polyline = BoundaryElementEntity;
                                }
                                else
                                {
                                    try
                                    {
                                        Polyline.JoinPolyline(BoundaryElementEntity);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex);
                                    }
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



        public static Hatch HatchRegion(this Region region, Transaction tr, bool Associative = true)
        {
            // Create a hatch and set its properties
            Hatch hatch = new Hatch();
            //hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            //hatch.ColorIndex = 1;  // Set your desired color index
            //hatch.Transparency = new Transparency(127);

            // Add the hatch to the modelspace & transaction
            Generic.GetCurrentSpaceBlockTableRecord(tr).AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);

            hatch.Associative = Associative;

            // Add the hatch loops and complete the hatch
            foreach ((HatchLoopTypes loopType, Curve2dCollection edgePtrs, IntegerCollection edgeTypes) item in region.GetLoops())
            {
                hatch.AppendLoop(item.loopType, item.edgePtrs, item.edgeTypes);
            }

            hatch.EvaluateHatch(true);
            return hatch;
        }



























    }
}
