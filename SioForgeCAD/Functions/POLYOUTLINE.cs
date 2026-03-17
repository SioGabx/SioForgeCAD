using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;

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

                Point2d[] finalLeftPoints = new Point2d[numVertices];
                Point2d[] finalRightPoints = new Point2d[numVertices];

                // 3. Calcul mathématique des VRAIS sommets par intersection infinie
                for (int i = 0; i < numVertices; i++)
                {
                    Point2d originalVertex = sourcePoly.GetPoint2dAt(i);

                    if (!isClosed && i == 0)
                    {
                        finalLeftPoints[i] = leftSegments[0].StartPoint;
                        finalRightPoints[i] = rightSegments[0].StartPoint;
                    }
                    else if (!isClosed && i == numVertices - 1)
                    {
                        finalLeftPoints[i] = leftSegments[numSegments - 1].EndPoint;
                        finalRightPoints[i] = rightSegments[numSegments - 1].EndPoint;
                    }
                    else
                    {
                        int prevIndex = (i == 0) ? numSegments - 1 : i - 1;
                        int currIndex = i;

                        finalLeftPoints[i] = GetOffsetIntersection(leftSegments[prevIndex], leftSegments[currIndex], originalVertex);
                        finalRightPoints[i] = GetOffsetIntersection(rightSegments[prevIndex], rightSegments[currIndex], originalVertex);
                    }
                }

                double[] leftBulges = new double[numSegments];
                double[] rightBulges = new double[numSegments];

                // 4. Calcul des nouveaux Bulges pour les arcs modifiés
                for (int i = 0; i < numSegments; i++)
                {
                    Point2d leftStart = finalLeftPoints[i];
                    Point2d leftEnd = finalLeftPoints[(i + 1) % numVertices];

                    Point2d rightStart = finalRightPoints[i];
                    Point2d rightEnd = finalRightPoints[(i + 1) % numVertices];

                    if (leftSegments[i] is CircularArc2d arcLeft)
                        leftBulges[i] = GetRecalculatedBulge(arcLeft, leftStart, leftEnd);
                    else
                        leftBulges[i] = 0.0;

                    if (rightSegments[i] is CircularArc2d arcRight)
                        rightBulges[i] = GetRecalculatedBulge(arcRight, rightStart, rightEnd);
                    else
                        rightBulges[i] = 0.0;
                }

                // 5. Dessin du/des contour(s)
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                if (!isClosed)
                {
                    Polyline outline = new Polyline();
                    int vertexIndex = 0;

                    // Côté gauche (du début à la fin)
                    for (int i = 0; i < numVertices; i++)
                    {
                        double b = (i < numSegments) ? leftBulges[i] : 0.0;
                        outline.AddVertexAt(vertexIndex++, finalLeftPoints[i], b, 0, 0);
                    }

                    // Côté droit (de la fin au début pour fermer la boucle)
                    for (int i = numVertices - 1; i >= 0; i--)
                    {
                        // Le bulge est inversé et correspond au segment précédent quand on recule
                        double b = (i > 0) ? -rightBulges[i - 1] : 0.0;
                        outline.AddVertexAt(vertexIndex++, finalRightPoints[i], b, 0, 0);
                    }

                    outline.Closed = true;
                    btr.AppendEntity(outline);
                    tr.AddNewlyCreatedDBObject(outline, true);
                }
                else
                {
                    // Polyligne fermée : génère 2 polylignes (Intérieur / Extérieur)
                    Polyline polyLeft = new Polyline();
                    Polyline polyRight = new Polyline();

                    for (int i = 0; i < numVertices; i++)
                    {
                        polyLeft.AddVertexAt(i, finalLeftPoints[i], leftBulges[i], 0, 0);
                        polyRight.AddVertexAt(i, finalRightPoints[i], rightBulges[i], 0, 0);
                    }

                    polyLeft.Closed = true;
                    polyRight.Closed = true;

                    btr.AppendEntity(polyLeft);
                    tr.AddNewlyCreatedDBObject(polyLeft, true);
                    btr.AppendEntity(polyRight);
                    tr.AddNewlyCreatedDBObject(polyRight, true);
                }

                tr.Commit();
                ed.WriteMessage("\nCommande POLYOUTILINE terminée. Contour généré avec succès !");
            }
        }

        // --- MÉTHODES UTILITAIRES MATHÉMATIQUES ---

        private static Point2d GetOffsetIntersection(Curve2d seg1, Curve2d seg2, Point2d originalVertex)
        {
            // Conversion en droite infinie ou cercle complet pour garantir l'intersection
            Curve2d unb1 = seg1 is LineSegment2d ls1 ? (Curve2d)new Line2d(ls1.StartPoint, ls1.Direction) :
                           seg1 is CircularArc2d ca1 ? new CircularArc2d(ca1.Center, ca1.Radius, 0, Math.PI * 2, ca1.ReferenceVector, false) : seg1;

            Curve2d unb2 = seg2 is LineSegment2d ls2 ? (Curve2d)new Line2d(ls2.StartPoint, ls2.Direction) :
                           seg2 is CircularArc2d ca2 ? new CircularArc2d(ca2.Center, ca2.Radius, 0, Math.PI * 2, ca2.ReferenceVector, false) : seg2;

            // Utilisation de la classe mathématique pour intercepter les géométries 2D
            CurveCurveIntersector2d intersector = new CurveCurveIntersector2d(unb1, unb2);
            int numInters = intersector.NumberOfIntersectionPoints;

            if (numInters > 0)
            {
                // Sélection de l'intersection la plus proche de l'angle d'origine
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

            // FIX CRITIQUE : Fallback plus sûr en cas de tolérance tangente ratée (moyenne des deux points)
            return new Point2d((seg1.EndPoint.X + seg2.StartPoint.X) / 2.0, (seg1.EndPoint.Y + seg2.StartPoint.Y) / 2.0);
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