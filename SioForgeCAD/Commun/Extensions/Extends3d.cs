﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Extends3dExtensions
    {
        public static Point3d TopLeft(this Extents3d extends)
        {
            return new Point3d(extends.MaxPoint.X, extends.MaxPoint.Y, 0);
        }
        public static Point3d TopRight(this Extents3d extends)
        {
            return new Point3d(extends.MinPoint.X, extends.MaxPoint.Y, 0);
        }
        public static Point3d BottomLeft(this Extents3d extends)
        {
            return new Point3d(extends.MaxPoint.X, extends.MinPoint.Y, 0);
        }
        public static Point3d BottomRight(this Extents3d extends)
        {
            return new Point3d(extends.MinPoint.X, extends.MinPoint.Y, 0);
        }


    }
}