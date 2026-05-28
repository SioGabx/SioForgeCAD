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

            // Demander l'intervalle des courbes
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez l'intervalle des courbes de niveau : ")
            {
                DefaultValue = 1.0,
                AllowNegative = false,
                AllowZero = false
            };
            PromptDoubleResult pdr = ed.GetDouble(pdo);

            if (pdr.Status != PromptStatus.OK) return;
            double intervalle = pdr.Value;
            double intervallePrincipal = (intervalle < 1.0) ? 1.0 : 5.0;

            PromptKeywordOptions pko = new PromptKeywordOptions("\nVoulez-vous lisser les courbes de niveau (Splines) ? [Oui/Non] : ", "Oui Non");
            pko.Keywords.Default = "Non";
            PromptResult pkr = ed.GetKeywords(pko);

            if (pkr.Status != PromptStatus.OK) return;
            bool createSplines = (pkr.StringResult == "Oui");

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<ObjectId> createdEntities = new List<ObjectId>();
                List<Point3d> PointsSet = new List<Point3d>();

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
                            Point3d point = new Point3d(blockRef.Position.X, blockRef.Position.Y, z);
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

                var DelaunayTriangles = DelaunayTriangulate.Triangulate(PointsSet);

                foreach (var Triangle in DelaunayTriangles)
                {
                    try
                    {
                        Point3d p1 = Triangle.Vertex1;
                        Point3d p2 = Triangle.Vertex2;
                        Point3d p3 = Triangle.Vertex3;

                        // Obtenir les vrais Min et Max du triangle (en double)
                        double trueMinZ = Math.Min(p1.Z, Math.Min(p2.Z, p3.Z));
                        double trueMaxZ = Math.Max(p1.Z, Math.Max(p2.Z, p3.Z));

                        // Calculer le premier et le dernier Z en fonction de l'intervalle
                        double startZ = Math.Ceiling(trueMinZ / intervalle) * intervalle;
                        double endZ = Math.Floor(trueMaxZ / intervalle) * intervalle;

                        // Ajout d'une marge de tolérance (1e-6) pour les problèmes d'arrondi sur la condition d'arrêt
                        for (double z = startZ; z <= endZ + 1e-6; z += intervalle)
                        {
                            // Arrondir la clé Z pour le dictionnaire 
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                DelaunayTriangles.DeepDispose();
                PointsSet.DeepDispose();


                // Chaînage des segments en Polylignes / Splines
                foreach (var kvp in segmentsByZ)
                {
                    double reste = Math.Abs(kvp.Key % intervallePrincipal);
                    bool estCourbePrincipale = reste < 1e-4 || Math.Abs(reste - intervallePrincipal) < 1e-4;

                    LineWeight epaisseurTrait = estCourbePrincipale ? LineWeight.LineWeight050 : LineWeight.LineWeight000;

                    foreach (Point3dCollection path in ChainSegments(kvp.Value))
                    {
                        if (path.Count < 2) continue;

                        Poly3dType polyType = createSplines ? Poly3dType.QuadSplinePoly : Poly3dType.SimplePoly;

                        bool isClosed = path[0].DistanceTo(path[path.Count - 1]) < 1e-5;
                        if (isClosed && path.Count > 2)
                        {
                            path.RemoveAt(path.Count - 1); // Enlever le point en double pour la fermeture
                        }

                        Polyline3d poly = new Polyline3d(polyType, path, isClosed)
                        {
                            LineWeight = epaisseurTrait
                        };
                        createdEntities.Add(poly.AddToDrawingCurrentTransaction());
                    }
                }

                if (createdEntities.Count > 0)
                {
                    var BlkDefId = BlockReferences.CreateFromExistingEnts(
                        typeof(CREATECONTOURSLINESFROMPOINTS).Name + "_" + DateTime.Now.Ticks, // Assurez-vous que c'est bien la classe voulue ici
                        $"Courbes généré à partir de {Generic.GetExtensionDLLName()}.",
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

                string typeEntity = createSplines ? "Splines" : "Polylignes";
                Generic.WriteMessage($"{createdEntities.Count} {typeEntity} de courbes de niveau créées.");
                tr.Commit();
            }
        }

        /// <summary>
        /// Assemble les segments indépendants en tracés continus (polylignes)
        /// </summary>
        private static List<Point3dCollection> ChainSegments(List<Tuple<Point3d, Point3d>> segments)
        {
            List<Point3dCollection> polylines = new List<Point3dCollection>();
            const double tol = 1e-5;

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
            const double eps = 1e-6; // Tolérance pour les erreurs d'arrondi (virgule flottante)

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
                    AddPt(new Point3d(a.X + (t * (b.X - a.X)), a.Y + (t * (b.Y - a.Y)), z));
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