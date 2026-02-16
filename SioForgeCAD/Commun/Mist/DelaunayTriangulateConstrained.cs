using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Mist
{
    internal static class DelaunayTriangulateConstrained
    {
            public static void TriangulateCommand()
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                // 1. Sélection : Accepter Points, Lignes et Polylignes (pour les contraintes)
                TypedValue[] filterList = {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "POINT"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };
                SelectionFilter selectionFilter = new SelectionFilter(filterList);

                PromptSelectionOptions pso = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSélectionnez les points et les lignes de contrainte :",
                    AllowDuplicates = false
                };

                PromptSelectionResult selRes = ed.GetSelection(pso, selectionFilter);
                if (selRes.Status != PromptStatus.OK) return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 2. Extraction des données
                    List<CdtVertex> vertices = new List<CdtVertex>();
                    List<CdtEdge> constraints = new List<CdtEdge>();

                    // Helper pour gérer les doublons de points rapidement
                    Dictionary<string, CdtVertex> uniquePoints = new Dictionary<string, CdtVertex>();

                    CdtVertex GetOrAddVertex(Point3d pt)
                    {
                        string key = $"{Math.Round(pt.X, 4)}_{Math.Round(pt.Y, 4)}"; // Clé simple pour tolérance
                        if (!uniquePoints.ContainsKey(key))
                        {
                            var v = new CdtVertex(pt.X, pt.Y, pt.Z);
                            uniquePoints[key] = v;
                            vertices.Add(v);
                        }
                        return uniquePoints[key];
                    }

                    foreach (ObjectId id in selRes.Value.GetObjectIds())
                    {
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                        if (ent is DBPoint dbPt)
                        {
                            GetOrAddVertex(dbPt.Position);
                        }
                        else if (ent is Line line)
                        {
                            var v1 = GetOrAddVertex(line.StartPoint);
                            var v2 = GetOrAddVertex(line.EndPoint);
                            constraints.Add(new CdtEdge(v1, v2));
                        }
                        else if (ent is Polyline pl)
                        {
                            for (int i = 0; i < pl.NumberOfVertices; i++)
                            {
                                // Traitement basique des segments droits de polyligne
                                Point3d pt1 = pl.GetPoint3dAt(i);
                                GetOrAddVertex(pt1);

                                if (i < pl.NumberOfVertices - 1 || pl.Closed)
                                {
                                    Point3d pt2 = pl.GetPoint3dAt((i + 1) % pl.NumberOfVertices);
                                    var vStart = GetOrAddVertex(pt1);
                                    var vEnd = GetOrAddVertex(pt2);
                                    constraints.Add(new CdtEdge(vStart, vEnd));
                                }
                            }
                        }
                    }

                    if (vertices.Count < 3)
                    {
                        ed.WriteMessage("\nIl faut au moins 3 points distincts.");
                        return;
                    }

                    ed.WriteMessage($"\nTraitement de {vertices.Count} points et {constraints.Count} contraintes...");

                    // 3. Exécution de l'algorithme CDT
                    CdtEngine engine = new CdtEngine();
                    List<CdtTriangle> resultTriangles = engine.RunCDT(vertices, constraints);

                    // 4. Dessin du résultat
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    foreach (var tri in resultTriangles)
                    {
                        // Création d'une Polyline3d fermée pour chaque triangle
                        using (Polyline3d poly = new Polyline3d())
                        {
                            btr.AppendEntity(poly);
                            tr.AddNewlyCreatedDBObject(poly, true);

                            poly.AppendVertex(new PolylineVertex3d(new Point3d(tri.V1.X, tri.V1.Y, tri.V1.Z)));
                            poly.AppendVertex(new PolylineVertex3d(new Point3d(tri.V2.X, tri.V2.Y, tri.V2.Z)));
                            poly.AppendVertex(new PolylineVertex3d(new Point3d(tri.V3.X, tri.V3.Y, tri.V3.Z)));
                            poly.Closed = true;
                        }
                    }

                    tr.Commit();
                    ed.WriteMessage($"\nTriangulation terminée : {resultTriangles.Count} triangles créés.");
                }
                Application.UpdateScreen();
            }
        }

        // --- MOTEUR CDT (Classes & Logique) ---

        public class CdtVertex
        {
            public double X, Y, Z;
            public CdtVertex(double x, double y, double z) { X = x; Y = y; Z = z; }
        }

        public class CdtEdge
        {
            public CdtVertex V1, V2;
            public CdtEdge(CdtVertex v1, CdtVertex v2) { V1 = v1; V2 = v2; }

            public bool EqualsEdge(CdtVertex a, CdtVertex b)
            {
                return (V1 == a && V2 == b) || (V1 == b && V2 == a);
            }
        }

        public class CdtTriangle
        {
            public CdtVertex V1, V2, V3;
            public bool IsActive = true;

            public CdtTriangle(CdtVertex v1, CdtVertex v2, CdtVertex v3)
            {
                V1 = v1; V2 = v2; V3 = v3;
            }

            // Vérifie si un point est dans le cercle circonscrit (pour Delaunay)
            public bool IsPointInCircumCircle(CdtVertex p)
            {
                // Utilisation d'un déterminant robuste ou formule simple
                double ax = V1.X - p.X; double ay = V1.Y - p.Y;
                double bx = V2.X - p.X; double by = V2.Y - p.Y;
                double cx = V3.X - p.X; double cy = V3.Y - p.Y;

                double det = (ax * ax + ay * ay) * (bx * cy - cx * by) -
                             (bx * bx + by * by) * (ax * cy - cx * ay) +
                             (cx * cx + cy * cy) * (ax * by - bx * ay);

                // Attention à l'orientation des points (CCW vs CW). 
                // Ici on suppose CCW, si det > 0 le point est à l'intérieur.
                return det > 1e-10;
            }

            public bool ContainsVertex(CdtVertex v)
            {
                return V1 == v || V2 == v || V3 == v;
            }
        }

        public class CdtEngine
        {
            public List<CdtTriangle> RunCDT(List<CdtVertex> points, List<CdtEdge> constraints)
            {
                List<CdtTriangle> triangles = new List<CdtTriangle>();

                // 1. Super Triangle
                double minX = points.Min(p => p.X);
                double minY = points.Min(p => p.Y);
                double maxX = points.Max(p => p.X);
                double maxY = points.Max(p => p.Y);
                double dx = maxX - minX;
                double dy = maxY - minY;
                double deltaMax = Math.Max(dx, dy) * 20; // Large marge

                CdtVertex st1 = new CdtVertex(minX - deltaMax, minY - deltaMax, 0);
                CdtVertex st2 = new CdtVertex(minX + deltaMax + dx, minY - deltaMax, 0);
                CdtVertex st3 = new CdtVertex(minX + dx / 2, maxY + deltaMax + dy, 0);

                triangles.Add(new CdtTriangle(st1, st2, st3));

                // 2. Delaunay Incremental (Bowyer-Watson)
                foreach (var p in points)
                {
                    List<CdtEdge> polygon = new List<CdtEdge>();

                    // Identifier les triangles dont le cercle circonscrit contient p
                    // On itère à l'envers pour suppression sûre
                    for (int i = triangles.Count - 1; i >= 0; i--)
                    {
                        CdtTriangle t = triangles[i];
                        if (t.IsPointInCircumCircle(p))
                        {
                            AddPolygonEdge(polygon, t.V1, t.V2);
                            AddPolygonEdge(polygon, t.V2, t.V3);
                            AddPolygonEdge(polygon, t.V3, t.V1);
                            triangles.RemoveAt(i);
                        }
                    }

                    // Créer les nouveaux triangles
                    foreach (var edge in polygon)
                    {
                        triangles.Add(new CdtTriangle(edge.V1, edge.V2, p));
                    }
                }

                // 3. Application des Contraintes (Méthode simplifiée de Force Edge)
                // Pour chaque contrainte, si elle n'existe pas, on tente de forcer le chemin
                foreach (var constraint in constraints)
                {
                    EnforceConstraint(triangles, constraint);
                }

                // 4. Nettoyage (Supprimer triangles liés au Super Triangle)
                triangles.RemoveAll(t =>
                    t.ContainsVertex(st1) || t.ContainsVertex(st2) || t.ContainsVertex(st3));

                return triangles;
            }

            // Gestion des arêtes du trou polygonal (si une arête est ajoutée 2 fois, elle est interne -> suppression)
            private void AddPolygonEdge(List<CdtEdge> polygon, CdtVertex v1, CdtVertex v2)
            {
                for (int i = 0; i < polygon.Count; i++)
                {
                    if (polygon[i].EqualsEdge(v1, v2))
                    {
                        polygon.RemoveAt(i);
                        return;
                    }
                }
                polygon.Add(new CdtEdge(v1, v2));
            }

            // --- Logique pour forcer une contrainte ---
            private void EnforceConstraint(List<CdtTriangle> triangles, CdtEdge constraint)
            {
                int safetyCounter = 0;
                while (safetyCounter++ < 500) // Eviter boucle infinie
                {
                    // Vérifier si la contrainte existe déjà comme arête
                    List<CdtTriangle> intersectingTriangles = new List<CdtTriangle>();
                    bool edgeExists = false;

                    foreach (var t in triangles)
                    {
                        // Si le triangle a cette arête exacte, c'est bon
                        if ((t.V1 == constraint.V1 && t.V2 == constraint.V2) || (t.V1 == constraint.V2 && t.V2 == constraint.V1) ||
                            (t.V2 == constraint.V1 && t.V3 == constraint.V2) || (t.V2 == constraint.V2 && t.V3 == constraint.V1) ||
                            (t.V3 == constraint.V1 && t.V1 == constraint.V2) || (t.V3 == constraint.V2 && t.V1 == constraint.V1))
                        {
                            edgeExists = true;
                            break;
                        }

                        // Sinon, vérifions si l'arête de contrainte croise une arête du triangle
                        // (Logique simplifiée ici: on cherche les triangles croisés)
                        if (IsTriangleIntersected(t, constraint))
                        {
                            intersectingTriangles.Add(t);
                        }
                    }

                    if (edgeExists || intersectingTriangles.Count == 0) return;

                    // Si on a des intersections, il faut faire un "Flip"
                    // Trouver deux triangles adjacents dans la liste d'intersection qui forment un quadrilatère convexe
                    bool flipped = false;

                    // Algorithme naïf : on cherche une arête commune traversée par la contrainte et on la flip
                    // Note: Une implémentation CDT robuste nécessite une structure Half-Edge.
                    // Ici on fait une tentative de réparation locale.
                    for (int i = 0; i < intersectingTriangles.Count; i++)
                    {
                        for (int j = i + 1; j < intersectingTriangles.Count; j++)
                        {
                            CdtTriangle t1 = intersectingTriangles[i];
                            CdtTriangle t2 = intersectingTriangles[j];

                            // Trouver l'arête commune
                            CdtEdge common = GetCommonEdge(t1, t2);
                            if (common != null)
                            {
                                // Vérifier si cette arête commune croise la contrainte
                                if (EdgesIntersect(common.V1, common.V2, constraint.V1, constraint.V2))
                                {
                                    // FLIP
                                    CdtVertex vOther1 = GetOppositeVertex(t1, common);
                                    CdtVertex vOther2 = GetOppositeVertex(t2, common);

                                    // On remplace t1 et t2 par deux nouveaux triangles (vOther1, vOther2, common.V1) etc.
                                    // Vérifions la convexité (si le flip est possible géométriquement)
                                    if (IsConvex(vOther1, common.V1, vOther2, common.V2))
                                    {
                                        triangles.Remove(t1);
                                        triangles.Remove(t2);

                                        triangles.Add(new CdtTriangle(vOther1, vOther2, common.V1));
                                        triangles.Add(new CdtTriangle(vOther1, vOther2, common.V2));

                                        flipped = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (flipped) break;
                    }

                    if (!flipped) return; // Impossible de résoudre (cas complexe ou non convexe)
                }
            }

            private bool IsTriangleIntersected(CdtTriangle t, CdtEdge c)
            {
                // Vérifie si la contrainte coupe l'une des arêtes du triangle
                if (EdgesIntersect(t.V1, t.V2, c.V1, c.V2)) return true;
                if (EdgesIntersect(t.V2, t.V3, c.V1, c.V2)) return true;
                if (EdgesIntersect(t.V3, t.V1, c.V1, c.V2)) return true;
                return false;
            }

            private CdtEdge GetCommonEdge(CdtTriangle t1, CdtTriangle t2)
            {
                // Cherche deux sommets communs
                List<CdtVertex> common = new List<CdtVertex>();
                if (t1.ContainsVertex(t2.V1)) common.Add(t2.V1);
                if (t1.ContainsVertex(t2.V2)) common.Add(t2.V2);
                if (t1.ContainsVertex(t2.V3)) common.Add(t2.V3);

                if (common.Count == 2) return new CdtEdge(common[0], common[1]);
                return null;
            }

            private CdtVertex GetOppositeVertex(CdtTriangle t, CdtEdge edge)
            {
                if (t.V1 != edge.V1 && t.V1 != edge.V2) return t.V1;
                if (t.V2 != edge.V1 && t.V2 != edge.V2) return t.V2;
                return t.V3;
            }

            // Test d'intersection strict de segments
            private bool EdgesIntersect(CdtVertex a, CdtVertex b, CdtVertex c, CdtVertex d)
            {
                return Ccw(a, c, d) != Ccw(b, c, d) && Ccw(a, b, c) != Ccw(a, b, d);
            }

            private bool IsConvex(CdtVertex a, CdtVertex b, CdtVertex c, CdtVertex d)
            {
                // Vérifie si le quadrilatère a-b-c-d est convexe (pour autoriser le flip de b-d vers a-c)
                return EdgesIntersect(a, c, b, d);
            }

            private bool Ccw(CdtVertex a, CdtVertex b, CdtVertex c)
            {
                return (c.Y - a.Y) * (b.X - a.X) > (b.Y - a.Y) * (c.X - a.X);
            }
        }
    }