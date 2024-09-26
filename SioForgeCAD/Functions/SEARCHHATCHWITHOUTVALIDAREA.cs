using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class SEARCHHATCHWITHOUTVALIDAREA
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
                List<ObjectId> NoAreaObjects = new List<ObjectId>();
                foreach (ObjectId ObjId in AllSearchObjectIds)
                {
                    var DBObject = ObjId.GetDBObject();
                    if (DBObject is Hatch HatchEnt)
                    {
                        var ObjectArea = HatchEnt.TryGetArea();
                        if (ObjectArea <= 0)
                        {
                            NoAreaObjects.Add(ObjId);
                        }
                    }
                }
                if (NoAreaObjects.Count > 0)
                {
                    Generic.WriteMessage($"{NoAreaObjects.Count} hachure(s) sans aire ont été selectionnés");
                    ed.SetImpliedSelection(NoAreaObjects.ToArray());
                }
                else
                {
                    Generic.WriteMessage($"Aucune hachure sans aire ont été détéctés");
                }


                tr.Commit();
            }
        }
    }
}
