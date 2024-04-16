using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class POLYCLEAN
    {
        public static void PolyClean()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var poly = ed.GetPolyline("Selectionnez une polyligne");
                if (poly == null)
                {
                    return;
                }
                int NumberOfVerticesBefore = poly.NumberOfVertices;
                poly.UpgradeOpen();
                poly.Cleanup();
                int NumberOfVerticesAfter = poly.NumberOfVertices;
                var NumberOfVerticesDeleted = (NumberOfVerticesBefore - NumberOfVerticesAfter);
                if (NumberOfVerticesDeleted > 0)
                {
                    Generic.WriteMessage($"La polyline à été simplifiée en supprimant {NumberOfVerticesDeleted} point{(NumberOfVerticesDeleted > 1 ? "s" : "")}");
                }
                else
                {
                    Generic.WriteMessage("La polyline est déja optimisée");
                }
                tr.Commit();
            }


        }


    }
}
