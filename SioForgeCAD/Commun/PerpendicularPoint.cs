using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static class PerpendicularPoint
    {
        // Fonction pour trouver le point d'intersection entre une ligne et un vecteur
        private static Point3d FindIntersection(Point3d startPoint, Vector3d vector, Line line)
        {
            double t = ((line.EndPoint.X - line.StartPoint.X) * (startPoint.Y - line.StartPoint.Y) -
                        (line.EndPoint.Y - line.StartPoint.Y) * (startPoint.X - line.StartPoint.X)) /
                        ((line.EndPoint.Y - line.StartPoint.Y) * vector.X - (line.EndPoint.X - line.StartPoint.X) * vector.Y);

            // Calculer le point d'intersection
            Point3d intersectionPoint = startPoint + t * vector;

            return intersectionPoint;
        }

        //public static Line GetPerpendicularLinePointProjection(Point3d LineStartPointSCG, Point3d LineEndPointSCG, Point3d PerpendicularPointCurrentSCU)
        //{
        //    Point3d PolyStart = new Points(LineStartPointSCG).SCG;
        //    Point3d PolyEnd = new Points(LineEndPointSCG).SCG;

        //    // Calculer la pente de la polyligne
        //    double m_AB = (PolyEnd.Y - PolyStart.Y) / (PolyEnd.X - PolyStart.X);
        //    // Calculer la pente de la ligne perpendiculaire
        //    double m_perp = -1 / m_AB;

        //    // Appliquer la transformation au vecteur directeur de la ligne perpendiculaire
        //    Vector3d perpVector = new Vector3d(1, m_perp, 0);
        //    return new Line(PerpendicularPointCurrentSCU, PerpendicularPointCurrentSCU + perpVector);
        //}

        public static Line GetPerpendicularLinePointProjection(Point3d LineStartPointSCG, Point3d LineEndPointSCG, Point3d PerpendicularPointCurrentSCU)
        {
            Point3d PolyStart = new Points(LineStartPointSCG).SCG;
            Point3d PolyEnd = new Points(LineEndPointSCG).SCG;

            // Calculer la pente de la polyligne
            double m_AB = (PolyEnd.X != PolyStart.X) ? (PolyEnd.Y - PolyStart.Y) / (PolyEnd.X - PolyStart.X) : double.PositiveInfinity;

            // Calculer la pente de la ligne perpendiculaire
            double m_perp = (m_AB != 0) ? -1 / m_AB : double.PositiveInfinity;

            // Appliquer la transformation au vecteur directeur de la ligne perpendiculaire
            Vector3d perpVector = (m_perp != double.PositiveInfinity) ? new Vector3d(1, m_perp, 0) : new Vector3d(0, 1, 0);

            return new Line(PerpendicularPointCurrentSCU, PerpendicularPointCurrentSCU + perpVector);
        }


        private static (Point3d PolylineSegmentStart,Point3d PolylineSegmentEnd) GetSegmentPoint(Polyline TargetPolyline, int Index)
        {
            int NumberOfVertices = TargetPolyline.NumberOfVertices;
            var PolylineSegmentStart = TargetPolyline.GetPoint3dAt(Index);
            Index += 1;
            if (Index >= NumberOfVertices)
            {
                Index = 0;
            }
            var PolylineSegmentEnd = TargetPolyline.GetPoint3dAt(Index);
            return (PolylineSegmentStart, PolylineSegmentEnd);
        }

        private static int getVerticesMaximum(Polyline TargetPolyline)
        {
            int NumberOfVertices = (TargetPolyline.NumberOfVertices - 1);
            if (TargetPolyline.Closed)
            {
                NumberOfVertices++;
            }
            return NumberOfVertices;
        }


        public static List<Line> GetListOfPerpendicularLinesFromPoint(Points BasePoint, Polyline TargetPolyline, bool CheckForSegmentIntersections = true)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<Line> PerpendicularLinesCollection = new List<Line>();
            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < getVerticesMaximum(TargetPolyline); PolylineSegmentIndex++)
            {
                var PolylineSegment = GetSegmentPoint(TargetPolyline, PolylineSegmentIndex);

                Vector3d PerpendicularVectorLine = GetPerpendicularLinePointProjection(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd, BasePoint.SCU).GetVector3d();

                Line SegmentLine = new Line(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd);
                Point3d IntersectionPoint = FindIntersection(BasePoint.SCU, PerpendicularVectorLine, SegmentLine);
                if (IntersectionPoint == Point3d.Origin)
                {
                    continue;
                }
                Line PerpendicularLine = new Line(BasePoint.SCU, IntersectionPoint);
                bool IsLineIsIntersectingOtherSegments = CheckForSegmentIntersections && CheckIfLineIsIntersectingOtherSegments(TargetPolyline, PerpendicularLine, PolylineSegmentIndex);
                if (!IsLineIsIntersectingOtherSegments && SegmentLine.IsLinePassesThroughPoint(IntersectionPoint))
                {
                    PerpendicularLinesCollection.Add(PerpendicularLine);
                }
            }
            List<Line> PerpendicularLinesCollectionSorted = PerpendicularLinesCollection.OrderBy(line => line.Length).ToList();
            return PerpendicularLinesCollectionSorted;
        }

        private static bool CheckIfLineIsIntersectingOtherSegments(Polyline TargetPolyline, Line PerpendicularLine, int CurrentIndex = -1)
        {
            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < getVerticesMaximum(TargetPolyline); PolylineSegmentIndex++)
            {
                if (PolylineSegmentIndex == CurrentIndex)
                {
                    continue;
                }
                var PolylineSegment = GetSegmentPoint(TargetPolyline, PolylineSegmentIndex);
                Line SegmentLineIntersectTest = new Line(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd);
                return Lines.AreLinesCutting(SegmentLineIntersectTest, PerpendicularLine);
            }
            return false;
        }

        private static void DrawVector(Vector3d vector3d, Point3d startPoint)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Dessiner le vecteur comme une ligne depuis le startPoint
                Point3d vectorEndPoint = startPoint + vector3d;
                Line vectorLine = new Line(startPoint, vectorEndPoint);
                btr.AppendEntity(vectorLine);
                trans.AddNewlyCreatedDBObject(vectorLine, true);

                trans.Commit();
            }
        }

        //public static void CreatePerpendicularLine()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;

        //    // Sélection de la polyligne
        //    PromptEntityResult polyResult = ed.GetEntity("Sélectionnez la polyligne : ");
        //    if (polyResult.Status != PromptStatus.OK)
        //    {
        //        ed.WriteMessage("Sélection de la polyligne annulée.");
        //        return;
        //    }

        //    ObjectId polyId = polyResult.ObjectId;

        //    // Sélection du point
        //    PromptPointOptions pointOptions = new PromptPointOptions("Sélectionnez le point K : ");
        //    PromptPointResult pointResult = ed.GetPoint(pointOptions);
        //    if (pointResult.Status != PromptStatus.OK)
        //    {
        //        ed.WriteMessage("Sélection du point annulée.");
        //        return;
        //    }

        //    Points pointK = pointResult.Value.ToPoints();

        //    using (Transaction trans = db.TransactionManager.StartTransaction())
        //    {
        //        try
        //        {
        //            // Ouvrir la polyligne
        //            Polyline poly = trans.GetObject(polyId, OpenMode.ForRead) as Polyline;
        //            if (poly == null)
        //            {
        //                ed.WriteMessage("L'objet sélectionné n'est pas une polyligne.");
        //                return;
        //            }
        //            List<Line> PerpendicularLinesCollection = new List<Line>();
        //            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
        //            {
        //                var startPt = poly.GetPoint3dAt(i);
        //                var endPt = poly.GetPoint3dAt(i + 1);

        //                Vector3d PerpendicularVectorLine = GetPerpendicularLinePointProjection(startPt, endPt, pointK.SCU).GetVector3d();

        //                Line SegmentLine = new Line(startPt, endPt);
        //                Point3d IntersectionPoint = FindIntersection(pointK.SCU, PerpendicularVectorLine, SegmentLine);
        //                if (IntersectionPoint == Point3d.Origin)
        //                {
        //                    continue;
        //                }
        //                Line PerpendicularLine = new Line(pointK.SCU, IntersectionPoint);


        //                bool IsNotIntersectingOtherSegments = true;
        //                for (int i2 = 0; i2 < poly.NumberOfVertices - 1; i2++)
        //                {
        //                    if (i2 == i)
        //                    {
        //                        continue;
        //                    }
        //                    if (!IsNotIntersectingOtherSegments)
        //                    {
        //                        continue;
        //                    }
        //                    Line SegmentLineIntersectTest = new Line(poly.GetPoint3dAt(i2), poly.GetPoint3dAt(i2 + 1));
        //                    Lines.SingleLine(SegmentLineIntersectTest, 255);
        //                    Lines.SingleLine(PerpendicularLine.Clone() as Line, 255);
        //                    IsNotIntersectingOtherSegments = !Lines.AreLinesCutting(SegmentLineIntersectTest, PerpendicularLine);
        //                }
        //                if (IsNotIntersectingOtherSegments && SegmentLine.IsLinePassesThroughPoint(IntersectionPoint))
        //                {
        //                    //Lines.SingleLine(PerpendicularLine);
        //                    PerpendicularLinesCollection.Add(PerpendicularLine);
        //                }
        //            }
        //            List<Line> PerpendicularLinesCollectionSorted = PerpendicularLinesCollection.OrderBy(line => line.Length).ToList();
        //            if (PerpendicularLinesCollectionSorted.Count > 0)
        //            {
        //                Lines.SingleLine(PerpendicularLinesCollectionSorted.FirstOrDefault(), 1);
        //            }

        //            trans.Commit();
        //        }
        //        catch (System.Exception ex)
        //        {
        //            ed.WriteMessage("Erreur : " + ex.Message);
        //            trans.Abort();
        //        }
        //    }
        //}














    }
}
