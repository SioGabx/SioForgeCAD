using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DBPointExtensions
    {
        public static bool FixNormal(this DBPoint dbPoint)
        {
            if (!dbPoint.Normal.IsEqualTo(Vector3d.ZAxis))
            {
                dbPoint.Normal = Vector3d.ZAxis;
                return true;
            }
            return false;
        }
    }
}
