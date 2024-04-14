using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class HatchsExtensions
    {
        public static bool GetPolyHole(this Hatch Hachure, out PolyHole polyHole)
        {
            polyHole = null;
            if (!Hachure.GetHatchPolylineV2(out List<Curve> ExternalCurves, out List<(Curve curve, HatchLoopTypes looptype)> OtherCurves))
            {
                return false;
            }
            List<Curve> ExternalMergedCurves = ExternalCurves.JoinMerge();
            ExternalCurves.RemoveCommun(ExternalMergedCurves).DeepDispose();
            List<Curve> InnerCurves = OtherCurves.Select(tuple => tuple.curve).ToList();
            if (Hachure.HatchStyle == HatchStyle.Ignore)
            {
                InnerCurves.DeepDispose();
                InnerCurves.Clear();
            }
            List<Curve> InnerMergedCurves = InnerCurves.JoinMerge();
            InnerCurves.RemoveCommun(InnerMergedCurves).DeepDispose();

            if (Hachure is null || ExternalMergedCurves is null || ExternalMergedCurves.Count == 0)
            {
                Generic.WriteMessage("Impossible de découpper cette hachure.");
                return false;
            }
            if (ExternalMergedCurves.Count > 1)
            {
                Generic.WriteMessage("Impossible de découpper une hachure combinée.");
                ExternalMergedCurves.DeepDispose();
                return false;
            }
            var Boundary = ExternalMergedCurves[0].ToPolyline();
            ExternalMergedCurves.DeepDispose();
            polyHole = new PolyHole(Boundary, InnerMergedCurves.Cast<Polyline>());
            return true;
        }




        public static double GetAssociatedBoundary(this Hatch Hachure, out Curve Boundary)
        {
            var objectIdCollection = Hachure.GetAssociatedObjectIds();
            Boundary = null;
            if (objectIdCollection.Count >= 1)
            {
                Curve Curve = objectIdCollection[0].GetNoTransactionDBObject(OpenMode.ForWrite) as Curve;
                Boundary = Curve;
            }
            return objectIdCollection.Count;
        }
        //public static void ReGenerateBoundaryCommand(this Hatch Hachure)
        //{
        //    Generic.Command("_-HATCHEDIT", Hachure.ObjectId, "_Boundary", "_Polyline", "_YES");
        //}


        public static bool GetHatchPolylineV2(this Hatch Hachure, out List<Curve> ExternalCurves, out List<(Curve curve, HatchLoopTypes looptype)> OtherCurves)
        {
            ExternalCurves = new List<Curve>();
            OtherCurves = new List<(Curve curve, HatchLoopTypes looptype)>();

            foreach ((Curve curve, HatchLoopTypes looptype) in GetHatchBoundary(Hachure))
            {
                Hachure.CopyPropertiesTo(curve);
                if (looptype.HasFlag(HatchLoopTypes.External))
                {
                    ExternalCurves.Add(curve);
                }
                else
                {
                    OtherCurves.Add((curve, looptype));
                }
            }
            return true;
        }

        private static List<(Curve, HatchLoopTypes)> GetHatchBoundary(Hatch hatch)
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


        public static Hatch HatchRegion(this Region region, Transaction tr, bool Associative = true)
        {
            // Create a hatch and set its properties
            Hatch hatch = new Hatch();
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
