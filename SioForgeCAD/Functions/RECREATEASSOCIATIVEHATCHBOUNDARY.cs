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
    public static class RECREATEASSOCIATIVEHATCHBOUNDARY
    {
        public static void Recreate()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            var SelectionResult = ed.GetSelectionRedraw("Selectionnez des hachures");
            if (SelectionResult.Status != PromptStatus.OK) { return; }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var CurrentLayer = Layers.GetCurrentLayerName();
                foreach (var item in SelectionResult.Value.GetObjectIds())
                {
                    if (item.GetDBObject() is Hatch hatch && !hatch.Associative)
                    {
                        Layers.SetCurrentLayerName(hatch.Layer);
                        Generic.Command("_-HATCHEDIT", hatch.ObjectId, "_Boundary", "_Polyline", "_Yes");
                    }
                }
                Layers.SetCurrentLayerName(CurrentLayer);
                tr.Commit();
            }
        }
    }
}
