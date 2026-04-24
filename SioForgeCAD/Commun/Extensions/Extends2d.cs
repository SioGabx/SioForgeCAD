using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Extends2dExtensions
    {
        public static Extents3d ToExtents3d(this Extents2d ext)
        {
            return new Extents3d(new Point3d(ext.MinPoint.X, ext.MinPoint.Y, 0), new Point3d(ext.MaxPoint.X, ext.MaxPoint.Y, 0));
        }
    }
}
