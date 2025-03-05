using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.LayerManager;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER
    {
        public static void Search()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();


            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                var Opts = new PromptSelectionOptions()
                {
                    MessageForAdding = "Selectionnez des hachures en particulier ou ignorer et rechercher dans tout le dessin",
                    RejectObjectsOnLockedLayers = false,
                };
                AllSelectedObject = ed.GetSelection(Opts);
            }
            ObjectId[] AllSearchObjectIds = Array.Empty<ObjectId>();
            if (AllSelectedObject.Status == PromptStatus.OK)
            {
                AllSearchObjectIds = AllSelectedObject.Value.GetObjectIds();
            }

            if (AllSearchObjectIds.Length == 0)
            {
                var AllObject = ed.SelectAll();
                if (AllSelectedObject.Status.HasFlag(PromptStatus.OK))
                {
                    return;
                }
                AllSearchObjectIds = AllObject.Value.GetObjectIds();
            }

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                Dictionary<ObjectId, List<ObjectId>> NotSameLayer = new Dictionary<ObjectId, List<ObjectId>>();
                foreach (ObjectId ObjId in AllSearchObjectIds)
                {
                    if (ObjId.GetDBObject() is Hatch HatchEnt)
                    {
                        var HasBoundary = HatchEnt.GetAssociatedObjectIds().Count > 0;
                        if (HasBoundary)
                        {
                            foreach (ObjectId Item in HatchEnt.GetAssociatedObjectIds())
                            {
                                if (!Item.IsNull && Item.IsValid && Item.IsWellBehaved && !Item.IsErased && !Item.IsEffectivelyErased)
                                {
                                    if (Item.GetNoTransactionDBObject() is Entity Boundary)
                                    {
                                        if (Boundary.Layer != HatchEnt.Layer)
                                        {
                                            if (!NotSameLayer.TryGetValue(ObjId, out List<ObjectId> value))
                                            {
                                                value = new List<ObjectId>();
                                                NotSameLayer[ObjId] = value;
                                            }

                                            value.Add(Item);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (NotSameLayer.Count > 0)
                {
                    Generic.WriteMessage($"{NotSameLayer.Count} hachure(s) ont des contours sur un ou des mauvais calques");
                    ed.SetImpliedSelection(NotSameLayer.Values.SelectMany(list => list).ToArray());

                    PromptKeywordOptions options = new PromptKeywordOptions($"\n{NotSameLayer.Count} hachure(s) ont des contours sur un ou des mauvais calques.\nVoullez-vous corriger ces erreurs ?");
                    options.Keywords.Add("Oui");
                    options.Keywords.Add("Non");
                    options.Keywords.Default = "Oui";
                    options.AllowNone = false;
                  
                    PromptResult result = ed.GetKeywords(options);

                    if (result.Status == PromptStatus.OK && result.StringResult == "Oui")
                    {
                        Fix(NotSameLayer);
                    }
                }
                else
                {
                    Generic.WriteMessage("Aucune hachure avec contours sur un ou des mauvais calques ont été détéctés");
                }
                tr.Commit();
            }
        }

        public static void Fix(Dictionary<ObjectId, List<ObjectId>> NotSameLayer)
        {
            foreach (var pair in NotSameLayer)
            {
                List<ObjectId> objectsToMove = pair.Value;
                string targetLayerName = (pair.Key.GetNoTransactionDBObject() as Entity).Layer;

                foreach (ObjectId objId in objectsToMove)
                {
                    Entity ent = objId.GetNoTransactionDBObject() as Entity;
                    if (ent != null)
                    {
                        ent.UpgradeOpen();
                        ent.Layer = targetLayerName;
                        ent.DowngradeOpen();
                    }
                }
            }
        }


    }
}
