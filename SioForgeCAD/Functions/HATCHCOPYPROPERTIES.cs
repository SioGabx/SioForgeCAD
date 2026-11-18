using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class HATCHCOPYPROPERTIES
    {
        public static void CopyProperties()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetHatch(out ObjectId SourceHatchId, "\nSélectionnez la hachure source (motif à copier) : "))
            {
                return;
            }

            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "HATCH")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions opts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSélectionnez les hachures cibles (à remplacer) : "
            };

            var selRes = ed.GetSelectionRedraw(opts, filter);
            if (selRes.Status != PromptStatus.OK)
            {
                return;
            }

            ObjectId[] selectedIds = selRes.Value.GetObjectIds();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch originHatch = SourceHatchId.GetDBObject(OpenMode.ForRead) as Hatch;
                if (originHatch == null)
                {
                    return;
                }

                int count = 0;
                foreach (ObjectId targetHatchId in selectedIds)
                {
                    if (SourceHatchId == targetHatchId)
                    {
                        // Ignorer si l'utilisateur a inclus la hachure source dans la sélection cible
                        continue;
                    }

                    Hatch targetHatch = targetHatchId.GetDBObject(OpenMode.ForWrite) as Hatch;
                    if (targetHatch != null)
                    {
                        originHatch.CopyHatchPropertiesToAndDelete(targetHatch);
                        count++;
                    }
                }

                if (count == 0)
                {
                    ed.WriteMessage("Aucune hachure cible valide sélectionnée (vous avez peut-être uniquement sélectionné la hachure source).");
                }else if (count > 0)
                {
                    ed.WriteMessage($"{count} hachures cibles ont été copiées à la perfection !");
                }

                tr.Commit();
            }
        }
    }
}