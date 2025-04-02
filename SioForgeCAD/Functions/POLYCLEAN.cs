using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class POLYCLEAN
    {
        public static void PolyClean()
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
                        bool WasNormalFixed = poly.FixNormals();
                        poly.Cleanup();
                        int NumberOfVerticesAfter = poly.NumberOfVertices;
                        var NumberOfVerticesDeleted = (NumberOfVerticesBefore - NumberOfVerticesAfter);
                        if (NumberOfVerticesDeleted > 0)
                        {
                            Generic.WriteMessage($"La polyline à été simplifiée en supprimant {NumberOfVerticesDeleted} point{(NumberOfVerticesDeleted > 1 ? "s" : "")}");
                        }
                        else if (NumberOfVerticesDeleted <= 0 && !WasNormalFixed)
                        {
                            Generic.WriteMessage("La polyline est déja optimisée");
                        }
                        if (WasNormalFixed)
                        {
                            Generic.WriteMessage("La normal de la polyline à été réparée");
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}
