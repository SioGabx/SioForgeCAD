using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Geometry;

namespace SioForgeCAD.Functions
{
    public static class CREATECONTOURSLINESFROMPOINTS
    {
        public static void GeneratePointsFromAlt()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var selRes = ed.GetBlocks(out var ObjIds, "Sélectionnez des côtes", false, true);
            if (!selRes) return;

            // 1. Demander l'intervalle des courbes
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez l'intervalle des courbes de niveau : ");
            pdo.DefaultValue = 1.0;
            pdo.AllowNegative = false;
            pdo.AllowZero = false;
            PromptDoubleResult pdr = ed.GetDouble(pdo);

            if (pdr.Status != PromptStatus.OK) return;
            double intervalle = pdr.Value;

            // 2. Demander si on veut transformer les polylignes en Splines
            PromptKeywordOptions pko = new PromptKeywordOptions("\nVoulez-vous lisser les courbes de niveau (Splines) ? [Oui/Non] : ", "Oui Non");
            pko.Keywords.Default = "Non";
            PromptResult pkr = ed.GetKeywords(pko);

            if (pkr.Status != PromptStatus.OK) return;
            bool createSplines = (pkr.StringResult == "Oui");

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<ObjectId> createdEntities = new List<ObjectId>();
                List<DBPoint> PointsSet = new List<DBPoint>();

                foreach (var selObj in ObjIds)
                {
                    if (!(tr.GetObject(selObj, OpenMode.ForRead) is BlockReference blockRef)) continue;

                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        if (!(tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)) continue;

                        string text = attRef.TextString;

                        if (Regex.IsMatch(text, @"^\d+\.\d{2,}$"))
                        {
                            double z = Convert.ToDouble(text);
                            DBPoint point = new DBPoint(new Point3d(blockRef.Position.X, blockRef.Position.Y, z));
                            PointsSet.Add(point);
                            break;
                        }
                    }
                }

                if (PointsSet.Count < 3)
                {
                    Generic.WriteMessage("\nVous devez sélectionner au moins 3 points.");
                    PointsSet.DeepDispose();
                    tr.Abort();
                    ed.SetImpliedSelection(ObjIds);
                    return;
                }

                // Dictionnaire pour stocker les segments non connectés par élévation (Z)
                Dictionary<double, List<Tuple<Point3d, Point3d>>> segmentsByZ = new Dictionary<double, List<Tuple<Point3d, Point3d>>>();

