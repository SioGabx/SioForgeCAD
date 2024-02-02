using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Vector3dExtensions
    {
        public static void DrawVector(this Vector3d vector3d, Point3d startPoint)
        {
            Point3d vectorEndPoint = startPoint.Add(vector3d);
            Line vectorLine = new Line(startPoint, vectorEndPoint);
            Lines.Draw(vectorLine);
        }

        public static Vector3d SetLength(this Vector3d vector3d, double Length)
        {
            return vector3d.GetNormal().MultiplyBy(Length);
        }

    }
}
