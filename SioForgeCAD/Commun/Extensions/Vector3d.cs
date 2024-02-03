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

        public static double GetRotationRelativeToSCG(this Vector3d vector)
        {
            Vector3d xAxisWCS = new Vector3d(0, 1, 0);
            var dot = vector.X * xAxisWCS.X + vector.Y * xAxisWCS.Y;      // Dot product between [x1, y1] and [x2, y2]
            var det = vector.X * xAxisWCS.Y - vector.Y * xAxisWCS.X;      //Determinant
            var angle = Math.Atan2(det, dot);  //atan2(y, x) or atan2(sin, cos)
            double angleDegrees = angle * (180.0 / Math.PI);
            angleDegrees = (angleDegrees < 0) ? (360.0 + angleDegrees) : angleDegrees;
            return angleDegrees;
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