                using (var Polys3D = DelaunayTriangulate.Triangulate(PointsSet).ToDBObjectCollection())
                {
                    foreach (Entity Poly3D in Polys3D)
                    {
                        try
                        {
                            List<Point3d> vertices = new List<Point3d>();

                            if (Poly3D is Polyline3d poly3d)
                            {
                                int limit = (int)Math.Floor(poly3d.EndParam);
                                for (int i = 0; i <= limit; i++)
                                {
                                    vertices.Add(poly3d.GetPointAtParameter(i));
                                }
                            }

                            if (vertices.Count >= 3)
                            {
                                Point3d p1 = vertices[0];
                                Point3d p2 = vertices[1];
                                Point3d p3 = vertices[2];

                                // 1. Obtenir les vrais Min et Max du triangle (en double)
                                double trueMinZ = Math.Min(p1.Z, Math.Min(p2.Z, p3.Z));
                                double trueMaxZ = Math.Max(p1.Z, Math.Max(p2.Z, p3.Z));

                                // 2. Calculer le premier et le dernier Z en fonction de l'intervalle
                                double startZ = Math.Ceiling(trueMinZ / intervalle) * intervalle;
                                double endZ = Math.Floor(trueMaxZ / intervalle) * intervalle;

                                // Ajout d'une marge de tolérance (1e-6) pour les problèmes d'arrondi sur la condition d'arrêt
                                for (double z = startZ; z <= endZ + 1e-6; z += intervalle)
                                {
                                    // 3. Arrondir la clé Z pour le dictionnaire afin d'éviter la dérive des flottants
                                    double zKey = Math.Round(z, 4);

                                    var intersections = GetTriangleContourIntersections(p1, p2, p3, z);

                                    if (intersections.Count == 2)
                                    {
                                        if (!segmentsByZ.ContainsKey(zKey))
                                            segmentsByZ[zKey] = new List<Tuple<Point3d, Point3d>>();

                                        segmentsByZ[zKey].Add(new Tuple<Point3d, Point3d>(intersections[0], intersections[1]));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                    Polys3D.DeepDispose();
                    PointsSet.DeepDispose();
                }

                // 3. Chaînage des segments en Polylignes / Splines
                foreach (var kvp in segmentsByZ)
                {
                    List<Point3dCollection> chainedPaths = ChainSegments(kvp.Value);

                    foreach (Point3dCollection path in chainedPaths)
                    {
                        if (path.Count < 2) continue;

                        if (createSplines && path.Count > 2)
                        {
                            Vector3d startTangent = new Vector3d(0,0,0);
                            Vector3d endTangent = new Vector3d(2,2,2);

                            // Création de la Spline (Ordre 4 = Spline cubique standard)
                            Spline spl = new Spline(path, startTangent, endTangent, KnotParameterizationEnum.SqrtChord, 3, 0.0);
                            
                            createdEntities.Add(spl.AddToDrawingCurrentTransaction());
                        }
                        else
                        {
                            // Vérifie la fermeture pour les Polylignes (le début et la fin se touchent)
                            bool isClosed = path[0].DistanceTo(path[path.Count - 1]) < 1e-5;
                            if (isClosed && path.Count > 2)
                            {
                                path.RemoveAt(path.Count - 1); // Enlever le point en double pour la fermeture
                            }

                            Polyline3d poly = new Polyline3d(Poly3dType.SimplePoly, path, isClosed);
                            createdEntities.Add(poly.AddToDrawingCurrentTransaction());
                        }
                    }
                }

                if (createdEntities.Count > 0)
                {
                    var BlkDefId = BlockReferences.CreateFromExistingEnts(
                        typeof(SKETCHUPCREATETERRAINFROMPOINTS).Name + "_" + DateTime.Now.Ticks, // Assurez-vous que c'est bien la classe voulue ici
                        $"Terrain généré à partir de {Generic.GetExtensionDLLName()} pour SketchUp.",
                        createdEntities.ToObjectIdCollection(),
                        Points.Empty,
                        true,
                        BlockScaling.Uniform,
                        true);

                    if (!BlkDefId.IsValid) { tr.Commit(); return; }
                    var BlkRef = new BlockReference(Points.Empty.SCG, BlkDefId);
                    BlkRef.AddToDrawingCurrentTransaction();

                    ed.SetImpliedSelection(new ObjectId[1] { BlkRef.ObjectId });
                }

                string typeEntity = createSplines ? "Splines" : "Polylignes 3D";
                Generic.WriteMessage($"\n{createdEntities.Count} {typeEntity} de courbes de niveau créées.");
                tr.Commit();
            }
        }

        /// <summary>
        /// Assemble les segments indépendants en tracés continus (polylignes)
        /// </summary>
        private static List<Point3dCollection> ChainSegments(List<Tuple<Point3d, Point3d>> segments)
        {
            List<Point3dCollection> polylines = new List<Point3dCollection>();
            double tol = 1e-5; // Tolérance d'accrochage

            List<Tuple<Point3d, Point3d>> pool = new List<Tuple<Point3d, Point3d>>(segments);

            while (pool.Count > 0)
            {
                var currentChain = new LinkedList<Point3d>();
                var firstSeg = pool[0];
                pool.RemoveAt(0);

                currentChain.AddLast(firstSeg.Item1);
                currentChain.AddLast(firstSeg.Item2);

                bool added = true;
                while (added)
                {
                    added = false;
                    for (int i = 0; i < pool.Count; i++)
                    {
                        var seg = pool[i];
                        Point3d head = currentChain.First.Value;
                        Point3d tail = currentChain.Last.Value;

                        if (seg.Item1.DistanceTo(tail) < tol) { currentChain.AddLast(seg.Item2); pool.RemoveAt(i); added = true; break; }
                        else if (seg.Item2.DistanceTo(tail) < tol) { currentChain.AddLast(seg.Item1); pool.RemoveAt(i); added = true; break; }
                        else if (seg.Item1.DistanceTo(head) < tol) { currentChain.AddFirst(seg.Item2); pool.RemoveAt(i); added = true; break; }
                        else if (seg.Item2.DistanceTo(head) < tol) { currentChain.AddFirst(seg.Item1); pool.RemoveAt(i); added = true; break; }
                    }
                }

                Point3dCollection pts = new Point3dCollection();
                foreach (var p in currentChain) pts.Add(p);

                polylines.Add(pts);
            }

            return polylines;
        }

        private static List<Point3d> GetTriangleContourIntersections(Point3d p1, Point3d p2, Point3d p3, double z)
        {
            List<Point3d> pts = new List<Point3d>();
            double eps = 1e-6; // Tolérance pour les erreurs d'arrondi (virgule flottante)

            // Fonction locale pour éviter d'ajouter des points en double 
            // (par ex. si z coupe exactement un sommet partagé par deux arêtes)
            void AddPt(Point3d pt)
            {
                foreach (var p in pts)
                {
                    if (p.DistanceTo(pt) < eps) return; // Le point existe déjà
                }
                pts.Add(pt);
            }

            // Fonction locale pour vérifier une arête
            void CheckEdge(Point3d a, Point3d b)
            {
                bool aOnPlane = Math.Abs(a.Z - z) < eps;
                bool bOnPlane = Math.Abs(b.Z - z) < eps;

                if (aOnPlane && bOnPlane)
                {
                    // Toute l'arête est sur le contour
                    AddPt(new Point3d(a.X, a.Y, z));
                    AddPt(new Point3d(b.X, b.Y, z));
                }
                else if (aOnPlane)
                {
                    // Seul le point A touche le contour
                    AddPt(new Point3d(a.X, a.Y, z));
                }
                else if (bOnPlane)
                {
                    // Seul le point B touche le contour
                    AddPt(new Point3d(b.X, b.Y, z));
                }
                else if ((a.Z < z && b.Z > z) || (b.Z < z && a.Z > z))
                {
                    // L'arête traverse le plan (croisement franc)
                    double t = (z - a.Z) / (b.Z - a.Z);
                    AddPt(new Point3d(a.X + t * (b.X - a.X), a.Y + t * (b.Y - a.Y), z));
                }
            }

            // Tester les 3 arêtes du triangle
            CheckEdge(p1, p2);
            CheckEdge(p2, p3);
            CheckEdge(p3, p1);

            return pts;
        }
    }
}