using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun.Drawing;


namespace SioForgeCAD.Commun.Extensions
{
    internal class LinesExtentions
    {
        public static Polyline AskForSelection(string Message)
        {
            while (true)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    MessageForAdding = Message,
                    SingleOnly = true,
                };
                PromptSelectionResult polyResult = ed.GetSelection(promptSelectionOptions);
                if (polyResult.Status != PromptStatus.OK)
                {
                    return null;
                }
                Entity SelectedEntity;

                using (Transaction GlobalTrans = db.TransactionManager.StartTransaction())
                {
                    SelectedEntity = polyResult.Value[0].ObjectId.GetEntity();
                }
                if (SelectedEntity is Line ProjectionTargetLine)
                {
                    SelectedEntity = ProjectionTargetLine.ToPolyline();
                }
                if (!(SelectedEntity is Polyline ProjectionTarget))
                {
                    ed.WriteMessage("L'objet sélectionné n'est pas une polyligne. \n");
                    continue;
                }
                return ProjectionTarget;
            }
        }



    }
}
