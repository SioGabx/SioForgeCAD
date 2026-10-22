using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class POLYLIGNDIVIDEEQUALAREA
    {
        public static void DiviserAireEquivalente()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. Sélection polyligne
            PromptEntityOptions peoPoly = new PromptEntityOptions("\nSélectionnez une polyligne fermée : ");
            peoPoly.SetRejectMessage("Veuillez sélectionner une polyligne valide.");
            peoPoly.AddAllowedClass(typeof(Polyline), false);
            PromptEntityResult perPoly = ed.GetEntity(peoPoly);
            if (perPoly.Status != PromptStatus.OK) return;

            // 2. Sélection ligne
            PromptEntityOptions peoLine = new PromptEntityOptions("\nSélectionnez le segment/ligne de direction : ");
            peoLine.SetRejectMessage("Veuillez sélectionner une ligne valide.");
            peoLine.AddAllowedClass(typeof(Line), false);
            PromptEntityResult perLine = ed.GetEntity(peoLine);
            if (perLine.Status != PromptStatus.OK) return;

            // 3. Nombre de parties
            PromptIntegerOptions pio = new PromptIntegerOptions("\nEn combien de parties voulez-vous diviser l'aire ? ")
            {
                LowerLimit = 2,
                UpperLimit = 100,
                DefaultValue = 2,
                UseDefaultValue = true
            };
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK) return;

            int numParts = pir.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline poly = tr.GetObject(perPoly.ObjectId, OpenMode.ForRead) as Polyline;
                Line baseLine = tr.GetObject(perLine.ObjectId, OpenMode.ForRead) as Line;

                if (poly == null || !poly.Closed)
                {
                    ed.WriteMessage("\nErreur : La polyligne doit être fermée.");
                    return;
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                double totalArea = poly.Area;
                Vector3d lineDir = (baseLine.EndPoint - baseLine.StartPoint).GetNormal();
                Vector3d perpDir = lineDir.CrossProduct(Vector3d.ZAxis).GetNormal();

                Point3d origStart = baseLine.StartPoint;
                Point3d origEnd = baseLine.EndPoint;

                // Projection des sommets pour établir le domaine exact de balayage
                double minOffset = double.MaxValue;
                double maxOffset = double.MinValue;

                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    Point3d pt = poly.GetPoint3dAt(i);
                    double proj = (pt - origStart).DotProduct(perpDir);
                    if (proj < minOffset) minOffset = proj;
                    if (proj > maxOffset) maxOffset = proj;
                }

                // Ajout d'une marge de sécurité
                double margin = (maxOffset - minOffset) * 0.05;
                minOffset -= margin;
                maxOffset += margin;

                // Détermination de l'orientation de croissance de l'aire
                double areaMin = CalculateHalfSpaceArea(poly, origStart, origEnd, perpDir, lineDir, minOffset);
                double areaMax = CalculateHalfSpaceArea(poly, origStart, origEnd, perpDir, lineDir, maxOffset);
                bool isIncreasing = areaMax > areaMin;

                // Boucle de division
                for (int part = 1; part < numParts; part++)
                {
                    double targetArea = (totalArea / numParts) * part;

                    double low = minOffset;
                    double high = maxOffset;
                    double mid = 0;
                    double currentArea = 0;
                    double tolerance = 1e-4;

                    for (int i = 0; i < 60; i++)
                    {
                        mid = (low + high) / 2.0;

                        currentArea = CalculateHalfSpaceArea(poly, origStart, origEnd, perpDir, lineDir, mid);

                        Debug.WriteLine($"[DEBUG] Part {part} | Itération {i + 1:D2} | Offset: {mid,10:F4} | Aire: {currentArea,12:F4} | Cible: {targetArea,12:F4}");

                        if (Math.Abs(currentArea - targetArea) < tolerance)
                            break;

                        if (isIncreasing)
                        {
                            if (currentArea < targetArea) low = mid;
                            else high = mid;
                        }
                        else
                        {
                            if (currentArea > targetArea) low = mid;
                            else high = mid;
                        }
                    }

                    // Insertion de la ligne de separation finale
                    Point3d pStart = origStart + (perpDir * mid);
                    Point3d pEnd = origEnd + (perpDir * mid);
                    Line cutLine = new Line(pStart, pEnd);
                    cutLine.SetDatabaseDefaults();
                    btr.AppendEntity(cutLine);
                    tr.AddNewlyCreatedDBObject(cutLine, true);
                }

                tr.Commit();
                ed.WriteMessage($"\nSuccès : Polyligne ({totalArea:F2} m²) divisée en {numParts} parties égales ({totalArea / numParts:F2} m²).");
            }
        }

        private static double CalculateHalfSpaceArea(Polyline poly, Point3d origStart, Point3d origEnd, Vector3d perpDir, Vector3d lineDir, double offset)
        {
            // Position courante de la ligne de coupe
            Point3d p1 = origStart + (perpDir * offset) - (lineDir * 1e5);
            Point3d p2 = origEnd + (perpDir * offset) + (lineDir * 1e5);

            // Construction d'un demi-plan très large du côté positif de perpDir
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
                            return 0;

                        Region mainReg = (Region)mainRegions[0];
                        Region halfReg = (Region)halfRegions[0];

                        // Intersection booléenne
                        mainReg.BooleanOperation(BooleanOperationType.BoolIntersect, halfReg);

                        double area = mainReg.Area;

                        // Nettoyage explicite
                        mainReg.Dispose();
                        halfReg.Dispose();

                        return area;
                    }
                }
            }
        }
    }
}