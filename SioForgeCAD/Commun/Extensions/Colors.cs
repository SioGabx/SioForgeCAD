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

        public static Color ConvertColorToGray(this Color BaseColor)
        {
            var DrawingColor = BaseColor.ColorValue;
            byte Gray = (byte)((0.2989 * DrawingColor.R) + (0.5870 * DrawingColor.G) + (0.1140 * DrawingColor.B));
            return Color.FromRgb(Gray, Gray, Gray);
        }
    }
}
