using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ColorsEntensions
    {
        public static System.Drawing.Color GetSystemColor(this Entity ent)
        {
            return ent.Color.ColorValue;
        }



    }
}
