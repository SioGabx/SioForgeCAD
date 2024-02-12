using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class DRAWPERPENDICULARLINEFROMPOINT
    {
        public static void DrawPerpendicularLineFromPoint()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
            {
                MessageForAdding = "Sélectionnez une polyligne",
                SingleOnly = true,
            };
            PromptSelectionResult polyResult = ed.GetSelection(promptSelectionOptions);
            if (polyResult.Status != PromptStatus.OK)
            {
                return;
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
                Generic.WriteMessage("L'objet sélectionné n'est pas une polyligne.");
                return;
            }
            while (true)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptPointOptions pointOptions = new PromptPointOptions("Sélectionnez un point : \n");
                    PromptPointResult pointResult = ed.GetPoint(pointOptions);
                    if (pointResult.Status != PromptStatus.OK)
                    {
                        trans.Commit();
                        return;
                    }

                    Points ProjectionOriginPoint = Points.GetFromPromptPointResult(pointResult);

                    var ListOfPerpendicularLines = PerpendicularPoint.GetListOfPerpendicularLinesFromPoint(ProjectionOriginPoint, ProjectionTarget, true);
                    if (ListOfPerpendicularLines.Count > 0)
                    {
                        Line NearestPointPerpendicularLine = ListOfPerpendicularLines.FirstOrDefault();
                        Lines.Draw(NearestPointPerpendicularLine, null);
                    }
                    ListOfPerpendicularLines.DeepDispose();
                    trans.Commit();
                }

            }

        }
    }
}
