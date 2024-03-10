using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using SioForgeCAD.Commun.Extensions;
using System.Linq;
using System.Diagnostics;
using System;
using SioForgeCAD.Commun.Drawing;

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


        public static bool GetHatchPolylineV2(this Hatch Hachure, out Polyline Polyline)
        {
            Hatch HatchClone = Hachure.Clone() as Hatch;
            List<Curve> ExternalCurves = new List<Curve>();
            List<(Curve curve, HatchLoopTypes looptype)> OtherCurves = new List<(Curve curve, HatchLoopTypes looptype)>();

             foreach ((Curve curve, HatchLoopTypes looptype) in GetHatchBoundary(HatchClone))
            {
                if (looptype.HasFlag(HatchLoopTypes.External))
                {
                    if (!curve.IsNewObject)
                    {
                        throw new System.Exception("bb");
                    }
                    ExternalCurves.Add(curve as Curve);
                }
                else
                {
                    curve.ColorIndex = 30;
                    OtherCurves.Add((curve, looptype));
                    curve.AddToDrawing();
                }
               
            }
            
            var MergedCurves = ExternalCurves.Join();
            ExternalCurves.RemoveCommun(MergedCurves).DeepDispose();
            MergedCurves.AddToDrawing(5);
            Polyline = null;
            return false;
        }

        public static List<(Curve, HatchLoopTypes)> GetHatchBoundary(Hatch hatch)
        {
            int numberOfLoops = hatch.NumberOfLoops;
            var result = new List<(Curve, HatchLoopTypes)>(numberOfLoops);
            for (int i = 0; i < numberOfLoops; i++)
            {
                var loop = hatch.GetLoopAt(i);
                if (loop.IsPolyline)
                {
                    var bulges = loop.Polyline;
                    var pline = new Polyline(bulges.Count);
                    for (int j = 0; j < bulges.Count; j++)
                    {
                        var vertex = bulges[j];
                        pline.AddVertexAt(j, vertex.Vertex, vertex.Bulge, 0.0, 0.0);
                    }
                    pline.Elevation = hatch.Elevation;
                    pline.Normal = hatch.Normal;
                    result.Add((pline, loop.LoopType));
                }
                else
                {
                    var plane = hatch.GetPlane();
                    var xform = Matrix3d.PlaneToWorld(plane);
                    var curves = loop.Curves;
                    foreach (Curve2d curve in curves)
                    {
                        switch (curve)
                        {
                            case LineSegment2d lineSegment:
                                var line = new Line(
                                    new Point3d(lineSegment.StartPoint.X, lineSegment.StartPoint.Y, 0.0),
                                    new Point3d(lineSegment.EndPoint.X, lineSegment.EndPoint.Y, 0.0));
                                line.TransformBy(xform);
                                result.Add((line, loop.LoopType));
                                break;
                            case CircularArc2d circularArc:
                                if (circularArc.EndPoint.IsEqualTo(circularArc.StartPoint) && circularArc.Radius > 0)
                                {
                                    var Circle = new Circle(circularArc.Center.ToPoint3d(), Vector3d.YAxis, circularArc.Radius);
                                    result.Add((Circle, loop.LoopType));
                                    break;
                                }

                                double startAngle = circularArc.IsClockWise ? -circularArc.EndAngle : circularArc.StartAngle;
                                double endAngle = circularArc.IsClockWise ? -circularArc.StartAngle : circularArc.EndAngle;
                                var arc = new Arc(
                                    new Point3d(circularArc.Center.X, circularArc.Center.Y, 0.0),
                                    circularArc.Radius,
                                    circularArc.ReferenceVector.Angle + startAngle,
                                    circularArc.ReferenceVector.Angle + endAngle);
                                arc.TransformBy(xform);
                                result.Add((arc, loop.LoopType));
                                break;
                            case EllipticalArc2d ellipticalArc:
                                double ratio = ellipticalArc.MinorRadius / ellipticalArc.MajorRadius;
                                double startParam = ellipticalArc.IsClockWise ? -ellipticalArc.EndAngle : ellipticalArc.StartAngle;
                                double endParam = ellipticalArc.IsClockWise ? -ellipticalArc.StartAngle : ellipticalArc.EndAngle;
                                var ellipse = new Ellipse(
                                    new Point3d(ellipticalArc.Center.X, ellipticalArc.Center.Y, 0.0),
                                    Vector3d.ZAxis,
                                    new Vector3d(ellipticalArc.MajorAxis.X, ellipticalArc.MajorAxis.Y, 0.0) * ellipticalArc.MajorRadius,
                                    ratio,
                                    Math.Atan2(Math.Sin(startParam) * ellipticalArc.MinorRadius, Math.Cos(startParam) * ellipticalArc.MajorRadius),
                                    Math.Atan2(Math.Sin(endParam) * ellipticalArc.MinorRadius, Math.Cos(endParam) * ellipticalArc.MajorRadius));
                                ellipse.TransformBy(xform);
                                result.Add((ellipse, loop.LoopType));
                                break;
                            case NurbCurve2d nurbCurve:
                                var points = new Point3dCollection();
                                for (int j = 0; j < nurbCurve.NumControlPoints; j++)
                                {
                                    var pt = nurbCurve.GetControlPointAt(j);
                                    points.Add(new Point3d(pt.X, pt.Y, 0.0));
                                }
                                var knots = new DoubleCollection();
                                for (int k = 0; k < nurbCurve.NumKnots; k++)
                                {
                                    knots.Add(nurbCurve.GetKnotAt(k));
                                }
                                var weights = new DoubleCollection();
                                for (int l = 0; l < nurbCurve.NumWeights; l++)
                                {
                                    weights.Add(nurbCurve.GetWeightAt(l));
                                }
                                var spline = new Spline(nurbCurve.Degree, nurbCurve.IsRational, nurbCurve.IsClosed(), false, points, knots, weights, 0.0, 0.0);
                                spline.TransformBy(xform);
                                result.Add((spline, loop.LoopType));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return result;
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
                                    catch (System.Exception ex)
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





        ///// <summary>
        ///// Converts hatch to polyline.
        ///// </summary>
        ///// <param name="hatch">The hatch.</param>
        ///// <returns>The result polylines.</returns>
        //public static List<Polyline> HatchToPline(Hatch hatch)
        //{
        //    var plines = new List<Polyline>();
        //    int loopCount = hatch.NumberOfLoops;
        //    for (int index = 0; index < loopCount;)
        //    {
        //        if (hatch.GetLoopAt(index).IsPolyline)
        //        {
        //            var loop = hatch.GetLoopAt(index).Polyline;
        //            var p = new Polyline();
        //            int i = 0;
        //            loop.Cast<BulgeVertex>().ForEach(y =>
        //            {
        //                p.AddVertexAt(i, y.Vertex, y.Bulge, 0, 0);
        //                i++;
        //            });
        //            plines.Add(p);
        //            break;
        //        }
        //        else
        //        {
        //            var loop = hatch.GetLoopAt(index).Curves;
        //            var p = new Polyline();
        //            int i = 0;
        //            loop.Cast<Curve2d>().ForEach(y =>
        //            {
        //                p.AddVertexAt(i, y.StartPoint, 0, 0, 0);
        //                i++;
        //                if (y == loop.Cast<Curve2d>().Last())
        //                {
        //                    p.AddVertexAt(i, y.EndPoint, 0, 0, 0);
        //                }
        //            });
        //            plines.Add(p);
        //            break;
        //        }
        //    }
        //    return plines;
        //}



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
