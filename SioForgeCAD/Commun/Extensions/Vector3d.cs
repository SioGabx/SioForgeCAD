using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Vector3dExtensions
    {
        public static void DrawVector(this Vector3d vector3d, Point3d startPoint, int ColorIndex = 0)
        {
            Point3d vectorEndPoint = startPoint.Add(vector3d);
            Line vectorLine = new Line(startPoint, vectorEndPoint);
            Lines.Draw(vectorLine, ColorIndex);
        }

        public static Vector3d SetLength(this Vector3d vector3d, double Length)
        {
            return vector3d.GetNormal().MultiplyBy(Length);
        }

        public static bool IsVectorOnRightSide(this Vector3d vectorToCheckSide, Vector3d referenceVector)
        {
            // Normaliser les vecteurs
            vectorToCheckSide = vectorToCheckSide.GetNormal();
            referenceVector = referenceVector.GetNormal();
            Vector3d crossProduct = vectorToCheckSide.CrossProduct(referenceVector);
            // Vérifier la composante Z du produit vectoriel pour déterminer l'orientation
            return crossProduct.Z >= 0;
        }

        public static Vector3d Inverse(this Vector3d vector3D)
        {
            return vector3D.MultiplyBy(-1);
        }

        public static Vector2d Inverse(this Vector2d vector2D)
        {
            return vector2D.MultiplyBy(-1);
        }

        public static double GetRotationRelativeToSCG(this Vector3d vector)
        {
            Vector2d xAxisWCS = new Vector2d(0, 1);
            var dot = DotProduct(vector.ToVector2d(), xAxisWCS); //vector.X * xAxisWCS.X + vector.Y * xAxisWCS.Y;      // Dot product between [x1, y1] and [x2, y2]
            var det = CrossProduct(vector.ToVector2d(), xAxisWCS); //vector.X * xAxisWCS.Y - vector.Y * xAxisWCS.X;     
            var angle = Math.Atan2(det, dot);
            double angleDegrees = angle * (180.0 / Math.PI);
            angleDegrees = (angleDegrees < 0) ? (360.0 + angleDegrees) : angleDegrees;
            return angleDegrees;
        }

        public static Vector2d ToVector2d(this Vector3d vector)
        {
            return new Vector2d(vector.X, vector.Y);
        }


        /// <summary>
        /// Gets the dot produc of two Vector2ds.
        /// </summary>
        /// <param name="v1">The vector 1.</param>
        /// <param name="v2">The vector 2.</param>
        /// <returns>The dot product.</returns>
        public static double DotProduct(this Vector2d v1, Vector2d v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        /// <summary>
        /// Gets the cross produc of two Vector2ds.
        /// </summary>
        /// <param name="v1">The vector 1.</param>
        /// <param name="v2">The vector 2.</param>
        /// <returns>The cross product.</returns>
        public static double CrossProduct(this Vector2d v1, Vector2d v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        public static bool IsColinear(this Vector3d v1, Vector3d v2, Tolerance tol)
        {
            return IsColinear(ToVector2d(v1), ToVector2d(v2), tol);
        }

        public static bool IsColinear(this Vector2d v1, Vector2d v2, Tolerance tol)
        {
            return Math.Abs(v1.CrossProduct(v2)) < tol.EqualPoint;
        }

        public static Point3d FindProjectedIntersection(this Vector3d FirstVector, Point3d FirstVectorBasePoint, Vector3d SecondVector, Point3d SecondVectorBasePoint)
        {
            Vector3d deltaStartPoints = FirstVectorBasePoint - SecondVectorBasePoint;
            double a = FirstVector.DotProduct(FirstVector);
            double b = FirstVector.DotProduct(SecondVector);
            double c = SecondVector.DotProduct(SecondVector);
            double d = FirstVector.DotProduct(deltaStartPoints);
            double e = SecondVector.DotProduct(deltaStartPoints);
            double s = (a * e - b * d) / (a * c - b * b);
            return SecondVectorBasePoint + s * SecondVector;
        }

    }
}
