using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class DIVIDEPOLYLIGNINEQUALAREAS
    {
        public static void DivideInEqualAreas()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            // 1. Sélection de la polyligne
            PromptEntityOptions peoPoly = new PromptEntityOptions("\nSélectionnez une polyligne fermée : ");
            peoPoly.SetRejectMessage("Veuillez sélectionner une polyligne valide.");
            peoPoly.AddAllowedClass(typeof(Polyline), false);
            PromptEntityResult perPoly = ed.GetEntity(peoPoly);
            if (perPoly.Status != PromptStatus.OK)
            {
                return;
            }

            // 2. Sélection du segment/ligne de direction
            PromptEntityOptions peoLine = new PromptEntityOptions("\nSélectionnez le segment/ligne de direction : ");
            peoLine.SetRejectMessage("Veuillez sélectionner une ligne valide.");
            peoLine.AddAllowedClass(typeof(Line), false); 
            peoLine.AddAllowedClass(typeof(Polyline), false);
            PromptEntityResult perLine = ed.GetEntity(peoLine);
            if (perLine.Status != PromptStatus.OK)
            {
                return;
            }

            // 3. Nombre de parties
            PromptIntegerOptions pio = new PromptIntegerOptions("\nEn combien de parties voulez-vous diviser l'aire ? ")
            {
                LowerLimit = 2,
                UpperLimit = 100,
                DefaultValue = 2,
                UseDefaultValue = true
            };
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK)
            {
                return;
            }

            int numParts = pir.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline poly = perPoly.ObjectId.GetDBObject(OpenMode.ForRead) as Polyline;
                Entity baseLineOrPoly = perLine.ObjectId.GetDBObject(OpenMode.ForRead) as Entity;
                Line baseLine = null;
                if (baseLineOrPoly is Line line)
                {
                    baseLine = line;
                }
                else if (baseLineOrPoly is Polyline polySegment)
                {
                    // Validation : la polyligne doit comporter exactement 2 sommets (1 segment)
                    if (polySegment.NumberOfVertices != 2)
                    {
                        Generic.WriteMessage("Erreur : La polyligne de direction doit contenir exactement 1 segment (2 sommets).");
                        return;
                    }
                    baseLine = polySegment.ToLine();
                }

                if (baseLine == null) return;

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                double totalArea = poly.Area;
                Vector3d lineDir = (baseLine.EndPoint - baseLine.StartPoint).GetNormal();
                Vector3d perpDir = lineDir.CrossProduct(Vector3d.ZAxis).GetNormal();

                Point3d origStart = baseLine.StartPoint;
                Point3d origEnd = baseLine.EndPoint;

                // Emprise de balayage par projection des sommets
                double minOffset = double.MaxValue;
                double maxOffset = double.MinValue;

                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    Point3d pt = poly.GetPoint3dAt(i);
                    double proj = (pt - origStart).DotProduct(perpDir);
                    if (proj < minOffset)
                    {
                        minOffset = proj;
                    }

                    if (proj > maxOffset)
                    {
                        maxOffset = proj;
                    }
                }

                double margin = (maxOffset - minOffset) * 0.05;
                minOffset -= margin;
                maxOffset += margin;

                double areaMin = CalculateHalfSpaceArea(poly, origStart, origEnd, perpDir, lineDir, minOffset);
                double areaMax = CalculateHalfSpaceArea(poly, origStart, origEnd, perpDir, lineDir, maxOffset);
                bool isIncreasing = areaMax > areaMin;

                // Génération des N-1 lignes ajustées
                for (int part = 1; part < numParts; part++)
                {
                    double targetArea = (totalArea / numParts) * part;

                    double low = minOffset;
                    double high = maxOffset;
                    double mid = 0;
                    double currentArea = 0;
                    double tolerance = 1e-5;
                    const int MaxNumberOfIteration = 70;
                    for (int i = 0; i < MaxNumberOfIteration; i++)
                    {
                        mid = (low + high) / 2.0;

                        currentArea = CalculateHalfSpaceArea(poly, origStart, origEnd, perpDir, lineDir, mid);

                        if (Math.Abs(currentArea - targetArea) < tolerance)
                        {
                            break;
                        }

                        if (isIncreasing)
                        {
                            if (currentArea < targetArea)
                            {
                                low = mid;
                            }
                            else
                            {
                                high = mid;
                            }
                        }
                        else
                        {
                            if (currentArea > targetArea)
                            {
                                low = mid;
                            }
                            else
                            {
                                high = mid;
                            }
                        }
                    }

                    // Calcul de la ligne de coupe prolongée virtuellement pour trouver les intersections
                    Point3d pCenterStart = origStart + (perpDir * mid);
                    Point3d pCenterEnd = origEnd + (perpDir * mid);
                    Point3d pExtStart = pCenterStart - (lineDir * 1e5);
                    Point3d pExtEnd = pCenterEnd + (lineDir * 1e5);

                    using (Line infiniteLine = new Line(pExtStart, pExtEnd))
                    {
                        Point3dCollection intersectPoints = new Point3dCollection();
                        poly.IntersectWith(infiniteLine, Intersect.OnBothOperands, intersectPoints, IntPtr.Zero, IntPtr.Zero);

                        // Création des segments coupés aux bordures exactes
                        if (intersectPoints.Count >= 2)
                        {
                            // Tri des points d'intersection le long de la ligne
                            Point3d[] sortedPts = new Point3d[intersectPoints.Count];
                            intersectPoints.CopyTo(sortedPts, 0);
                            Array.Sort(sortedPts, (pt1, pt2) => (pt1 - pExtStart).Length.CompareTo((pt2 - pExtStart).Length));

                            // Si la ligne traverse plusieurs fois une forme complexe (ex: forme en C ou U), 
                            // on crée des segments pour chaque paire d'intersections.
                            for (int k = 0; k < sortedPts.Length - 1; k += 2)
                            {
                                Line cutLine = new Line(sortedPts[k], sortedPts[k + 1]);
                                cutLine.AddToDrawingCurrentTransaction();
                            }
                        }
                    }
                }

                tr.Commit();
                Generic.WriteMessage($"Succès : Polyligne ({totalArea:F2} m²) divisée en {numParts} parties égales ({totalArea / numParts:F2} m²).");
            }
        }

        private static double CalculateHalfSpaceArea(Polyline poly, Point3d origStart, Point3d origEnd, Vector3d perpDir, Vector3d lineDir, double offset)
        {
            Point3d p1 = origStart + (perpDir * offset) - (lineDir * 1e5);
            Point3d p2 = origEnd + (perpDir * offset) + (lineDir * 1e5);

            Point3d p3 = p2 + (perpDir * 1e5);
            Point3d p4 = p1 + (perpDir * 1e5);

            using (Polyline halfSpacePoly = new Polyline())
            {
                halfSpacePoly.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0, 0, 0);
                halfSpacePoly.AddVertexAt(1, new Point2d(p2.X, p2.Y), 0, 0, 0);
                halfSpacePoly.AddVertexAt(2, new Point2d(p3.X, p3.Y), 0, 0, 0);
                halfSpacePoly.AddVertexAt(3, new Point2d(p4.X, p4.Y), 0, 0, 0);
                halfSpacePoly.Closed = true;

                using (DBObjectCollection mainColl = new DBObjectCollection { poly })
                using (DBObjectCollection halfColl = new DBObjectCollection { halfSpacePoly })
                {
                    using (DBObjectCollection mainRegions = Region.CreateFromCurves(mainColl))
                    using (DBObjectCollection halfRegions = Region.CreateFromCurves(halfColl))
                    {
                        if (mainRegions.Count == 0 || halfRegions.Count == 0)
                        {
                            return 0;
                        }

                        Region mainReg = (Region)mainRegions[0];
                        Region halfReg = (Region)halfRegions[0];

                        mainReg.BooleanOperation(BooleanOperationType.BoolIntersect, halfReg);

                        double area = mainReg.Area;

                        mainReg.Dispose();
                        halfReg.Dispose();

                        return area;
                    }
                }
            }
        }
    }
}