using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
