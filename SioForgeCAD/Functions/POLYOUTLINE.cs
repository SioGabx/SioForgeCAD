using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    internal class POLYOUTLINE
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
                    ed.WriteMessage("\nLa polyligne sélectionnée n'a pas d'épaisseur globale. Annulation.");
                    return;
                }

                double halfWidth = sourcePoly.ConstantWidth / 2.0;
                bool isClosed = sourcePoly.Closed;

                int numSegments = sourcePoly.NumberOfVertices - (isClosed ? 0 : 1);
                int numVertices = isClosed ? numSegments : numSegments + 1;

                Curve2d[] leftSegments = new Curve2d[numSegments];
                Curve2d[] rightSegments = new Curve2d[numSegments];

                // 2. Récupérer les segments géométriques mathématiques et les décaler
                for (int i = 0; i < numSegments; i++)
                {
                    SegmentType type = sourcePoly.GetSegmentType(i);

                    if (type == SegmentType.Line)
                    {
                        LineSegment2d lineSeg = sourcePoly.GetLineSegment2dAt(i);
                        Vector2d normal = (lineSeg.EndPoint - lineSeg.StartPoint).GetNormal().GetPerpendicularVector();

                        leftSegments[i] = new LineSegment2d(lineSeg.StartPoint + normal * halfWidth, lineSeg.EndPoint + normal * halfWidth);
                        rightSegments[i] = new LineSegment2d(lineSeg.StartPoint - normal * halfWidth, lineSeg.EndPoint - normal * halfWidth);
                    }
                    else if (type == SegmentType.Arc)
                    {
                        CircularArc2d arcSeg = sourcePoly.GetArcSegment2dAt(i);

                        // FIX CRITIQUE : Les signes d'offset des arcs sont corrigés
                        double offsetLeft = arcSeg.IsClockWise ? halfWidth : -halfWidth;
                        double offsetRight = arcSeg.IsClockWise ? -halfWidth : halfWidth;

                        // FIX CRITIQUE : Empêcher le rayon de devenir négatif ou nul (ce qui crashe la géométrie)
                        double rLeft = Math.Max(1e-6, arcSeg.Radius + offsetLeft);
                        double rRight = Math.Max(1e-6, arcSeg.Radius + offsetRight);

                        leftSegments[i] = new CircularArc2d(arcSeg.Center, rLeft, arcSeg.StartAngle, arcSeg.EndAngle, arcSeg.ReferenceVector, arcSeg.IsClockWise);
                        rightSegments[i] = new CircularArc2d(arcSeg.Center, rRight, arcSeg.StartAngle, arcSeg.EndAngle, arcSeg.ReferenceVector, arcSeg.IsClockWise);
                    }
                }
                // 3 & 4. Calcul dynamique des sommets et chanfreins (remplace les tableaux par des listes)
                List<Point2d> finalLeftPoints = new List<Point2d>();
                List<double> leftBulges = new List<double>();

                List<Point2d> finalRightPoints = new List<Point2d>();
                List<double> rightBulges = new List<double>();

                for (int i = 0; i < numSegments; i++)
                {
                    Point2d origVertexStart = sourcePoly.GetPoint2dAt(i);
                    Point2d origVertexEnd = sourcePoly.GetPoint2dAt((i + 1) % numVertices);

                    // Traitement Côté Gauche
                    ProcessOffsetSide(i, numSegments, isClosed, leftSegments, origVertexStart, origVertexEnd, finalLeftPoints, leftBulges);

                    // Traitement Côté Droit
                    ProcessOffsetSide(i, numSegments, isClosed, rightSegments, origVertexStart, origVertexEnd, finalRightPoints, rightBulges);
                }

                if (!isClosed)
                {
                    finalLeftPoints.Add(leftSegments[numSegments - 1].EndPoint);
                    finalRightPoints.Add(rightSegments[numSegments - 1].EndPoint);
                }

                // 5. Dessin du/des contour(s)
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                if (!isClosed)
                {
                    Polyline outline = new Polyline();
                    int vertexIndex = 0;

                    // Côté gauche
                    for (int j = 0; j < finalLeftPoints.Count; j++)
                    {
                        double b = (j < leftBulges.Count) ? leftBulges[j] : 0.0;
                        outline.AddVertexAt(vertexIndex++, finalLeftPoints[j], b, 0, 0);
                    }

                    // Côté droit (inversé)
                    for (int j = finalRightPoints.Count - 1; j >= 0; j--)
                    {
                        double b = (j > 0) ? -rightBulges[j - 1] : 0.0;
                        outline.AddVertexAt(vertexIndex++, finalRightPoints[j], b, 0, 0);
                    }

                    outline.Closed = true;
                    btr.AppendEntity(outline);
                    tr.AddNewlyCreatedDBObject(outline, true);
                }
                else
                {
                    Polyline polyLeft = new Polyline();
                    Polyline polyRight = new Polyline();

                    for (int j = 0; j < finalLeftPoints.Count; j++)
                        polyLeft.AddVertexAt(j, finalLeftPoints[j], leftBulges[j], 0, 0);

                    for (int j = 0; j < finalRightPoints.Count; j++)
                        polyRight.AddVertexAt(j, finalRightPoints[j], rightBulges[j], 0, 0);

                    polyLeft.Closed = true;
                    polyRight.Closed = true;

                    btr.AppendEntity(polyLeft);
                    tr.AddNewlyCreatedDBObject(polyLeft, true);
                    btr.AppendEntity(polyRight);
                    tr.AddNewlyCreatedDBObject(polyRight, true);
                }

                tr.Commit();
                ed.WriteMessage("\nCommande POLYOUTLINE terminée. Contour généré avec succès !");
            }
        }

        // --- MÉTHODES UTILITAIRES MATHÉMATIQUES ---
        private static void ProcessOffsetSide(
            int i, int numSegments, bool isClosed, Curve2d[] segments,
            Point2d origVertexStart, Point2d origVertexEnd,
            List<Point2d> points, List<double> bulges)
        {
            Curve2d currSeg = segments[i];
            Curve2d prevSeg = (i == 0 && !isClosed) ? null : segments[(i == 0) ? numSegments - 1 : i - 1];
            Curve2d nextSeg = (i == numSegments - 1 && !isClosed) ? null : segments[(i == numSegments - 1) ? 0 : i + 1];

            // 1. Point de départ exact du segment
            Point2d startPt;
            if (prevSeg == null)
            {
                startPt = currSeg.StartPoint;
            }
            else
            {
                Point2d? interPrev = GetExactIntersection(prevSeg, currSeg, origVertexStart);
                startPt = interPrev ?? currSeg.StartPoint;
            }

            // 2. Point de fin exact du segment
            Point2d endPt;
            if (nextSeg == null)
            {
                endPt = currSeg.EndPoint;
            }
            else
            {
                Point2d? interNext = GetExactIntersection(currSeg, nextSeg, origVertexEnd);
                endPt = interNext ?? currSeg.EndPoint;
            }

            // 3. Ajout du point et calcul du bulge standard
            points.Add(startPt);
            double bulge = 0.0;
            if (currSeg is CircularArc2d arc)
            {
                bulge = GetRecalculatedBulge(arc, startPt, endPt);
            }
            bulges.Add(bulge);

            // 4. FIX CRITIQUE : SI AUCUNE INTERSECTION (Création du Chanfrein)
            if (nextSeg != null)
            {
                Point2d? interNext = GetExactIntersection(currSeg, nextSeg, origVertexEnd);
                if (!interNext.HasValue)
                {
                    // On ajoute le bout flottant du segment actuel comme nouveau sommet.
                    // On lui force un Bulge de 0.0, ce qui crée une belle ligne droite 
                    // jusqu'au début du segment suivant !
                    points.Add(endPt);
                    endPt.AddToDrawing();

                    bulges.Add(0.0);
                }
            }
        }

        private static Point2d? GetExactIntersection(Curve2d seg1, Curve2d seg2, Point2d originalVertex)
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

                for (int i = 1; i < numInters; i++)
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

            // Retourne null au lieu de forcer une moyenne : 
            // cela signale au système qu'il doit tirer un chanfrein droit.
            return null;
        }
    
        private static double GetRecalculatedBulge(CircularArc2d arc, Point2d newStart, Point2d newEnd)
        {
            // 1. Calcul des angles absolus par rapport au centre pour les NOUVEAUX points
            double startAngle = (newStart - arc.Center).Angle;
            double endAngle = (newEnd - arc.Center).Angle;

            double sweepAngle;

            // 2. On force STRICTEMENT le même sens de rotation que l'arc d'origine
            if (!arc.IsClockWise)
            {
                // Anti-horaire (CCW) -> L'angle de balayage DOIT être positif
                sweepAngle = endAngle - startAngle;
                if (sweepAngle < 0) sweepAngle += 2 * Math.PI;
            }
            else
            {
                // Horaire (CW) -> L'angle de balayage DOIT être négatif
                sweepAngle = endAngle - startAngle;
                if (sweepAngle > 0) sweepAngle -= 2 * Math.PI;
            }

            // 3. Calcul de l'angle balayé d'ORIGINE pour comparer
            double origStartAngle = (arc.StartPoint - arc.Center).Angle;
            double origEndAngle = (arc.EndPoint - arc.Center).Angle;
            double origSweepAngle;

            if (!arc.IsClockWise)
            {
                origSweepAngle = origEndAngle - origStartAngle;
                if (origSweepAngle < 0) origSweepAngle += 2 * Math.PI;
            }
            else
            {
                origSweepAngle = origEndAngle - origStartAngle;
                if (origSweepAngle > 0) origSweepAngle -= 2 * Math.PI;
            }

            // 4. DÉTECTION D'EFFONDREMENT (Croisement des points)
            // Si l'arc d'origine était mineur (<= 180°) et que le nouveau devient majeur (> 180°)
            if (Math.Abs(origSweepAngle) <= Math.PI + 1e-6 && Math.Abs(sweepAngle) > Math.PI + 1e-6)
            {
                // Les points se sont croisés. L'arc n'a plus la place d'exister.
                // On retourne 0.0 pour en faire une ligne droite (chanfrein propre)
                // au lieu de générer un bulge géant ou de l'inverser.
                return 0.0;
            }

            // 5. Calcul du Bulge 
            // Grâce à l'étape 2, le signe sera TOUJOURS correct (Positif pour CCW, Négatif pour CW)
            return Math.Tan(sweepAngle / 4.0);
        }
    }
}