using Autodesk.AutoCAD.DatabaseServices;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ColorsEntensions
    {
        public static System.Drawing.Color GetSystemDrawingColor(this Entity ent)
        {
            return ent.Color.ColorValue;
        }
    }
}
