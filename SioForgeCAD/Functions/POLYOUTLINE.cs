using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class POLYOUTLINE
    {
        public static void CreatePolyOutline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. Sélection de la polyligne
            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez une polyligne avec une épaisseur globale : ");
            peo.SetRejectMessage("\nVeuillez sélectionner une LwPolyline.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline sourcePoly = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;

                if (sourcePoly.ConstantWidth <= 0)
                {
                    Generic.WriteMessage("La polyligne sélectionnée n'a pas d'épaisseur globale. Annulation.");
                    return;
                }
                (Curve2d[] leftSegments, Curve2d[] rightSegments) = GetOffsetCurves(sourcePoly);

                var AjustedleftSegments = TrimAndExtendOffsetCurves(leftSegments, sourcePoly);
                var AjustedrightSegments = TrimAndExtendOffsetCurves(rightSegments, sourcePoly);

                var Polys = GetPolyline(sourcePoly, AjustedleftSegments, AjustedrightSegments);

                //AjustedleftSegments.ForEach(t => t.ConvertToCurve().AddToDrawing(5));
                //AjustedrightSegments.ForEach(t => t.ConvertToCurve().AddToDrawing(5));

                //leftSegments.ForEach(t => t.ConvertToCurve().AddToDrawing(4));
                //rightSegments.ForEach(t => t.ConvertToCurve().AddToDrawing(4));


                Polys.left?.AddToDrawing();
                Polys.right?.AddToDrawing();


                tr.Commit();
            }
        }


        public static (Curve2d[] leftSegments, Curve2d[] rightSegments) GetOffsetCurves(Polyline sourcePoly)
        {

            double halfWidth = sourcePoly.ConstantWidth / 2.0;
            bool isClosed = sourcePoly.Closed;

            int numSegments = sourcePoly.NumberOfVertices - (isClosed ? 0 : 1);
            int numVertices = isClosed ? numSegments : numSegments + 1;

            Curve2d[] leftSegments = new Curve2d[numSegments];
            Curve2d[] rightSegments = new Curve2d[numSegments];

            for (int i = 0; i < numSegments; i++)
            {
                SegmentType type = sourcePoly.GetSegmentType(i);

                if (type == SegmentType.Line)
                {
                    LineSegment2d lineSeg = sourcePoly.GetLineSegment2dAt(i);
                    Vector2d normal = (lineSeg.EndPoint - lineSeg.StartPoint).GetNormal().GetPerpendicularVector();

                    leftSegments[i] = new LineSegment2d(lineSeg.StartPoint + (normal * halfWidth), lineSeg.EndPoint + (normal * halfWidth));
                    rightSegments[i] = new LineSegment2d(lineSeg.StartPoint - (normal * halfWidth), lineSeg.EndPoint - (normal * halfWidth));
                }
                else if (type == SegmentType.Arc)
                {
                    CircularArc2d arcSeg = sourcePoly.GetArcSegment2dAt(i);

                    double offsetLeft = arcSeg.IsClockWise ? halfWidth : -halfWidth;
                    double offsetRight = arcSeg.IsClockWise ? -halfWidth : halfWidth;

                    double rLeft = Math.Max(1e-6, arcSeg.Radius + offsetLeft);
                    double rRight = Math.Max(1e-6, arcSeg.Radius + offsetRight);

                    leftSegments[i] = new CircularArc2d(arcSeg.Center, rLeft, arcSeg.StartAngle, arcSeg.EndAngle, arcSeg.ReferenceVector, arcSeg.IsClockWise);
                    rightSegments[i] = new CircularArc2d(arcSeg.Center, rRight, arcSeg.StartAngle, arcSeg.EndAngle, arcSeg.ReferenceVector, arcSeg.IsClockWise);
                }
            }
            return (leftSegments, rightSegments);
        }

        public static List<Curve2d> TrimAndExtendOffsetCurves(Curve2d[] curves, Polyline sourcePoly)
        {
            List<(Curve2d Original, Curve2d Ajusted)> TrimAndExtendCurves = new List<(Curve2d, Curve2d)>();
            curves.ForEach(t => TrimAndExtendCurves.Add((t, t.Clone() as Curve2d)));

            if (curves == null || curves.Length < 2) return TrimAndExtendCurves.ConvertAll(t => t.Ajusted);

            int StartIndex = sourcePoly.Closed ? -1 : 0;
            int EndIndex = sourcePoly.Closed ? -1 : curves.Length - 1;
            for (int i = 0; i < curves.Length; i++)
            {
                Curve2d c1 = curves[i % curves.Length];
                Curve2d c2 = curves[(i + 1) % curves.Length];

                Point2d origVertexStart = sourcePoly.GetPoint2dAt(i);
                Point2d origVertexEnd = sourcePoly.GetPoint2dAt((i + 1) % curves.Length);
                var FinalPt = GetExactIntersection2(c1, c2, origVertexEnd);

                if (FinalPt.HasValue && FinalPt is Point2d ExtendedPt)
                {

                    if (c1 != (sourcePoly.Closed ? null : curves[curves.Length - 1])) //Avoid extending if the poly is not closed the start of the poly to the end point
                    {
                        AdjustCurveEnd(GetCurveFromList(TrimAndExtendCurves, c1).Ajusted, ExtendedPt);
                    }
                    if (c2 != (sourcePoly.Closed ? null : curves[0]))//Avoid extending if the poly is not closed the end of the poly to the start point
                    {
                        AdjustCurveStart(GetCurveFromList(TrimAndExtendCurves, c2).Ajusted, ExtendedPt);
                    }
                }
                else
                {
                    if (c1 != (sourcePoly.Closed ? null : curves[curves.Length - 1]) &&
                            c2 != (sourcePoly.Closed ? null : curves[0])
                            )
                    {

                        var Values = GetCurveFromList(TrimAndExtendCurves, c1);
                        var index = TrimAndExtendCurves.IndexOf(Values) + 1;

                        if (c1 is LineSegment2d && c2 is CircularArc2d)
                        {
                            var CloseLineSeg = new LineSegment2d(origVertexEnd, c2.StartPoint);
                            if (GetExactIntersection2(CloseLineSeg, c1, origVertexEnd) is Point2d LineSegmentPt)
                            {
                                AdjustCurveEnd(GetCurveFromList(TrimAndExtendCurves, c1).Ajusted, LineSegmentPt);
                                AdjustCurveStart(CloseLineSeg, LineSegmentPt);
                                TrimAndExtendCurves.Insert(index, (CloseLineSeg, CloseLineSeg));
                            }
                        }
                        else if (c1 is CircularArc2d && c2 is LineSegment2d)
                        {
                            var CloseLineSeg = new LineSegment2d(c1.EndPoint, origVertexEnd);
                            if (GetExactIntersection2(CloseLineSeg, c2, origVertexEnd) is Point2d LineSegmentPt)
                            {
                                AdjustCurveStart(GetCurveFromList(TrimAndExtendCurves, c2).Ajusted, LineSegmentPt);
                                AdjustCurveEnd(CloseLineSeg, LineSegmentPt);
                                TrimAndExtendCurves.Insert(index, (CloseLineSeg, CloseLineSeg));
                            }
                        }
                        else if (c1 is CircularArc2d && c2 is CircularArc2d)
                        {

                            var CloseLineSeg1 = new LineSegment2d(c1.EndPoint, origVertexEnd);
                            TrimAndExtendCurves.Insert(index, (CloseLineSeg1, CloseLineSeg1));
                            var CloseLineSeg2 = new LineSegment2d(origVertexEnd, c2.StartPoint);
                            TrimAndExtendCurves.Insert(index + 1, (CloseLineSeg2, CloseLineSeg2));
                        }
                    }
                }
            }

            return TrimAndExtendCurves.ConvertAll(t => t.Ajusted);
        }



        private static (Polyline left, Polyline right) GetPolyline(Polyline Original, List<Curve2d> AjustedLeftSegments, List<Curve2d> AjustedRightSegments)
        {
            if (!Original.Closed)
            {
                List<Curve2d> fullContour = new List<Curve2d>();
                fullContour.AddRange(AjustedLeftSegments);
                fullContour.Add(new LineSegment2d(AjustedLeftSegments.Last().EndPoint, AjustedRightSegments.Last().EndPoint));
                var reversedRight = AjustedRightSegments.Select(s => s.Reverse()).Reverse().ToList();

                fullContour.AddRange(reversedRight);
                fullContour.Add(new LineSegment2d(reversedRight.Last().EndPoint, AjustedLeftSegments.First().StartPoint));
                return (CreatePolylineFromSegments(fullContour, true), null);
            }
            else
            {
                Polyline polyLeft = CreatePolylineFromSegments(AjustedLeftSegments, true);
                Polyline polyRight = CreatePolylineFromSegments(AjustedRightSegments, true);
                return (polyLeft, polyRight);
            }
        }


        private static Polyline CreatePolylineFromSegments(List<Curve2d> segments, bool closed)
        {
            Polyline pl = new Polyline();

            for (int i = 0; i < segments.Count; i++)
            {
                Curve2d segment = segments[i];
                double bulge = 0;

                // Si le segment est un arc, on calcule le "bulge"
                if (segment is CircularArc2d arc)
                {
                    // Formule du bulge : tan(angle_total / 4)
                    // Attention au sens de l'arc (Clockwise vs CounterClockwise)
                    double angle = arc.IsClockWise ? -(arc.EndAngle - arc.StartAngle) : (arc.EndAngle - arc.StartAngle);
                    bulge = Math.Tan(angle / 4);
                }

                // On ajoute le point de départ du segment
                pl.AddVertexAt(i, segment.StartPoint, bulge, 0, 0);

                // Si c'est le dernier segment et que la courbe est ouverte, 
                // on n'oublie pas d'ajouter le point final.
                if (i == segments.Count - 1 && !closed)
                {
                    pl.AddVertexAt(i + 1, segment.EndPoint, 0, 0, 0);
                }
            }

            pl.Closed = closed;
            return pl;
        }


        private static (Curve2d Original, Curve2d Ajusted) GetCurveFromList(List<(Curve2d Original, Curve2d Ajusted)> values, Curve2d OToFind)
        {
            return values.FirstOrDefault(c => c.Original == OToFind);
        }

        private static Point2d? GetExactIntersection2(Curve2d seg1, Curve2d seg2, Point2d originalVertex)
        {
            Curve2d unb1 = seg1 is LineSegment2d ls1 ? (Curve2d)new Line2d(ls1.StartPoint, ls1.Direction) :
                           seg1 is CircularArc2d ca1 ? new CircularArc2d(ca1.Center, ca1.Radius, 0, Math.PI * 2, ca1.ReferenceVector, false) : seg1;

            Curve2d unb2 = seg2 is LineSegment2d ls2 ? (Curve2d)new Line2d(ls2.StartPoint, ls2.Direction) :
                           seg2 is CircularArc2d ca2 ? new CircularArc2d(ca2.Center, ca2.Radius, 0, Math.PI * 2, ca2.ReferenceVector, false) : seg2;

            CurveCurveIntersector2d intersector = new CurveCurveIntersector2d(unb1, unb2);
            int numInters = intersector.NumberOfIntersectionPoints;

            if (numInters > 0)
            {
                Point2d bestPoint = intersector.GetIntersectionPoint(0);
                double minDistance = bestPoint.GetDistanceTo(originalVertex);

                for (int i = 0; i < numInters; i++)
                {
                    Point2d currentPt = intersector.GetIntersectionPoint(i);

                    double dist = currentPt.GetDistanceTo(originalVertex);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestPoint = currentPt;
                    }
                }
                return bestPoint;
            }
            return null; // no intersect found
        }

        private static void AdjustCurveEnd(Curve2d curve, Point2d pt)
        {
            if (curve is LineSegment2d line)
            {
                line.Set(line.StartPoint, pt);
            }
            else if (curve is CircularArc2d arc)
            {
                Interval interval = arc.GetInterval();

                double dMin = interval.GetBounds()[0];
                double dMax = interval.GetBounds()[1];
                double midParam = (dMin + dMax) / 2.0;

                arc.Set(arc.StartPoint, arc.EvaluatePoint(midParam), pt);

            }
        }

        private static void AdjustCurveStart(Curve2d curve, Point2d pt)
        {
            if (curve is LineSegment2d line)
            {
                line.Set(pt, line.EndPoint);
            }
            else if (curve is CircularArc2d arc)
            {
                Interval interval = arc.GetInterval();
                double dMin = interval.GetBounds()[0];
                double dMax = interval.GetBounds()[1];
                double midParam = (dMin + dMax) / 2.0;

                arc.Set(pt, arc.EvaluatePoint(midParam), arc.EndPoint);
            }
        }
    }

}