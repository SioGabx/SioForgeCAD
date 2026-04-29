using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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
