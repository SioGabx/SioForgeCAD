using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class BLKTOSTATICBLOCK
    {
        public static void Convert()
        {
            Editor editor = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            PromptSelectionResult promptResult;

            while (true)
            {
                promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count > 0)
                    {
                        break;
                    }
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ed = Generic.GetEditor();
                foreach (ObjectId blockRefId in promptResult.Value.GetObjectIds())
                {
                    if (!(tr.GetObject(blockRefId, OpenMode.ForWrite) is BlockReference blockRef))
                    {
                        return;
                    }
                    blockRef.ConvertToStaticBlock(BlockReferences.GetUniqueBlockName(blockRef.GetBlockReferenceName() + "_static"));
                    /*
                      Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                    if (ent != null && ent.Visible == false)
                    {
                        ent.Erase();
                        count++;
                    } 
                      
                     
                    (defun C:PurgeHiddenEntities ( / selset n count entnme entdat bit60) (if (setq selset (ssget "X")) (progn (setq n 0 count 0) (repeat (sslength selset) (setq entdat (entget (setq entnme (ssname selset n)))) (if (and (setq bit60 (assoc 60 entdat))(= 1 (cdr bit60))) (progn (entdel entnme) (setq count (1+ count)) ) ) (setq n (1+ n)) ) (princ (strcat "\n " (itoa count) " hidden entities deleted.")) ) ) (princ) )

                    */
                }
                tr.Commit();
            }
        }
    }
}
