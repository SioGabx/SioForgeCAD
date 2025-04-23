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

            Polyline ProjectionTarget = ed.GetPolyline(out _, "Sélectionnez une polyligne", false, false, true);
            if (ProjectionTarget is null)
            {
                return;
            }

            while (true)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptPointOptions pointOptions = new PromptPointOptions("\nSélectionnez un point");
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
