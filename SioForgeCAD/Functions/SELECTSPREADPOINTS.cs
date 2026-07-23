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

        private class Cell
        {
            public double MinX;
            public double MaxX;
            public double MinY;
            public double MaxY;
            public List<Candidate> Points = new List<Candidate>();
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
            if (wanted >= pts.Count)
            {
                return pts.Select(p => p.Id).ToArray();
            }

            double minX = pts.Min(p => p.Position.X);
            double maxX = pts.Max(p => p.Position.X);
            double minY = pts.Min(p => p.Position.Y);
            double maxY = pts.Max(p => p.Position.Y);


            Cell root = new Cell
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                Points = pts
            };

            List<Cell> cells = new List<Cell> { root };


            // Nombre de divisions adaptatives
            while (cells.Count < wanted)
            {
                // cellule avec le plus de points
                Cell biggest = cells.Where(c => c.Points.Count > 1).OrderByDescending(c => c.Points.Count).FirstOrDefault();

                if (biggest == null)
                {
                    break;
                }

                cells.Remove(biggest);

                double midX = (biggest.MinX + biggest.MaxX) / 2.0;
                double midY = (biggest.MinY + biggest.MaxY) / 2.0;

                Cell[] children = {
                    new Cell { MinX = biggest.MinX, MaxX = midX, MinY = biggest.MinY, MaxY = midY },
                    new Cell { MinX = midX, MaxX = biggest.MaxX, MinY = biggest.MinY, MaxY = midY },
                    new Cell { MinX = biggest.MinX, MaxX = midX, MinY = midY, MaxY = biggest.MaxY },
                    new Cell { MinX = midX, MaxX = biggest.MaxX, MinY = midY, MaxY = biggest.MaxY }
                };

                foreach (Candidate p in biggest.Points)
                {
                    foreach (Cell child in children)
                    {
                        if (p.Position.X >= child.MinX && p.Position.X <= child.MaxX && p.Position.Y >= child.MinY && p.Position.Y <= child.MaxY)
                        {
                            child.Points.Add(p);
                            break;
                        }
                    }
                }

                foreach (Cell child in children)
                {
                    if (child.Points.Count > 0)
                    {
                        cells.Add(child);
                    }
                }
            }

            List<Candidate> result = new List<Candidate>();

            // Choisir le point le plus central de chaque cellule
            foreach (Cell cell in cells)
            {
                double cx = (cell.MinX + cell.MaxX) / 2;
                double cy = (cell.MinY + cell.MaxY) / 2;

                Candidate best = cell.Points.OrderBy(p =>
                {
                    double dx = p.Position.X - cx;
                    double dy = p.Position.Y - cy;
                    return (dx * dx) + (dy * dy);
                }).First();

                result.Add(best);
            }

            // Si on a encore trop peu de cellules compléter avec des points restants
            if (result.Count < wanted)
            {
                HashSet<ObjectId> used = new HashSet<ObjectId>(result.Select(r => r.Id));

                foreach (Candidate p in pts)
                {
                    if (result.Count >= wanted)
                    {
                        break;
                    }

                    if (!used.Contains(p.Id))
                    {
                        result.Add(p);
                        used.Add(p.Id);
                    }
                }
            }

            return result.Take(wanted).Select(p => p.Id).ToArray();
        }

    }
}