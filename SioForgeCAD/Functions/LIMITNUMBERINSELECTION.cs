using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class LIMITNUMBERINSELECTION
    {
        public static void Limit()
        {
            Editor ed = Generic.GetEditor();
            var Select = ed.GetSelectionRedraw();
            if (Select.Status == PromptStatus.OK)
            {
                var SelectedObjIds = Select.Value.GetObjectIds();


               
                int? PromptNumberToSelectResult = ed.GetIntegerInRange("\nCombien d'entités voulez-vous selectionner ?", 0, SelectedObjIds.Length, SelectedObjIds.Length);

                if (!(PromptNumberToSelectResult is int NumberToSelect)) {
                    return;
                }
                
                Random Rand = new Random();
                ObjectId[] ImpliedSelection = SelectedObjIds.OrderBy(_ => Rand.Next()).Take(NumberToSelect).ToArray();

                if (SelectedObjIds.Length > 0)
                {
                    ed.SetImpliedSelection(ImpliedSelection);
                }
            }
        }
    }
}
