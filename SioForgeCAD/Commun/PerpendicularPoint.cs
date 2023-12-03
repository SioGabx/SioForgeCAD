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

                Vector3d PerpendicularVectorLine = GetPerpendicularLinePointProjection(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd, BasePoint.SCG).GetVector3d();

                Line SegmentLine = new Line(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd);
                Point3d IntersectionPoint = FindIntersection(BasePoint.SCG, PerpendicularVectorLine, SegmentLine);
                if (IntersectionPoint == Point3d.Origin)
                {
                    continue;
                }
                Line PerpendicularLine = new Line(BasePoint.SCG, IntersectionPoint);
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

    

    }
}
