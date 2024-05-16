using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class EXECUTECOMMANDONEACHSELECTED
    {
        public async static void Execute()
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
