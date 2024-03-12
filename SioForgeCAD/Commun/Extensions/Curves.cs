using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Commun.Extensions
{
    public static class CurvesExtensions
    {
        /// <summary>
        /// Gets the parameter at a specified point on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="point">The point.</param>
        /// <returns>The parameter.</returns>
        public static double GetParamAtPointX(this Curve cv, Point3d point)
        {
            if (point.DistanceTo(cv.StartPoint) < Generic.MediumTolerance.EqualPoint)
            {
                return 0.0;
            }
            else if (point.DistanceTo(cv.EndPoint) < Generic.MediumTolerance.EqualPoint)
            {
                return cv.GetParameterAtPoint(cv.EndPoint);
            }
            else
            {
                try
                {
                    return cv.GetParameterAtPoint(point);
                }
                catch
                {
                    return cv.GetParameterAtPoint(cv.GetClosestPointTo(point, false));
                }
            }
        }





        /// <summary>
        /// Gets the point at a specified parameter on curve.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="param">The parameter.</param>
        /// <returns>The point.</returns>
        public static Point3d GetPointAtParam(this Curve cv, double param)
        {
            if (param < 0)
            {
                param = 0;
            }
            else if (param > cv.EndParam)
            {
                param = cv.EndParam;
            }
            return cv.GetPointAtParameter(param);
        }


        /// <summary>
        /// Gets all points on curve whose parameters are an arithmetic sequence starting from 0.
        /// </summary>
        /// <param name="cv">The curve.</param>
        /// <param name="paramDelta">The parameter increment. Th default is 1, in which case the method returns all points on curve whose parameters are integres.</param>
        /// <returns>The points.</returns>
        public static IEnumerable<Point3d> GetPoints(this Curve cv, double paramDelta = 1)
        {
            for (var param = 0d; param <= cv.EndParam; param += paramDelta)
            {
                yield return cv.GetPointAtParam(param);
            }
        }




        /// <summary>
        /// Order the collection by contiguous curves ([n].EndPoint equals to [n+1].StartPoint)
        /// </summary>
        /// <param name="source">Collection this method applies to.</param>
        /// <returns>Ordered array of Curve3d.</returns>
        public static Curve3d[] ToOrderedArray(this IEnumerable<Curve3d> source)
        {
            var list = source.ToList();
            int count = list.Count;
            var array = new Curve3d[count];
            int i = 0;
            array[0] = list[0];
            list.RemoveAt(0);
            int index;
            while (i < count - 1)
            {
                var pt = array[i++].EndPoint;
                if ((index = list.FindIndex(c => c.StartPoint.IsEqualTo(pt))) != -1)
                {

                    array[i] = list[index];
                }
                else if ((index = list.FindIndex(c => c.EndPoint.IsEqualTo(pt))) != -1)
                {
                    array[i] = list[index].GetReverseParameterCurve();
                }
                else
                {
                    Debug.WriteLine("Not contiguous curves.");
                    return new Curve3d[0];
                }
                list.RemoveAt(index);
            }
            return array;
        }


        public static List<Curve> OffsetPolyline(this IEnumerable<Curve> Curves, double OffsetDistance, bool OffsetToInside)
        {
            List<Curve> OffsetCurves = new List<Curve>();
            if (OffsetToInside)
            {
                OffsetDistance = -Math.Abs(OffsetDistance);
            }
            foreach (var ent in Curves)
            {
                DBObjectCollection OffsetCurve = new DBObjectCollection();
                if (ent is Polyline)
                {
                    OffsetCurve = ent.GetOffsetCurves((ent as Polyline).GetArea() < 0.0 ? -OffsetDistance : OffsetDistance);
                }
                else if (ent is Ellipse || ent is Circle)
                {
                    OffsetCurve = ent.GetOffsetCurves(OffsetDistance);
                }

                OffsetCurves.AddRange(OffsetCurve.ToList().Cast<Curve>());
            }
            Curves.DeepDispose();
            return OffsetCurves;
        }


        public static bool IsSelfIntersecting(this Curve poly, out Point3dCollection IntersectionFound)
        {
            IntersectionFound = new Point3dCollection();
            DBObjectCollection entities = new DBObjectCollection();
            poly.Explode(entities);
            for (int i = 0; i < entities.Count; ++i)
            {
                for (int j = i + 1; j < entities.Count; ++j)
                {
                    Curve curve1 = entities[i] as Curve;
                    Curve curve2 = entities[j] as Curve;
                    Point3dCollection points = new Point3dCollection();
                    curve1.IntersectWith(curve2, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);

                    foreach (Point3d point in points)
                    {
                        // Make a check to skip the start/end points
                        // since they are connected vertices
                        if (point == curve1.StartPoint || point == curve1.EndPoint)
                        {
                            if (point == curve2.StartPoint || point == curve2.EndPoint)
                            {
                                continue;
                            }
                        }

                        // If two consecutive segments, then skip
                        if (j == i + 1)
                        {
                            continue;
                        }
                        IntersectionFound.Add(point);
                    }

                }
                // Need to be disposed explicitely
                // since entities are not DB resident
                entities[i].Dispose();
            }

            if (IntersectionFound.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        public static bool CanBeJoinWith(this Curve A, Curve B)
        {
            if (A == B) { return false; }
            if (A.Closed || B.Closed)
            {
                return false;
            }

            if (A.IsCurveCanClose(B))
            {
                //Check if the polyline is already joined
                IEnumerable<Point3d> PAPoint = A.GetPoints();
                var PAPointList = PAPoint.ToList();
                if (A.StartPoint.DistanceTo(A.EndPoint) > Generic.MediumTolerance.EqualPoint)
                {
                    PAPointList.Remove(A.StartPoint);
                    PAPointList.Remove(A.EndPoint);
                }

                IEnumerable<Point3d> PBPoint = B.GetPoints();

                if (PAPointList.ContainsAll(PBPoint))
                {
                    return false;
                }
            }

            if (!A.HasEndPointOrStartPointInCommun(B))
            {
                return false;
            }
            return true;
        }

        public static bool IsCurveCanClose(this Curve PolyA, Curve PolyB)
        {
            Point3d StartPointA = PolyA.StartPoint.Flatten();
            Point3d EndPointA = PolyA.EndPoint.Flatten();

            Point3d StartPointB = PolyB.StartPoint.Flatten();
            Point3d EndPointB = PolyB.EndPoint.Flatten();
            return (StartPointA.IsEqualTo(StartPointB, Generic.LowTolerance) && EndPointA.IsEqualTo(EndPointB, Generic.LowTolerance)) ||
                 (StartPointA.IsEqualTo(EndPointB, Generic.LowTolerance) && EndPointA.IsEqualTo(StartPointB, Generic.LowTolerance));
        }

        public static bool HasEndPointOrStartPointInCommun(this Curve A, Curve B)
        {
            if (A == null || B == null) return false;

            if (A.EndPoint.IsEqualTo(B.EndPoint, Generic.LowTolerance)) return true;
            if (A.EndPoint.IsEqualTo(B.StartPoint, Generic.LowTolerance)) return true;
            if (A.StartPoint.IsEqualTo(B.EndPoint, Generic.LowTolerance)) return true;
            if (A.StartPoint.IsEqualTo(B.StartPoint, Generic.LowTolerance)) return true;

            return false;
        }



        public static List<Curve> Join(this IEnumerable<Curve> Curves)
        {
            List<Curve> entities = Curves.ToList();
            for (int i = 0; i < entities.Count; i++)
            {
                var JoignableEnt = entities[i].GetJoinableCurve();
                entities[i] = JoignableEnt;
            }


            for (int i = entities.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    try
                    {
                        // check if start/endpoints are the same
                        // if they are join them and reset the loops and start again
                        Curve srcCurve = entities[i];
                        Curve addCurve = entities[j];

                        if (srcCurve.CanBeJoinWith(addCurve))
                        {
                            if (addCurve is Spline && !(srcCurve is Spline))
                            {
                                addCurve.JoinEntity(srcCurve);
                                entities.RemoveAt(i);
                                srcCurve.Dispose();
                            }
                            else
                            {
                                srcCurve.JoinEntity(addCurve);
                                entities.RemoveAt(j);
                                addCurve.Dispose();
                            }


                            // reset i to the start (as it has changed)
                            i = entities.Count;
                            j = 0;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine("\nError: n{0}", ex.Message);
                    }
                }
            }

            return entities;
        }


        private static Curve GetJoinableCurve(this Curve srcCurve)
        {
            if (srcCurve is Line srcPolylineType)
            {
                return srcPolylineType.ToPolyline();
            }
            if (srcCurve is Arc srcPArcType)
            {
                return srcPArcType.ToPolyline();
            }

            if (srcCurve is Ellipse srcPEllipseType)
            {
                return srcPEllipseType.Spline;
            }
            return srcCurve;
        }



        public static List<Curve> RegionMerge(this IEnumerable<Curve> Curves)
        {
            DBObjectCollection reg;
            try
            {
                var CurvesCollection = Curves.ToDBObjectCollection();
                foreach (var ent in Curves.ToArray())
                {
                    if (ent is Polyline polyline && polyline.IsSelfIntersecting(out Point3dCollection IntersectionFound))
                    {
                        Generic.WriteMessage("Jeux de selection incorrect : une ou plusieurs polylignes se coupent elles-même");
                    }
                }
                reg = Region.CreateFromCurves(CurvesCollection);
            }
            catch (Exception e)
            {
                Generic.WriteMessage("Impossible de combiner les hachures");
                Debug.WriteLine(e);
                return new List<Curve>();
            }
            if (reg.Count > 0)
            {
                Region RegionZero = reg[0] as Region;
                for (int i = 1; i < reg.Count; i++)
                {
                    RegionZero.BooleanOperation(BooleanOperationType.BoolUnite, reg[i] as Region);
                }
                var MergedCurves = RegionZero.GetPolylines();
                return MergedCurves.Cast<Curve>().ToList();
            }
            else
            {
                return Curves.ToList();
            }
        }




    }
}
