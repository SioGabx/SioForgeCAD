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

            var selRes = ed.GetBlocks(out var ObjIds, "Selectionnez des côtes", false, true);

            if (!selRes) return;

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
                            double z = (double)Convert.ToDouble(text);
                            DBPoint point = new DBPoint(new Point3d(blockRef.Position.X, blockRef.Position.Y, z));
                            PointsSet.Add(point);
                            break;
                        }
                    }
                }

                if (PointsSet.Count < 3)
                {
                    Generic.WriteMessage("Vous devez selectionner au moins 3 points");
                    PointsSet.DeepDispose();
                    tr.Abort();
                    ed.SetImpliedSelection(ObjIds);
                    return;
                }

                // Définition de l'intervalle des courbes de niveau (en mètres)
                double intervalle = 1.0;

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

                            // Si on a bien un triangle
                            if (vertices.Count >= 3)
                            {
                                Point3d p1 = vertices[0];
                                Point3d p2 = vertices[1];
                                Point3d p3 = vertices[2];

                                // Calcul du Z min et max pour ce triangle
                                int minZ = (int)Math.Ceiling(Math.Min(p1.Z, Math.Min(p2.Z, p3.Z)));
                                int maxZ = (int)Math.Floor(Math.Max(p1.Z, Math.Max(p2.Z, p3.Z)));

                                // Création des segments pour chaque élévation
                                for (int z = minZ; z <= maxZ; z += (int)intervalle)
                                {
                                    var intersections = GetTriangleContourIntersections(p1, p2, p3, z);

                                    // Si un plan coupe un triangle, on obtient 2 points
                                    if (intersections.Count == 2)
                                    {
                                        Line contourSegment = new Line(intersections[0], intersections[1]);
                                        createdEntities.Add(contourSegment.AddToDrawingCurrentTransaction());
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }

                PointsSet.DeepDispose();

                if (createdEntities.Count > 0)
                {
                    var BlkDefId = BlockReferences.CreateFromExistingEnts(
                        typeof(SKETCHUPCREATETERRAINFROMPOINTS).Name + "_" + DateTime.Now.Ticks,
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

                Generic.WriteMessage($"{createdEntities.Count} segments de courbes de niveau créés.");
                tr.Commit();
            }
        }

        /// <summary>
        /// Trouve les intersections entre un triangle 3D et un plan horizontal à l'élévation Z
        /// </summary>
        private static List<Point3d> GetTriangleContourIntersections(Point3d p1, Point3d p2, Point3d p3, double z)
        {
            List<Point3d> pts = new List<Point3d>();

            AddIntersection(p1, p2, z, pts);
            AddIntersection(p2, p3, z, pts);
            AddIntersection(p3, p1, z, pts);

            return pts;
        }

        /// <summary>
        /// Calcule le point d'intersection sur une arête si elle traverse l'élévation Z
        /// </summary>
        private static void AddIntersection(Point3d a, Point3d b, double z, List<Point3d> pts)
        {
            // Vérifie si le segment croise l'élévation Z
            if ((a.Z <= z && b.Z > z) || (b.Z <= z && a.Z > z))
            {
                // Interpolation linéaire
                double t = (z - a.Z) / (b.Z - a.Z);
                double x = a.X + t * (b.X - a.X);
                double y = a.Y + t * (b.Y - a.Y);
                pts.Add(new Point3d(x, y, z));
            }
        }
    }
}
