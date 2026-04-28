using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class TableExtensions
    {
        public static bool FixNormal(this Table table)
        {
            if (!table.Normal.IsEqualTo(Vector3d.ZAxis))
            {
                table.Normal = Vector3d.ZAxis;
                return true;
            }
            return false;
        }
    }
}
