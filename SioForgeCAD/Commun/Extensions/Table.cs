using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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
