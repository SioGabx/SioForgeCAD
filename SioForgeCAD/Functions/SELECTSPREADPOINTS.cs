using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class SELECTSPREADPOINTS
    {
        private class Candidate
        {
            public ObjectId Id;
            public Point3d Position;
        }

        public static void Select()
        {
            Editor ed = Generic.GetEditor();

            var sel = ed.GetSelectionRedraw();

            if (sel.Status != PromptStatus.OK)
            {
                return;
            }

            List<Candidate> pts = new List<Candidate>();

            using (var tr = Generic.GetDatabase().TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in sel.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;

                    if (ent is DBPoint p)
                    {
                        pts.Add(new Candidate
                        {
                            Id = id,
                            Position = p.Position
                        });
                    }
                    else if (ent is BlockReference b)
                    {
                        pts.Add(new Candidate
                        {
                            Id = id,
                            Position = b.Position
                        });
                    }
                }

                tr.Commit();
            }


            if (pts.Count == 0)
            {
                ed.WriteMessage("\nAucun point ou bloc.");
                return;
            }

            int? PromptNumberToSelectResult = ed.GetIntegerInRange("\nNombre d'objets désirés", 0, pts.Count, pts.Count);
            if (!(PromptNumberToSelectResult is int NumberToSelect))
            {
                return;
            }
            ObjectId[] result = SelectUniformGrid(pts, NumberToSelect);
            ed.SetImpliedSelection(result);
        }

        private static ObjectId[] SelectUniformGrid(List<Candidate> pts, int wanted)
        {
            int count = pts.Count;

            if (wanted >= count)
            {
                return pts.Select(p => p.Id).ToArray();
            }

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (Candidate p in pts)
            {
                double x = p.Position.X;
                double y = p.Position.Y;

                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }

            double width = maxX - minX;
            double height = maxY - minY;

            // Cas dégénéré : tous les points sont identiques
            if (width == 0 && height == 0)
            {
                return pts.Take(wanted).Select(p => p.Id).ToArray();
            }

            // Déterminer une grille adaptée au ratio de l'emprise
            double aspect = width / Math.Max(height, double.Epsilon);
            int columns = Math.Max(1, (int)Math.Round(Math.Sqrt(wanted * aspect)));
            int rows = Math.Max(1, (int)Math.Ceiling((double)wanted / columns));

            // Evite d'avoir beaucoup plus de cellules que nécessaire
            while (columns * rows > wanted * 1.2 && columns > 1)
            {
                columns--;
                rows = Math.Max(1, (int)Math.Ceiling((double)wanted / columns));
            }

            double cellWidth = width / columns;
            double cellHeight = height / rows;

            // Un point par cellule
            Candidate[] selected = new Candidate[columns * rows];
            double[] bestDistance = new double[selected.Length];

            for (int i = 0; i < bestDistance.Length; i++)
            {
                bestDistance[i] = double.MaxValue;
            }

            for (int i = 0; i < pts.Count; i++)
            {
                Candidate p = pts[i];
                int col;
                int row;

                if (cellWidth == 0)
                {
                    col = 0;
                }
                else
                {
                    col = (int)((p.Position.X - minX) / cellWidth);
                }

                if (cellHeight == 0)
                {
                    row = 0;
                }
                else
                {
                    row = (int)((p.Position.Y - minY) / cellHeight);
                }

                // Le point situé exactement sur maxX/maxY pourrait tomber juste après la dernière cellule.
                if (col >= columns)
                {
                    col = columns - 1;
                }

                if (row >= rows)
                {
                    row = rows - 1;
                }

                if (col < 0)
                {
                    col = 0;
                }

                if (row < 0)
                {
                    row = 0;
                }

                int index = (row * columns) + col;

                // Centre de la cellule
                double cx = minX + ((col + 0.5) * cellWidth);
                double cy = minY + ((row + 0.5) * cellHeight);

                double dx = p.Position.X - cx;
                double dy = p.Position.Y - cy;

                double distance = (dx * dx) + (dy * dy);

                // On garde le point le plus proche du centre
                if (distance < bestDistance[index])
                {
                    bestDistance[index] = distance;
                    selected[index] = p;
                }
            }

            // Récupérer les points sélectionnés
            List<Candidate> result = new List<Candidate>(wanted);
            HashSet<ObjectId> used = new HashSet<ObjectId>();

            for (int i = 0; i < selected.Length; i++)
            {
                Candidate p = selected[i];

                if (p != null && used.Add(p.Id))
                {
                    result.Add(p);

                    if (result.Count >= wanted)
                    {
                        break;
                    }
                }
            }

            // Compléter si certaines cellules étaient vides
            if (result.Count < wanted)
            {
                foreach (Candidate p in pts)
                {
                    if (result.Count >= wanted)
                    {
                        break;
                    }

                    if (used.Add(p.Id))
                    {
                        result.Add(p);
                    }
                }
            }

            return result.Take(wanted).Select(p => p.Id).ToArray();
        }
    }
}