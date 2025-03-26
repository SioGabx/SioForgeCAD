using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class VIEWPORTLOCK
    {
        public static void Menu()
        {
            Editor ed = Generic.GetEditor();
            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("Veuillez selectionner une opération :")
            {
                AllowArbitraryInput = false,
                AppendKeywordsToMessage = true
            };
            promptKeywordOptions.Keywords.Add("Lock All");
            promptKeywordOptions.Keywords.Default = "Lock All";
            promptKeywordOptions.Keywords.Add("Unlock All");

            var KeyResult = ed.GetKeywords(promptKeywordOptions);
            if (!KeyResult.Status.HasFlag(PromptStatus.OK) && !KeyResult.Status.HasFlag(PromptStatus.Keyword))
            {
                return;
            }

            DoLockUnlock(KeyResult.StringResult == "Lock");
        }

        public static void DoLockUnlock(bool Lock)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            TypedValue[] viewportFilter = { new TypedValue((int)DxfCode.Start, "Viewport") };
            try
            {
                PromptSelectionResult viewportSelection = ed.SelectAll(new SelectionFilter(viewportFilter));
                SelectionSet selectionSet = viewportSelection.Value;
                if (selectionSet is null)
                {
                    return;
                }
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId objectId in selectionSet?.GetObjectIds())
                    {
                        Viewport viewport = (Viewport)objectId.GetDBObject(OpenMode.ForWrite);
                        viewport.Locked = Lock;
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
