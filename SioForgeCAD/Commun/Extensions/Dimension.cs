using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DimensionExtensions
    {
        public static bool FixNormal(this Dimension dim)
        {
            if (!dim.Normal.IsEqualTo(Vector3d.ZAxis))
            {
                dim.Normal = Vector3d.ZAxis;
                return true;
            }
            return false;
        }
    }
}
