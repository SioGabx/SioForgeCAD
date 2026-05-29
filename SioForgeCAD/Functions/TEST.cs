using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SioForgeCAD.Functions
{
    public static class TEST
    {
        public static void OffsetSegmentsCustom()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez une polyligne fermée : ");

            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            double offsetDist = .5; // Distance de décalage

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline pline = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForWrite);
                bool isClockwise = IsClockwise(pline);



                int numSegments = pline.NumberOfVertices;

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                for (int i = 0; i < numSegments; i++)
                {
                    // 1. Isoler le segment
                    LineSegment2d seg = pline.GetLineSegment2dAt(i);

                    // 2. Calculer le vecteur directeur et la normale
                    Vector2d v = seg.EndPoint - seg.StartPoint;
                    double len = v.Length;
                    Vector2d unitNormal = isClockwise ? new Vector2d(v.Y, -v.X) / len : new Vector2d(-v.Y, v.X) / len;

                    // 3. Créer le vecteur de translation
                    Vector2d translationVector = new Vector2d(unitNormal.X * offsetDist, unitNormal.Y * offsetDist);

                    // 4. Créer une matrice de translation (Garanti le parallélisme)
                    Matrix2d mat = Matrix2d.Displacement(translationVector);

                    // 5. Créer une copie du segment et appliquer la transformation
                    LineSegment2d movedSeg = (LineSegment2d)seg.Clone();
                    v.TransformBy(mat);

                    // Dessiner le nouveau segment dans le dessin (Entity Line)
                    Line line = new Line(movedSeg.StartPoint.ToPoint3d(), movedSeg.EndPoint.ToPoint3d());
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                }

                tr.Commit();
            }
        }

        private static bool IsClockwise(Polyline pl)
        {
            double area = 0;
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p1 = pl.GetPoint2dAt(i);
                Point2d p2 = (i == pl.NumberOfVertices - 1) ? pl.GetPoint2dAt(0) : pl.GetPoint2dAt(i + 1);
                area += (p2.X - p1.X) * (p2.Y + p1.Y);
            }
            return area > 0; // Si l'aire signée est positive, c'est CW
        }
    }
}