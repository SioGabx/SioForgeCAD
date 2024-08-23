using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class HATCHRECREATEMISSINGBOUNDARIES
    {
        public static void Recreate()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            var SelectionResult = ed.GetSelectionRedraw("Selectionnez des hachures");
            if (SelectionResult.Status != PromptStatus.OK) { return; }

            List<ObjectId> FinalSelectedObjects = new List<ObjectId>();
            int SuccessfulyAddedBoundary = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var CurrentLayer = Layers.GetCurrentLayerName();
                foreach (var item in SelectionResult.Value.GetObjectIds())
                {

                    if (item.GetDBObject() is Hatch hatch)
                    {
                        var HasBoundary = hatch.GetAssociatedObjectIds().Count > 0;
                        if (HasBoundary)
                        {
                            foreach (ObjectId Item in hatch.GetAssociatedObjectIds())
                            {
                               if (Item.IsNull ||!Item.IsValid ||!Item.IsWellBehaved || Item.IsErased || Item.IsEffectivelyErased)
                                {
                                    HasBoundary = false;
                                }
                            }
                        }
                        if (!hatch.Associative || !HasBoundary)
                        {
                            Layers.SetCurrentLayerName(hatch.Layer);
                            Generic.Command("_-HATCHEDIT", hatch.ObjectId, "_Boundary", "_Polyline", "_Yes");
                            FinalSelectedObjects.Add(hatch.ObjectId);
                            FinalSelectedObjects.AddRange(hatch.GetAssociatedObjectIds().ToList());
                            SuccessfulyAddedBoundary++;
                        }
                    }
                }
                Generic.WriteMessage(SuccessfulyAddedBoundary > 0 ? $"Ajout de contours sur {SuccessfulyAddedBoundary} hachure(s)" : "Aucun ajout de nouveau contours");
                ed.SetImpliedSelection(FinalSelectedObjects.ToArray());
                Layers.SetCurrentLayerName(CurrentLayer);
                tr.Commit();
            }
        }
    }
}
