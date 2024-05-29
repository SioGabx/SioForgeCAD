using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class TAREA
    {
        public static void Compute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();

            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                AllSelectedObject = ed.GetSelection();
            }

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }

            var AllSelectedObjectIds = AllSelectedObject.Value.GetObjectIds();
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                List<ObjectId> NoAreaObjects = new List<ObjectId>();
                double TotalArea = 0;
                foreach (ObjectId ObjId in AllSelectedObjectIds)
                {
                    var DBObject = ObjId.GetDBObject();
                    if (DBObject is Entity Ent)
                    {
                        var ObjectArea = Ent.TryGetArea();
                        TotalArea += ObjectArea;
                        if (ObjectArea <= 0)
                        {
                            NoAreaObjects.Add(ObjId);
                        }
                    }
                }
                short DisplayPrecision = (short)Application.GetSystemVariable("LUPREC");
                string NoValidAreaObjectMessage = $"\n\nAttention : Certain(s) objet(s) sélectionné(s) n'ont pas d'aire valide. Ils ont été exclus de la sélection";
                var Message = $"L'aire totale des objets est égale à {Math.Round(TotalArea, DisplayPrecision)}{((NoAreaObjects.Count > 0) ? NoValidAreaObjectMessage : "")}";

                Generic.WriteMessage(Message);
                Application.ShowAlertDialog(Message);
                System.Windows.Clipboard.SetText(TotalArea.ToString());
                var AllValidAreaObjectIds = AllSelectedObjectIds.RemoveCommun(NoAreaObjects);
                ed.SetImpliedSelection(AllValidAreaObjectIds.ToArray());
                tr.Commit();
            }
        }
    }
}
