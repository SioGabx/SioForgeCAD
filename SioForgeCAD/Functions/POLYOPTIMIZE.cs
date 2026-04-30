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

    public static class POLYOPTIMIZE
    {
        public static void PolyOptimize()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            var PolySelection = ed.GetSelectionRedraw("Selectionnez une polyligne", true, false);
            if (PolySelection.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var polyObjId in PolySelection.Value.GetObjectIds())
                {
                    if (polyObjId.GetDBObject() is Polyline poly)
                    {
                        if (poly.IsEntityOnLockedLayer()) { continue; }
                        int NumberOfVerticesBefore = poly.NumberOfVertices;
                        poly.UpgradeOpen();
                        poly.OptimizeFacetsToArcs(.01);
                        int NumberOfVerticesAfter = poly.NumberOfVertices;
                        var NumberOfVerticesDeleted = NumberOfVerticesBefore - NumberOfVerticesAfter;
                        if (NumberOfVerticesDeleted > 0)
                        {
                            Generic.WriteMessage($"La polyline à été simplifiée en supprimant {NumberOfVerticesDeleted} point{(NumberOfVerticesDeleted > 1 ? "s" : "")}");
                        }
                        else if (NumberOfVerticesDeleted <= 0)
                        {
                            Generic.WriteMessage("La polyline est déja optimisée");
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}

