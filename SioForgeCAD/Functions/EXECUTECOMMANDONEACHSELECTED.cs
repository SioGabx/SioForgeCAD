using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class EXECUTECOMMANDONEACHSELECTED
    {
        public static async void Execute()
        {
            Editor ed = Generic.GetEditor();
            PromptSelectionResult selectionResult;

            while (true)
            {
                if (!ed.GetImpliedSelection(out selectionResult))
                {
                    selectionResult = ed.GetSelection();
                }
                if (selectionResult.Status == PromptStatus.Cancel)
                {
                    return;
                }
                else if (selectionResult.Status == PromptStatus.OK)
                {
                    break;
                }
                else
                {
                    Generic.WriteMessage("Sélection invalide.");
                }
            }

            var commandPrompt = ed.GetString("Indiquez la commande que vous souhaitez executer sur l'ensemble des éléments séléctionnés");
            if (commandPrompt.Status != PromptStatus.OK) { return; }
            foreach (var EntObjId in selectionResult.Value.GetObjectIds())
            {
                ed.SetImpliedSelection(new ObjectId[1] { EntObjId });
                try
                {
                    await Generic.CommandAsync(commandPrompt.StringResult);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }
    }
}
