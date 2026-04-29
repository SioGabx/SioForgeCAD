using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class PolylinesExtensions
    {
        public static int GetReelNumberOfVertices(this Polyline TargetPolyline)
        {
            if (TargetPolyline?.IsDisposed == true)
            {
                return 0;
            }
            int NumberOfVertices = TargetPolyline.NumberOfVertices - 1;
            if (TargetPolyline.Closed)
            {
                NumberOfVertices++;
            }
            return NumberOfVertices;
        }
        public static Polyline GetPolylineFromPoints(this IEnumerable<Points> listOfPoints)
        {
            Polyline polyline = new Polyline();
            foreach (Points point in listOfPoints)
            {
                polyline.AddVertexAt(polyline.NumberOfVertices, point.SCG.ToPoint2d(), 0, 0, 0);
            }
            return polyline;
        }

        public enum PolylineSide
        {
            Right,
            Left,
            Collinear
        }

        public static PolylineSide CheckPointSide(this Polyline BasePolyline, Point3d TargetPoint)
        {
            for (int segmentIndex = 0; segmentIndex < BasePolyline.NumberOfVertices - 1; segmentIndex++)
            {
                Point3d startPoint = BasePolyline.GetPoint3dAt(segmentIndex);
                Point3d endPoint = BasePolyline.GetPoint3dAt(segmentIndex + 1);

                Vector2d polylineVector = new Vector2d(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
                Vector2d pointVector = new Vector2d(TargetPoint.X - startPoint.X, TargetPoint.Y - startPoint.Y);

                //cross product
                double crossProduct = (polylineVector.X * pointVector.Y) - (polylineVector.Y * pointVector.X);

                if (crossProduct < 0)
                {
                    //left
                    return PolylineSide.Left;
                }
                else if (crossProduct > 0)
                {
                    // Right
                    return PolylineSide.Right;
                }
            }
            //collinear
            return PolylineSide.Collinear;
        }

        public static bool IsAtLeftSide(this Polyline BasePolyline, Point3d TargetPoint)
        {
            return CheckPointSide(BasePolyline, TargetPoint) == PolylineSide.Left;
        }
        public static bool IsAtRightSide(this Polyline BasePolyline, Point3d TargetPoint)
        {
            return CheckPointSide(BasePolyline, TargetPoint) == PolylineSide.Right;
        }

        public static bool FixNormal(this Polyline polyline)
        {
            //Fix normals : should be 0,0,1 and its 0,0,-1. This happen when the polyline was drawn with the bottom up
            if (polyline.Normal.IsEqualTo(Vector3d.ZAxis.MultiplyBy(-1)))
            {
                Debug.WriteLine("Correction de la normal d'une polyline");
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    //var acPlArc = acPlLwObj.GetArcSegmentAt(i);
                    var acPl3DPoint = polyline.GetPoint3dAt(i);
                    var acPl2DPointNew = new Point2d(acPl3DPoint.X, acPl3DPoint.Y);
                    polyline.SetPointAt(i, acPl2DPointNew);
                    polyline.SetBulgeAt(i, -polyline.GetBulgeAt(i));
                }

                polyline.Normal = Vector3d.ZAxis;
                return true;
            }
            return false;
        }


        public static void ResetStyle(this Polyline polyline)
        {
            if (polyline == null) return;

            // Récupérer la base de données active (soit celle de la poly, soit la globale)
            Database db = polyline.Database ?? HostApplicationServices.WorkingDatabase;

            polyline.LayerId = db.Clayer;

            polyline.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
            polyline.Linetype = "ByLayer";
            polyline.LinetypeScale = 1.0;
            polyline.LineWeight = LineWeight.ByLayer;
            polyline.Transparency = new Transparency(TransparencyMethod.ByLayer);

            // 3. Réinitialisation de l'épaisseur 3D
            polyline.Thickness = 0.0;
            polyline.ConstantWidth = 0.0;

            // 4. Mise à plat absolue (Z = 0)
            polyline.Elevation = 0.0;
            polyline.Normal = Vector3d.ZAxis;
        }
        public static void Flatten(this Polyline polyline)
        {
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var acPl3DPoint = polyline.GetPoint3dAt(i);
                var acPl2DPointNew = new Point2d(acPl3DPoint.X, acPl3DPoint.Y);
                polyline.SetPointAt(i, acPl2DPointNew);
            }
        }

        public static bool HasAngle(this Polyline TargetPolyline, double DegreesTolerance)
        {
            for (int i = 0; i < TargetPolyline.NumberOfVertices - 2; i++)
            {
                Point2d pt1 = TargetPolyline.GetPoint2dAt(i);
                Point2d pt2 = TargetPolyline.GetPoint2dAt(i + 1);
                Point2d pt3 = TargetPolyline.GetPoint2dAt(i + 2);

                Vector2d v1 = pt2 - pt1;
                Vector2d v2 = pt3 - pt2;

                double angle = v1.GetAngleTo(v2) * (180.0 / Math.PI);

                if (Math.Abs(angle - 180) > DegreesTolerance)
                {
                    return true;
                }
            }
            return false;
        }

        public static (Point3d StartPoint, Point3d EndPoint, double Bulge) GetSegmentAt(this Polyline TargetPolyline, int Index)
        {
            int NumberOfVertices = TargetPolyline.NumberOfVertices;
            double Bulge = TargetPolyline.GetBulgeAt(Index);
            var PolylineSegmentStart = TargetPolyline.GetPoint3dAt(Index);
            Index++;
            if (Index >= NumberOfVertices)
            {
                Index = 0;
            }
            var PolylineSegmentEnd = TargetPolyline.GetPoint3dAt(Index);
            return (PolylineSegmentStart, PolylineSegmentEnd, Bulge);
        }

        public static double GetArea(this Polyline pline)
        {
            double area = 0.0;
            if (pline.NumberOfVertices == 0)
            {
                return area;
            }
            int last = pline.NumberOfVertices - 1;
            Point2d p0 = pline.GetPoint2dAt(0);

            if (pline.GetBulgeAt(0) != 0.0)
            {
                area += pline.GetArcSegment2dAt(0).GetArea();
            }
            for (int i = 1; i < last; i++)
            {
                area += p0.GetArea(pline.GetPoint2dAt(i), pline.GetPoint2dAt(i + 1));
                if (pline.GetBulgeAt(i) != 0.0)
                {
                    area += pline.GetArcSegment2dAt(i).GetArea();
                }
            }
            if ((pline.GetBulgeAt(last) != 0.0) && pline.Closed)
            {
                area += pline.GetArcSegment2dAt(last).GetArea();
            }
            return area;
        }

        public static DBObjectCollection BreakAt(this Polyline poly, params Point3d[] points)
        {
            DoubleCollection DblCollection = new DoubleCollection();
            foreach (Point3d point in points)
            {
                var param = poly.GetParamAtPointX(point);
                DblCollection.Add(param);
                DblCollection.Add(param);
            }
            return poly.GetSplitCurves(DblCollection);
        }

        public static void CleanupPolylines(this IEnumerable<Polyline> ListOfPolyline)
        {
            foreach (var Line in ListOfPolyline)
            {
                Line.Cleanup();
            }
        }

        public static void Cleanup(this Polyline polyline)
        {
            if (polyline == null || polyline.NumberOfVertices <= 2)
            {
                return;
            }

            Tolerance tol = Tolerance.Global;

            // 1. Fermer proprement la polyligne si le point de départ et de fin se touchent
            if (!polyline.Closed && polyline.NumberOfVertices > 2)
            {
                Point2d startPt = polyline.GetPoint2dAt(0);
                Point2d endPt = polyline.GetPoint2dAt(polyline.NumberOfVertices - 1);

                if (startPt.IsEqualTo(endPt, tol))
                {
                    polyline.RemoveVertexAt(polyline.NumberOfVertices - 1);
                    polyline.Closed = true;
                }
            }

            // 2. Supprimer les sommets en double (points de longueur nulle) avec transfert de Bulge
            for (int i = polyline.NumberOfVertices - 1; i > 0; i--)
            {
                if (polyline.GetPoint2dAt(i).IsEqualTo(polyline.GetPoint2dAt(i - 1), tol))
                {
                    double bulgeToKeep = polyline.GetBulgeAt(i);
                    polyline.SetBulgeAt(i - 1, bulgeToKeep);
                    polyline.RemoveVertexAt(i);
                }
            }

            if (polyline.Closed && polyline.NumberOfVertices > 2)
            {
                int lastIdx = polyline.NumberOfVertices - 1;
                if (polyline.GetPoint2dAt(0).IsEqualTo(polyline.GetPoint2dAt(lastIdx), tol))
                {
                    polyline.RemoveVertexAt(lastIdx);
                }
            }

            // 3. Fusionner les segments colinéaires (Lignes) ET les arcs continus (Arcs)
            for (int i = polyline.NumberOfVertices - 1; i >= 0; i--)
            {
                if (polyline.NumberOfVertices <= 2)
                {
                    break;
                }

                int currIdx = i;
                int prevIdx = (i == 0) ? polyline.NumberOfVertices - 1 : i - 1;
                int nextIdx = (i == polyline.NumberOfVertices - 1) ? 0 : i + 1;

                if (!polyline.Closed && (i == 0 || i == polyline.NumberOfVertices - 1))
                {
                    continue;
                }

                double bulgePrev = polyline.GetBulgeAt(prevIdx);
                double bulgeCurr = polyline.GetBulgeAt(currIdx);

                bool isPrevLine = Math.Abs(bulgePrev) < Generic.MediumTolerance.EqualPoint;
                bool isCurrLine = Math.Abs(bulgeCurr) < Generic.MediumTolerance.EqualPoint;

                // --- CAS A : Deux LIGNES DROITES colinéaires ---
                if (isPrevLine && isCurrLine)
                {
                    Point2d pPrev = polyline.GetPoint2dAt(prevIdx);
                    Point2d pCurr = polyline.GetPoint2dAt(currIdx);
                    Point2d pNext = polyline.GetPoint2dAt(nextIdx);

                    Vector2d v1 = pPrev.GetVectorTo(pCurr);
                    Vector2d v2 = pCurr.GetVectorTo(pNext);

                    if (v1.Length > Generic.MediumTolerance.EqualPoint && v2.Length > Generic.MediumTolerance.EqualPoint)
                    {
                        if (v1.GetNormal().IsParallelTo(v2.GetNormal(), tol))
                        {
                            polyline.RemoveVertexAt(currIdx);
                        }
                    }
                }
                // --- CAS B : Deux ARCS continus ---
                else if (!isPrevLine && !isCurrLine)
                {
                    try
                    {
                        CircularArc2d arc1 = polyline.GetArcSegment2dAt(prevIdx);
                        CircularArc2d arc2 = polyline.GetArcSegment2dAt(currIdx);

                        // S'ils partagent le même centre, le même rayon, et tournent dans le même sens
                        if (arc1.Center.IsEqualTo(arc2.Center, tol) &&
                            Math.Abs(arc1.Radius - arc2.Radius) < Generic.MediumTolerance.EqualPoint &&
                            Math.Sign(bulgePrev) == Math.Sign(bulgeCurr))
                        {
                            // Calcul des angles inclus à partir des bulges : theta = 4 * atan(bulge)
                            double theta1 = 4 * Math.Atan(bulgePrev);
                            double theta2 = 4 * Math.Atan(bulgeCurr);
                            double thetaNew = theta1 + theta2;

                            // Règle géométrique d'AutoCAD : Un seul segment d'arc ne peut pas faire 360° ou plus
                            // Si la somme dépasse un cercle complet, on ne les fusionne pas.
                            if (Math.Abs(thetaNew) < (Math.PI * 2) - Generic.MediumTolerance.EqualPoint)
                            {
                                // Calcul du nouveau bulge fusionné
                                double newBulge = Math.Tan(thetaNew / 4);

                                polyline.RemoveVertexAt(currIdx);

                                // Ajustement d'index : si on a supprimé le point 0, le point de fin (N-1) est devenu N-2 !
                                int targetIdx = (prevIdx > currIdx) ? prevIdx - 1 : prevIdx;
                                polyline.SetBulgeAt(targetIdx, newBulge);
                            }
                        }
                    }
                    catch
                    {
                        // Sécurité au cas où GetArcSegment2dAt échoue sur un arc dégénéré
                    }
                }
            }
        }

        /// <summary>
        /// Convertit les facettes (suites de segments de lignes courtes) d'une polyligne en véritables arcs (Bulges) 
        /// si les points suivent une trajectoire circulaire.
        /// </summary>
        /// <param name="polyline">La polyligne à optimiser.</param>
        /// <param name="tolerance">La distance d'erreur maximale autorisée entre les points supprimés et le nouvel arc (par ex: 0.01).</param>
        /// <param name="minSegmentsToFormArc">Le nombre minimum de segments de ligne successifs requis pour autoriser la conversion en un arc (Défaut: 3).</param>
        public static void OptimizeFacetsToArcs(this Polyline polyline, double tolerance = 0.01, int minSegmentsToFormArc = 3)
        {
            if (polyline == null || polyline.NumberOfVertices <= minSegmentsToFormArc) return;

            int i = 0;
            while (i <= polyline.NumberOfVertices - minSegmentsToFormArc - 1)
            {
                if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-6) { i++; continue; }

                int bestEndIdx = -1;
                double bestBulge = 0;

                // Exploration pour trouver l'arc le plus long possible
                for (int j = i + minSegmentsToFormArc; j < polyline.NumberOfVertices; j++)
                {
                    if (Math.Abs(polyline.GetBulgeAt(j - 1)) > 1e-6) break;

                    Point2d pStart = polyline.GetPoint2dAt(i);
                    Point2d pEnd = polyline.GetPoint2dAt(j);
                    Point2d pMid = polyline.GetPoint2dAt(i + (j - i) / 2);

                    // Calcul de la corde
                    Line2d chord = new Line2d(pStart, pEnd);
                    if (chord.GetDistanceTo(pMid) < 1e-6) break;

                    CircularArc2d arc;
                    try { arc = new CircularArc2d(pStart, pMid, pEnd); }
                    catch { break; }

                    // --- CRITÈRE DE TOLÉRANCE GÉOMÉTRIQUE ---
                    bool allPointsFit = true;
                    for (int k = i + 1; k < j; k++)
                    {
                        Point2d pk = polyline.GetPoint2dAt(k);

                        // 1. Vérification de la distance au rayon
                        double distToCenter = pk.GetDistanceTo(arc.Center);
                        if (Math.Abs(distToCenter - arc.Radius) > tolerance)
                        {
                            allPointsFit = false;
                            break;
                        }

                        // 2. Vérification de la déviation angulaire (Évite de lisser des angles vifs)
                        // On vérifie que le segment (k-1, k) ne fait pas un angle trop brusque
                        Vector2d v1 = polyline.GetPoint2dAt(k) - polyline.GetPoint2dAt(k - 1);
                        Vector2d v2 = polyline.GetPoint2dAt(k + 1) - polyline.GetPoint2dAt(k);
                        double angle = v1.GetAngleTo(v2);

                        // Si l'angle entre deux segments est > 45° (par ex), ce n'est probablement pas un arc
                        if (Math.Abs(angle) > Math.PI / 4)
                        {
                            allPointsFit = false;
                            break;
                        }
                    }

                    if (allPointsFit)
                    {
                        bestEndIdx = j;
                        // Calcul précis du Bulge
                        double alpha = arc.EndAngle - arc.StartAngle;
                        if (alpha < 0) alpha += Math.PI * 2;
                        if (alpha > Math.PI * 2) alpha -= Math.PI * 2;

                        // Si l'arc fait plus de 180°, le calcul du bulge doit rester cohérent
                        double b = Math.Tan(alpha / 4.0);
                        bestBulge = arc.IsClockWise ? -b : b;
                    }
                    else break;
                }

                if (bestEndIdx != -1)
                {
                    polyline.SetBulgeAt(i, bestBulge);
                    for (int k = bestEndIdx - 1; k > i; k--)
                    {
                        polyline.RemoveVertexAt(k);
                    }
                    i++;
                }
                else i++;
            }
        }

        public static void Inverse(this Polyline poly)
        {
            //https://www.keanw.com/2012/09/reversing-the-direction-of-an-autocad-polyline-using-net.html
            try
            {
                poly.ReverseCurve();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static IEnumerable<Point2d> GetPolyPoints(this Polyline poly)
        {
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                yield return poly.GetPoint2dAt(i);
            }
        }

        public static Spline GetSpline(this Polyline pline)
        {
            Spline spline = null;
            void CreateSpline(NurbCurve3d nurb)
            {
                if (spline is null)
                {
                    spline = (Spline)Curve.CreateFromGeCurve(nurb);
                }
                else
                {
                    using (var spl = (Spline)Curve.CreateFromGeCurve(nurb))
                    {
                        try
                        {
                            spline.JoinEntity(spl);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"GetSpline : Impossible to Join a Entity : {ex.Message}");
                        }
                    }
                }
            }
            for (int i = 0; i < pline.NumberOfVertices; i++)
            {
                switch (pline.GetSegmentType(i))
                {
                    case SegmentType.Line:
                        CreateSpline(new NurbCurve3d(pline.GetLineSegmentAt(i)));
                        break;
                    case SegmentType.Arc:
                        CreateSpline(new NurbCurve3d(pline.GetArcSegmentAt(i).GetEllipticalArc()));
                        break;
                    default:
                        break;
                }
            }
            return spline;
        }

        public static Polyline ToPolygon(this Polyline poly, uint NumberOfVertexPerArc = 15)
        {
            if (poly.HasBulges)
            {
                uint NumberOfVertex = (uint)poly.GetReelNumberOfVertices();
                for (int i = 0; i < poly.GetReelNumberOfVertices(); i++)
                {
                    if (poly.GetSegmentType(i) == SegmentType.Arc)
                    {
                        NumberOfVertex += NumberOfVertexPerArc;
                    }
                }
                var NewPoly = new Polyline();

                for (int VerticeIndex = 0; VerticeIndex < poly.NumberOfVertices; VerticeIndex++)
                {
                    var CurrentPoint = poly.GetPoint3dAt(VerticeIndex);
                    NewPoly.AddVertex(CurrentPoint);
                    if (poly.GetSegmentType(VerticeIndex) == SegmentType.Line)
                    {
                        continue;
                    }
                    else if (poly.GetSegmentType(VerticeIndex) == SegmentType.Arc)
                    {
                        var Segment = poly.GetArcSegmentAt(VerticeIndex);
                        using (var Arc = Segment.ToCircleOrArc())
                        {
                            var ReelNumberOfVertex = NumberOfVertexPerArc * Math.Max(Math.Abs(poly.GetBulgeAt(VerticeIndex)), 1);
                            var Interval = (Arc.EndParam - Arc.StartParam) / (ReelNumberOfVertex + 1);
                            for (int NumberOfInterval = 1; NumberOfInterval < ReelNumberOfVertex + 1; NumberOfInterval++)
                            {
                                var Pt = Arc.GetPointAtParam(Arc.StartParam + (Interval * NumberOfInterval));
                                NewPoly.AddVertex(Pt, 0, 0, 0);
                            }
                        }
                    }
                }

                NewPoly.Closed = poly.Closed;
                return NewPoly;
            }
            return poly.Clone() as Polyline;
        }

        /// <summary>
        /// Gets the bulge between two parameters within the same arc segment of a polyline.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <param name="startParam">The start parameter.</param>
        /// <param name="endParam">The end parameter.</param>
        /// <returns>The bulge.</returns>
        public static double GetBulgeBetween(this Polyline poly, double startParam, double endParam)
        {
            double total = poly.GetBulgeAt((int)Math.Floor(startParam));
            return (endParam - startParam) * total;
        }

        public static void AddVertex(this Polyline Poly, Point3d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            AddVertex(Poly, point.ToPoint2d(), bulge, startWidth, endWidth);
        }
        public static void AddVertex(this Polyline Poly, Point2d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            Poly.AddVertexAt(Poly.NumberOfVertices, point, bulge, startWidth, endWidth);
        }

        public static void AddVertex(this Polyline3d Poly, Point3d point)
        {
            using (var Vertex = new PolylineVertex3d(point))
            {
                Poly.AppendVertex(Vertex);
            }
        }

        public static void AddVertexIfNotExist(this Polyline Poly, Point3d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            for (int i = 0; i < Poly.NumberOfVertices; i++)
            {
                if (Poly.GetPoint3dAt(i) == point)
                {
                    return;
                }
            }
            AddVertex(Poly, point, bulge, startWidth, endWidth);
        }

        public static bool IsClockwise(this Polyline poly)
        {
            if (poly.NumberOfVertices < 2)
            {
                return false;
            }

            double area = 0.0;
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                Point2d p1 = poly.GetPoint2dAt(i);
                Point2d p2 = poly.GetPoint2dAt((i + 1) % poly.NumberOfVertices);
                area += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

            return area > 0; // horaire si aire > 0 (repère AutoCAD)
        }

        /// <summary>
        /// Connects polylines.
        /// </summary>
        /// <param name="poly">The base polyline.</param>
        /// <param name="poly1">The other polyline.</param>
        public static void JoinPolyline(this Polyline poly, Polyline poly1)
        {
            int index = poly.GetPolyPoints().Count();
            int index1 = 0;
            var Points = poly1.GetPoints();
            if (!poly.IsWriteEnabled)
            {
                poly.UpgradeOpen();
            }
            foreach (var point in Points)
            {
                poly.AddVertexAt(index, point.ToPoint2d(), poly1.GetBulgeAt(index1), 0, 0);
                index++;
                index1++;
            }
        }

        public static Polyline ToPolyline(this Polyline3d poly3d)
        {
            if (poly3d.PolyType == Poly3dType.SimplePoly)
            {
                Polyline poly2d = new Polyline();
                foreach (PolylineVertex3d vertex in poly3d)
                {
                    if (vertex != null)
                    {
                        Point2d point = new Point2d(vertex.Position.X, vertex.Position.Y);
                        poly2d.AddVertexAt(poly2d.NumberOfVertices, point, 0, 0, 0);
                    }
                }
                poly2d.Closed = poly3d.Closed;
                return poly2d;
            }
            else
            {
                return poly3d.Spline.ToPolyline() as Polyline;
            }
        }

        public static Entity ToLWPolylineOrSpline(this Polyline3d poly3d)
        {
            if (poly3d.PolyType == Poly3dType.SimplePoly)
            {
                return poly3d.ToPolyline();
            }
            else
            {
                return poly3d.Spline;
            }
        }

        public static Polyline ToPolyline(this Polyline2d poly2d)
        {
            if (poly2d.PolyType == Poly2dType.QuadSplinePoly || poly2d.PolyType == Poly2dType.CubicSplinePoly)
            {
                var Spline = poly2d.Spline;
                return Spline.ToPolyline() as Polyline;
            }
            Polyline poly = new Polyline();
            poly.ConvertFrom(poly2d, false);
            return poly;
        }

        public static Entity ToLWPolylineOrSpline(this Polyline2d poly2d)
        {
            if (poly2d.PolyType == Poly2dType.SimplePoly)
            {
                return poly2d.ToPolyline();
            }
            else
            {
                return poly2d.Spline;
            }
        }

        public static IEnumerable<Polyline> SmartOffset(this Polyline ArgPoly, double ShrinkDistance)
        {
            using (var poly = ArgPoly.Clone() as Polyline)
            {
                if (poly.Area <= Generic.MediumTolerance.EqualPoint)
                {
                    return Array.Empty<Polyline>();
                }
                poly.Closed = true;

                //Forcing close can result in weird point, we need to cleanup these before executing a offset
                poly.Cleanup();

                IEnumerable<Polyline> OffsetResult = InternalSmartOffset(poly);
                if (!OffsetResult.Any())
                {
                    poly.Inverse();
                    OffsetResult = InternalSmartOffset(poly);
                }
                return OffsetResult;
            }

            IEnumerable<Polyline> InternalSmartOffset(Polyline InternalPoly)
            {
                // UseOffsetGapTypeCurrentValue need to be 0 to avoid rouded corners
                List<Polyline> OffsetPolylineResult = InternalPoly.OffsetPolyline(ShrinkDistance, UseOffsetGapTypeCurrentValue: false).Cast<Polyline>().ToList();

                if (OffsetPolylineResult.Count == 0)
                {
                    //If OffsetPolyline result in no geometry, we need to fix the polyline first : custom cleanup
                    bool HasVertexRemoved = true;
                    while (HasVertexRemoved)
                    {
                        HasVertexRemoved = false;
                        int index = 0;
                        while (index < InternalPoly.GetReelNumberOfVertices())
                        {
                            var CurrentPoint = InternalPoly.GetPoint2dAt(index);
                            int nextPoint = index + 1;
                            if (nextPoint >= InternalPoly.GetReelNumberOfVertices())
                            {
                                nextPoint = 0;
                            }
                            var NextPoint = InternalPoly.GetPoint2dAt(nextPoint);
                            var DistanceBetween = CurrentPoint.GetDistanceTo(NextPoint);
                            if (InternalPoly.GetSegmentType(index) == SegmentType.Line)
                            {
                                //Small line that we cant offset;
                                if (DistanceBetween <= Math.Abs(ShrinkDistance))
                                {
                                    InternalPoly.RemoveVertexAt(index);
                                    continue;
                                }
                            }
                            else if (InternalPoly.GetSegmentType(index) == SegmentType.Arc)
                            {
                                //If there is 0.2 with gap, that mean previous offset generated Arc, we need to remove those.
                                var Segment = InternalPoly.GetArcSegmentAt(index);
                                //Multiply by 2 + 5% of error margin
                                if (DistanceBetween <= Math.Abs(ShrinkDistance) * 2.05)
                                {
                                    using (var Arc = Segment.ToCircleOrArc())
                                    {
                                        var ArcMidPoint = Arc.GetPointAtParam((Arc.StartParam + Arc.EndParam) / 2);
                                        var SegMidPoint = CurrentPoint.GetMiddlePoint(NextPoint);

                                        var NewPoint = ArcMidPoint.TransformBy(Matrix3d.Displacement(SegMidPoint.GetVectorTo(ArcMidPoint).SetLength(Math.Abs(ShrinkDistance * 100))));

                                        InternalPoly.SetBulgeAt(index, 0);
                                        InternalPoly.AddVertexAt(index + 1, NewPoint.ToPoint2d(), 0, 0, 0);
                                        continue;
                                    }
                                }
                            }
                            index++;
                        }
                    }

                    //Cleanup the line (NEEDED ! if not in futur please explain why)
                    InternalPoly.Cleanup();
                    // UseOffsetGapTypeCurrentValue need to be 0 to avoid rouded corners
                    OffsetPolylineResult = InternalPoly.OffsetPolyline(ShrinkDistance, UseOffsetGapTypeCurrentValue: false).Cast<Polyline>().ToList();
                }

                var OffsetMergedPolylineResult = OffsetPolylineResult.JoinMerge();
                OffsetPolylineResult.DeepDispose();
                var ReturnOffsetMergedPolylineResult = OffsetMergedPolylineResult.Cast<Polyline>().Where(p => p?.Closed == true && p.NumberOfVertices >= 2).ToList();
                OffsetMergedPolylineResult.RemoveCommun(ReturnOffsetMergedPolylineResult).DeepDispose();
                foreach (var item in ReturnOffsetMergedPolylineResult)
                {
                    item.Cleanup();
                }
                return ReturnOffsetMergedPolylineResult;
            }
        }

        public static Point3d GetInnerCentroid(this Polyline poly)
        {
            var polygon = poly.ToPolygon(10);
            var pt = PolygonOperation.GetInnerCentroid(polygon, 1);
            if (polygon != poly) { polygon?.Dispose(); }
            return pt;
        }

        public static Point3d GetCentroid(this Polyline pl)
        {
            int count = pl.NumberOfVertices;
            if (count == 0)
            {
                throw new ArgumentException("Polyline vide.");
            }

            double sumX = 0, sumY = 0, sumZ = 0;
            for (int i = 0; i < count; i++)
            {
                Point3d pt = pl.GetPoint3dAt(i);
                sumX += pt.X;
                sumY += pt.Y;
                sumZ += pt.Z;
            }
            return new Point3d(sumX / count, sumY / count, sumZ / count);
        }

        public static bool IsOverlaping(this Polyline LineA, Polyline LineB)
        {
            var NumberOfVertices = LineA.GetReelNumberOfVertices();
            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < NumberOfVertices; PolylineSegmentIndex++)
            {
                var PolylineSegment = LineA.GetSegmentAt(PolylineSegmentIndex);
                Point3d MiddlePoint = PolylineSegment.StartPoint.GetMiddlePoint(PolylineSegment.EndPoint);

                if ((PolylineSegment.StartPoint.DistanceTo(PolylineSegment.EndPoint) / 2) > Generic.MediumTolerance.EqualPoint)
                {
                    if (MiddlePoint.IsOnPolyline(LineB))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsInside(this Polyline LineA, Polyline LineB, bool CheckEach = true)
        {
            int NumberOfVertices = 1;
            int ReelNumberOfVertices = LineA.GetReelNumberOfVertices();
            if (CheckEach)
            {
                NumberOfVertices = ReelNumberOfVertices;
            }

            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < NumberOfVertices; PolylineSegmentIndex++)
            {
                var PolylineSegment = LineA.GetSegmentAt(PolylineSegmentIndex);
                if ((PolylineSegment.StartPoint.DistanceTo(PolylineSegment.EndPoint) / 2) > Generic.MediumTolerance.EqualPoint)
                {
                    Point3d MiddlePoint;
                    if (LineA.GetSegmentType(PolylineSegmentIndex) == SegmentType.Arc)
                    {
                        var Startparam = LineA.GetParameterAtPoint(PolylineSegment.StartPoint);
                        var Endparam = LineA.GetParameterAtPoint(PolylineSegment.EndPoint);
                        MiddlePoint = LineA.GetPointAtParam(Startparam + ((Endparam - Startparam) / 2));
                    }
                    else
                    {
                        MiddlePoint = PolylineSegment.StartPoint.GetMiddlePoint(PolylineSegment.EndPoint);
                    }

                    if (!MiddlePoint.IsInsidePolyline(LineB))
                    {
                        return false;
                    }
                }
                else
                {
                    //No good point found, we run back the function
                    if (NumberOfVertices < ReelNumberOfVertices - 1)
                    {
                        NumberOfVertices++;
                    }
                }
            }
            return true;
        }

        public static bool IsSameAs(this Polyline polylineA, Polyline polylineB)
        {
            if (polylineA.IsDisposed || polylineB.IsDisposed) { return false; }
            if (polylineA.NumberOfVertices != polylineB.NumberOfVertices)
            {
                return false;
            }
            Tolerance tol = Generic.MediumTolerance;

            bool IsClockwisePolyA = polylineA.IsClockwise();
            bool IsClockwisePolyB = polylineB.IsClockwise();
            if (IsClockwisePolyA != IsClockwisePolyB)
            {
                if (IsClockwisePolyA)
                {
                    polylineB.Inverse();
                }
                else
                {
                    polylineB.Inverse();
                }
            }

            for (int i = 0; i < polylineA.GetReelNumberOfVertices(); i++)
            {
                var SegA = polylineA.GetSegmentAt(i);
                var SegB = polylineB.GetSegmentAt(i);
                if (!SegA.StartPoint.IsEqualTo(SegB.StartPoint, tol))
                {
                    return false;
                }

                if (!SegA.EndPoint.IsEqualTo(SegB.EndPoint, tol))
                {
                    return false;
                }

                if (SegA.Bulge != SegB.Bulge)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSegmentIntersecting(this Polyline polyline, Polyline CutLine, out Point3dCollection IntersectionPointsFounds, Intersect intersect)
        {
            IntersectionPointsFounds = new Point3dCollection();
            if (polyline?.IsDisposed != false || CutLine?.IsDisposed != false)
            {
                return false;
            }
            polyline.IntersectWith(CutLine, intersect, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count > 0;
        }

        public static bool ContainsSegment(this Polyline poly, Point3d Start, Point3d End)
        {
            Tolerance tol = Generic.MediumTolerance;
            for (int i = 0; i < poly.GetReelNumberOfVertices(); i++)
            {
                var Seg = poly.GetSegmentAt(i);
                if (Seg.StartPoint.IsEqualTo(Start, tol) && Seg.EndPoint.IsEqualTo(End, tol)) { return true; }
                if (Seg.StartPoint.IsEqualTo(End, tol) && Seg.EndPoint.IsEqualTo(Start, tol)) { return true; }
            }
            return false;
        }

        public static double GetPassingThroughBulgeFrom(this Point3d Through, Point3d Start, Point3d End)
        {
            var MiddlePoint = Start.GetMiddlePoint(End);
            var D1 = MiddlePoint.DistanceTo(Through);
            var D2 = MiddlePoint.DistanceTo(Start);
            return D1 / D2;
        }
    }
}
