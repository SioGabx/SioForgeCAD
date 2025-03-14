using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;

namespace SioForgeCAD.Functions
{
    public static class STRIPTEXTFORMATING
    {
        public static void Strip()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        if (selObj?.ObjectId.GetDBObject(OpenMode.ForWrite) is Entity ent)
                        {
                            if (ent is MText mText)
                            {
                                mText.Contents = mText.Text;
                            }
                            if (ent is MLeader MLeader)
                            {
                                MText MlmText = MLeader.MText;
                                MlmText.Contents = MlmText.Text;
                                MLeader.MText = MlmText;
                            }
                        }
                    }
                    tr.Commit();
                }
            }
        }
    }
}
