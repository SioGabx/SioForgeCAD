using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            if (point.DistanceTo(cv.StartPoint) < Tolerance.Global.EqualPoint)
            {
                return 0.0;
            }
            else if (point.DistanceTo(cv.EndPoint) < Tolerance.Global.EqualPoint)
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

    }
}
