using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class POLYISCLOCKWISE
    {
        public static void Check()
        {
            var ed = Generic.GetEditor();
            using (var result = ed.GetPolyline("Selectionnez une polyligne"))
            {
                if (result == null)
                {
                    return;
                }
                Generic.WriteMessage($"L'orientation de la polyline est {(result.IsClockwise() ? "CLOCKWISE" : "ANTICLOCKWISE")}");
            }
        }
    }
}
