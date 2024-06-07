using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ColorsEntensions
    {
        public static System.Drawing.Color GetSystemDrawingColor(this Entity ent)
        {
            return ent.Color.ColorValue;
        }

        public static Color GetColor(this Entity ent)
        {
            return ent.Color;
        }
    }
}
