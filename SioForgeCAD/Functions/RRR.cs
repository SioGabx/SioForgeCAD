using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class RRR
    {
        public static async void Rotate()
        {
            Editor ed = Generic.GetEditor();
            PromptSelectionResult selectionResult;

            while (true)
            {
                selectionResult = ed.GetSelection();
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

            try
            {
                await Generic.CommandAsync("_rotate", selectionResult.Value, "", Editor.PauseToken, "_reference", "@");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
