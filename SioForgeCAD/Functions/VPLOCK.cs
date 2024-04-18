using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class VPLOCK
    {
        public static void DoLockUnlock(bool Lock)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            TypedValue[] viewportFilter = { new TypedValue((int)DxfCode.Start, "Viewport") };
            try
            {
                PromptSelectionResult viewportSelection = ed.SelectAll(new SelectionFilter(viewportFilter));
                SelectionSet selectionSet = viewportSelection.Value;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId objectId in selectionSet.GetObjectIds())
                    {
                        Viewport viewport = (Viewport)tr.GetObject(objectId, OpenMode.ForWrite);
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
