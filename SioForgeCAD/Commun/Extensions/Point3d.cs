using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Point3dExtensions
    {
        public static Point2d ToPoint2d(this Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }  
        public static Point3d ToPoint3d(this Point2d p)
        {
            return new Point3d(p.X, p.Y, 0);
        } 
        public static Point3d Flatten(this Point3d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }

        public static Points ToPoints(this Point3d p)
        {
            return Points.From3DPoint(p);
        }

    }
}
