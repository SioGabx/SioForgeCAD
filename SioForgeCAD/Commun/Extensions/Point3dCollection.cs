using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Point3dCollectionExtensions
    {
        public static Point3d[] ToArray(this Point3dCollection collection)
        {
            Point3d[] array = new Point3d[collection.Count];
            collection.CopyTo(array, 0);
            return array;
        }

    }
}
