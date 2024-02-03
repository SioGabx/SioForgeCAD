using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;


namespace SioForgeCAD.Commun.Extensions
{
    public static class LinesExtentions
    {
        public static void Cleanup(this Polyline polyline)
        {
            if (polyline != null)
            {
                int vertexCount = polyline.NumberOfVertices;

                if (vertexCount > 2)
                {
                    Point2d lastPoint = polyline.GetPoint2dAt(0);
                    int index = 1;
                    while ((polyline.NumberOfVertices - 1) > index)
                    {
                        Point2d currentPoint = polyline.GetPoint2dAt(index);
                        Point2d nextPoint = polyline.GetPoint2dAt(index + 1);
                        Vector2d vector1 = currentPoint.GetVectorTo(lastPoint);
                        Vector2d vector2 = nextPoint.GetVectorTo(currentPoint);

                        // Calculer la normal du vecteur en utilisant le produit vectoriel
                        double crossProduct = vector1.X * vector2.Y - vector1.Y * vector2.X;

                        if (Math.Abs(crossProduct) < Tolerance.Global.EqualPoint || currentPoint == nextPoint)
                        {
                            polyline.RemoveVertexAt(index);
                            // Décrémenter l'index pour réexaminer le point actuel lors de la prochaine itération
                            index--;
                        }
                        lastPoint = currentPoint;
                        index++;
                    }
                }
            }
        }

        public static void AddVertex(this Polyline Poly, Point3d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            Poly.AddVertexAt(Poly.NumberOfVertices, point.ToPoint2d(), bulge, startWidth, endWidth);
        }

        public static void AddVertexIfNotExist(this Polyline Poly, Point3d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            for (int i = 0; i < Poly.NumberOfVertices; i++)
            {
                if (Poly.GetPoint3dAt(i) == point)
                {
                    return;
                }
            }
            AddVertex(Poly, point, bulge, startWidth, endWidth);
        }



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
                if (polyResult.Status == PromptStatus.Error)
                {
                    continue;
                }
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
