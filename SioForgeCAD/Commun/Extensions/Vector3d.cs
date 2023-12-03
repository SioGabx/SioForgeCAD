using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System.Collections.Generic;
using System.Linq;

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

    }
}
