using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public class PolyHole
    {
        public Polyline Boundary;
        public List<Polyline> Holes;

        public PolyHole(Polyline boundary, List<Polyline> holes)
        {
            Boundary = boundary;
            Holes = holes;
        }

        public static List<PolyHole> CreateFromList(IEnumerable<Polyline> polylines)
        {
            List<PolyHole> polyholes = new List<PolyHole>();
            foreach (var poly in polylines)
            {
                polyholes.Add(new PolyHole(poly, new List<Polyline>()));
            }
            return polyholes;
        }
    }

    public static class PolylinesExtensions
    {
        public static int GetReelNumberOfVertices(this Polyline TargetPolyline)
        {
            int NumberOfVertices = (TargetPolyline.NumberOfVertices - 1);
            if (TargetPolyline.Closed)
            {
                NumberOfVertices++;
            }
            return NumberOfVertices;
        }

        public static (Point3d StartPoint, Point3d EndPoint) GetSegmentAt(this Polyline TargetPolyline, int Index)
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
                area += Point3dExtensions.GetArea(p0, pline.GetPoint2dAt(i), pline.GetPoint2dAt(i + 1));
                if (pline.GetBulgeAt(i) != 0.0)
                {
                    area += pline.GetArcSegment2dAt(i).GetArea(); ;
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

        public static void Cleanup(this Polyline polyline)
        {
            int InverseCount = 0;
            void InversePoly()
            {
                InverseCount++;
                polyline.Inverse();
            }

            if (polyline == null) { return; }
            int vertexCount = polyline.NumberOfVertices;
            if (vertexCount <= 2) { return; }

            bool HasAVertexRemoved = true;
            while (HasAVertexRemoved)
            {
                InversePoly();
                HasAVertexRemoved = false;
                int index = 1;
                while ((polyline.GetReelNumberOfVertices()) > index)
                {
                    Point3d lastPoint = polyline.GetPoint3dAt(index - 1);
                    Point3d currentPoint = polyline.GetPoint3dAt(index);
                    Point3d nextPoint;
                    if (polyline.GetReelNumberOfVertices() <= index + 1)
                    {
                        nextPoint = polyline.StartPoint;
                    }
                    else
                    {
                        nextPoint = polyline.GetPoint3dAt(index + 1);
                    }

                    Vector2d vector1 = currentPoint.GetVectorTo(lastPoint).ToVector2d();
                    Vector2d vector2 = nextPoint.GetVectorTo(currentPoint).ToVector2d();

                    bool IsColinear = vector1.IsColinear(vector2, Generic.MediumTolerance);
                    var HasBulge = polyline.GetSegmentType(index) == SegmentType.Arc;
                    if (!HasBulge && (IsColinear || currentPoint.IsEqualTo(nextPoint, Generic.LowTolerance)))
                    {
                        polyline.RemoveVertexAt(index);
                        HasAVertexRemoved = true;
                    }
                    else
                    {
                        index++;
                    }
                }


                index = 0;
                while (index < polyline.GetReelNumberOfVertices())
                {
                    try
                    {
                        var seg = polyline.GetSegmentAt(index);
                        if (seg.StartPoint.IsEqualTo(seg.EndPoint, Generic.LowTolerance))
                        {
                            polyline.RemoveVertexAt(index);
                            HasAVertexRemoved = true;
                        }
                        else
                        {
                            index++;
                        }
                    }
                    catch (Exception)
                    {
                        index++;
                    }

                }

            }
            if (InverseCount % 2 != 0)
            {
                InversePoly();
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
                    spline = (Spline)Curve.CreateFromGeCurve(nurb);
                else
                    using (var spl = (Spline)Curve.CreateFromGeCurve(nurb))
                        spline.JoinEntity(spl);
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

        public static Polyline ToPolygon(this Polyline poly, uint NumberOfVertexPerArc = 0)
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

                using (var Spline = poly.GetSpline())
                {
                    Polyline Resultpoly;
                    if (NumberOfVertexPerArc > 0)
                    {
                        Resultpoly = Spline.ToPolyline(NumberOfVertex, false, true) as Polyline;
                    }
                    else
                    {
                        Resultpoly = Spline.ToPolyline() as Polyline;
                    }
                    Resultpoly.Cleanup();

                    return Resultpoly;
                }
            }
            return poly;
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
            Poly.AddVertexAt(Poly.NumberOfVertices, point.ToPoint2d(), bulge, startWidth, endWidth);
        }

        public static void AddVertex(this Polyline3d Poly, Point3d point)
        {
            var Vertex = new PolylineVertex3d(point);
            Poly.AppendVertex(Vertex);
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
            double sum = 0;
            for (var i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var cur = poly.GetPoint2dAt(i);
                var next = poly.GetPoint2dAt(i + 1);
                sum += (next.X - cur.X) * (next.Y + cur.Y);
            }
            return sum > 0;
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
                foreach (ObjectId vertexId in poly3d)
                {
                    PolylineVertex3d vertex = vertexId.GetDBObject() as PolylineVertex3d;
                    if (vertex != null)
                    {
                        Point2d point = new Point2d(vertex.Position.X, vertex.Position.Y);
                        poly2d.AddVertexAt(poly2d.NumberOfVertices, point, 0, 0, 0);
                    }
                }
                return poly2d;
            }
            else
            {
                return poly3d.Spline.ToPolyline() as Polyline;
            }
        }

        public static Polyline ToPolyline(this Polyline2d poly2d)
        {
            if (poly2d.PolyType == Poly2dType.QuadSplinePoly || poly2d.PolyType == Poly2dType.CubicSplinePoly)
            {
                return poly2d.Spline.ToPolyline() as Polyline;
            }
            Polyline poly = new Polyline();
            poly.ConvertFrom(poly2d, false);
            return poly;
        }

        public static Point3d GetInnerCentroid(this Polyline poly)
        {
            var polygon = poly.ToPolygon(10);
            return PolygonOperation.GetInnerCentroid(polygon, 5);
        }

        public static bool IsOverlaping(this Polyline LineA, Polyline LineB)
        {
            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < LineA.GetReelNumberOfVertices(); PolylineSegmentIndex++)
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
            if (CheckEach)
            {
                NumberOfVertices = LineA.GetReelNumberOfVertices();
            }

            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < NumberOfVertices; PolylineSegmentIndex++)
            {
                var PolylineSegment = LineA.GetSegmentAt(PolylineSegmentIndex);
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

                if ((PolylineSegment.StartPoint.DistanceTo(PolylineSegment.EndPoint) / 2) > Generic.MediumTolerance.EqualPoint)
                {
                    if (!MiddlePoint.IsInsidePolyline(LineB))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool IsSegmentIntersecting(this Polyline polyline, Polyline CutLine, out Point3dCollection IntersectionPointsFounds, Intersect intersect = Intersect.OnBothOperands)
        {
            IntersectionPointsFounds = new Point3dCollection();
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


      



    }
}

