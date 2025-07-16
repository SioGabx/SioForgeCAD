using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class RENAMELAYOUT
    {
        public static void Replace()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            string oldText = GetUserValue("Ancien texte : ");
            string newText = GetUserValue("Nouveau texte : ");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (Layout layout in ed.GetAllLayout())
                {
                    if (layout.LayoutName.Equals("Model", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (layout.LayoutName.Contains(oldText))
                    {
                        string oldName = layout.LayoutName;
                        string newName = oldName.Replace(oldText, newText);
                        if (newName == oldName)
                            continue;

                        try
                        {
                            layout.TryUpgradeOpen();
                            layout.LayoutName = newName;
                            Generic.WriteMessage($"Layout renommé : {oldName} -> {newName}");
                        }
                        catch (System.Exception ex)
                        {
                            Generic.WriteMessage($"Erreur en renommant {layout.LayoutName} : {ex.Message}");
                        }
                    }
                }

                tr.Commit();
            }

        }

        public static string GetUserValue(string Message)
        {
            Forms.InputDialogBox dialogBox = new Forms.InputDialogBox();
            dialogBox.SetUserInputPlaceholder("");
            dialogBox.SetPrompt(Message);
            dialogBox.SetCursorAtEnd();
            DialogResult dialogResult = dialogBox.ShowDialog();
            if (dialogResult != DialogResult.OK)
            {
                return null;
            }
            return dialogBox.GetUserInput();
        }
    }
}