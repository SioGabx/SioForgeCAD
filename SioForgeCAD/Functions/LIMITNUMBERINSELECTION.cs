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

                PromptIntegerOptions PromptNumberToSelectOptions = new PromptIntegerOptions("\nCombien d'entités voulez-vous selectionner ?")
                {
                    LowerLimit = 1,
                    UpperLimit = SelectedObjIds.Length,
                    DefaultValue = Convert.ToInt32(Math.Round((double)(SelectedObjIds.Length), 0)),
                    AllowNone = false
                };
                PromptIntegerResult PromptNumberToSelect = ed.GetInteger(PromptNumberToSelectOptions);
                if (PromptNumberToSelect.Status != PromptStatus.OK) { return; }
                int NumberToSelect = PromptNumberToSelect.Value;

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
