using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SioForgeCAD.Functions
{
    public static class CREATECONTOURSLINESFROMPOINTS
    {
        private const double EPS = 1e-6;

        public static void GeneratePointsFromAlt()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var selRes = ed.GetBlocks(out var ObjIds, "Sélectionnez des côtes", false, true);
            if (!selRes)
            {
                return;
            }

            var selBuildings = ed.GetSelectionRedraw(
                "\nSélectionnez les bâtiments : ",
                true,
                false,
                ed.GetCurvesFilter());

            if (selBuildings.Status != PromptStatus.OK)
            {
                return;
            }

            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez l'intervalle des courbes de niveau : ")
            {
                DefaultValue = 1.0,
                AllowNegative = false,
                AllowZero = false
            };

            PromptDoubleResult pdr = ed.GetDouble(pdo);

            if (pdr.Status != PromptStatus.OK)
            {
                return;
            }

            double intervalle = pdr.Value;

            double intervallePrincipal = (intervalle < 1.0) ? 1.0 : 5.0;

            PromptKeywordOptions pko =
                new PromptKeywordOptions(
                    "\nVoulez-vous lisser les courbes de niveau (Splines) ? [Oui/Non] : ",
                    "Oui Non");

            pko.Keywords.Default = "Non";

            PromptResult pkr = ed.GetKeywords(pko);

            if (pkr.Status != PromptStatus.OK)
            {
                return;
            }

            bool createSplines = (pkr.StringResult == "Oui");

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    List<Entity> createdEntities = new List<Entity>();

                    List<Point3d> terrainPoints = ReadTopoPoints(tr, ObjIds);

                    if (terrainPoints.Count < 3)
                    {
                        Generic.WriteMessage("\nVous devez sélectionner au moins 3 points.");
                        tr.Abort();
                        return;
                    }

                    List<Polyline> buildings = ReadBuildings(tr, selBuildings.Value);

                    

                    var firstTriangles = DelaunayTriangulate.Triangulate(terrainPoints);

                    List<Point3d> sampledPoints = GenerateBuildingSamplePoints(buildings, firstTriangles, 0.25, 0.50);

                    double maxTerrainZ = terrainPoints.Max(p => p.Z);
                    sampledPoints.ForEach(t => t.AddToDrawing(5));
                    List<Point3d> roofPoints = GenerateBuildingPeakPoints(buildings, 1, 1.5, maxTerrainZ - 1.0);

                    roofPoints.ForEach(t => t.AddToDrawing(6));

                    List<Point3d> finalPoints = new List<Point3d>();

                    finalPoints.AddRange(terrainPoints);
                    finalPoints.AddRange(sampledPoints);
                    finalPoints.AddRange(roofPoints);

                    // =====================================================
                    // SECOND DELAUNAY
                    // =====================================================

                    var finalTriangles = DelaunayTriangulate.Triangulate(finalPoints);

                    // =====================================================
                    // GENERATION DES SEGMENTS DE COURBES
                    // =====================================================

                    Dictionary<double, List<Tuple<Point3d, Point3d>>> segmentsByZ = GenerateContourSegments(finalTriangles, intervalle);

                    // =====================================================
                    // CHAINAGE
                    // =====================================================

                    foreach (var kvp in segmentsByZ)
                    {
                        double reste = Math.Abs(kvp.Key % intervallePrincipal);

                        bool estCourbePrincipale = reste < 1e-4 || Math.Abs(reste - intervallePrincipal) < 1e-4;

                        LineWeight epaisseurTrait = estCourbePrincipale ? LineWeight.LineWeight050 : LineWeight.LineWeight000;

                        foreach (Point3dCollection path in ChainSegments(kvp.Value))
                        {
                            if (path.Count <= 2)
                            {
                                continue;
                            }

                            Poly3dType polyType = createSplines ? Poly3dType.QuadSplinePoly : Poly3dType.SimplePoly;

                            bool isClosed =
                                path[0].DistanceTo(path[path.Count - 1]) < 1e-5;

                            if (isClosed && path.Count > 2)
                            {
                                path.RemoveAt(path.Count - 1);
                            }

                            Polyline3d poly = new Polyline3d(polyType, path, isClosed)
                            {
                                LineWeight = epaisseurTrait
                            };

                            // =====================================================
                            // SUPPRESSION DES COURBES INTERNES AUX BATIMENTS
                            // =====================================================

                            if (IsContourInsideBuildings(poly, buildings))
                            {
                                poly.Dispose();
                                continue;
                            }

                            createdEntities.Add(poly);
                        }
                    }

                    // =====================================================
                    // DESSIN
                    // =====================================================

                    if (createdEntities.Count > 0)
                    {
                        var blkDefId =
                            BlockReferences.Create(typeof(CREATECONTOURSLINESFROMPOINTS).Name + "_" + DateTime.Now.Ticks,
                                                            $"Courbes générées à partir de {Generic.GetExtensionDLLName()}.",
                                createdEntities.ToDBObjectCollection(),
                                Points.Empty,
                                true,
                                BlockScaling.Uniform);

                        if (!blkDefId.IsValid)
                        {
                            tr.Commit();
                            return;
                        }

                        BlockReference blkRef = new BlockReference(Points.Empty.SCG, blkDefId);

                        blkRef.AddToDrawingCurrentTransaction();

                        ed.SetImpliedSelection(new ObjectId[1] { blkRef.ObjectId });
                    }

                    Generic.WriteMessage($"{createdEntities.Count} courbes générées.");

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Generic.WriteMessage($"Erreur : {ex.Message}");
                    tr.Abort();
                }
            }
        }

        // =====================================================================
        // TOPO POINTS
        // =====================================================================

        private static List<Point3d> ReadTopoPoints(
            Transaction tr,
            ObjectId[] ids)
        {
            List<Point3d> pts = new List<Point3d>();

            foreach (ObjectId id in ids)
            {
                if (!(tr.GetObject(id, OpenMode.ForRead) is BlockReference blockRef))
                {
                    continue;
                }

                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    if (!(tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef))
                    {
                        continue;
                    }

                    string text = attRef.TextString;

                    if (Regex.IsMatch(text, @"^\d+\.\d{2,}$"))
                    {
                        double z = Convert.ToDouble(text);
                        pts.Add(new Point3d(blockRef.Position.X, blockRef.Position.Y, z));
                        break;
                    }
                }
            }

            return pts;
        }

        // =====================================================================
        // BATIMENTS
        // =====================================================================

        private static List<Polyline> ReadBuildings(Transaction tr, object sel)
        {
            List<Polyline> result = new List<Polyline>();

            if (!(sel is SelectionSet selectionSet))
            {
                return result;
            }
            foreach (SelectedObject so in selectionSet)
            {
                if (so == null)
                {
                    continue;
                }

                Polyline pl = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Polyline;

                if (pl?.Closed == true)
                {
                    result.Add(pl);
                }
            }

            return result;
        }

        // =====================================================================
        // ECHANTILLONNAGE
        // =====================================================================

        private static Polyline CreateInnerOffsetPolyline(Polyline source, double offsetDist)
        {
            Polyline result = new Polyline();
            bool clockwise = IsClockwise(source);
            int count = source.NumberOfVertices;
            List<Line2d> offsetLines = new List<Line2d>();

            for (int i = 0; i < count; i++)
            {
                if (source.GetSegmentType(i) != SegmentType.Line)
                {
                    continue;
                }

                Point2d p1 = source.GetPoint2dAt(i);
                Point2d p2 = source.GetPoint2dAt((i + 1) % count);
                Vector2d dir = p2 - p1;
                double len = dir.Length;
                if (len < EPS)
                {
                    continue;
                }

                dir = dir / len;
                Vector2d normal = clockwise ? new Vector2d(dir.Y, -dir.X) : new Vector2d(-dir.Y, dir.X);
                Vector2d offset = normal * offsetDist;
                Point2d o1 = p1 + offset;
                Point2d o2 = p2 + offset;
                offsetLines.Add(new Line2d(o1, o2));
            }

            for (int i = 0; i < offsetLines.Count; i++)
            {
                Line2d prev = offsetLines[(i - 1 + offsetLines.Count) % offsetLines.Count];
                Line2d current = offsetLines[i];
                Point2d[] inters = prev.IntersectWith(current);
                Point2d inter = inters.Length == 0 ? current.StartPoint : inters.FirstOrDefault();
                result.AddVertexAt(result.NumberOfVertices, inter, 0, 0, 0);
            }
            result.Closed = true;
            return result;
        }


        private static List<Point3d> GenerateBuildingSamplePoints(List<Polyline> buildings, dynamic triangles, double sampleDist, double offsetDist)
        {
            List<Point3d> result = new List<Point3d>();

            foreach (Polyline building in buildings)
            {
                Polyline offsetPoly = CreateInnerOffsetPolyline(building, offsetDist);

                for (int i = 0; i < offsetPoly.NumberOfVertices; i++)
                {
                    Point2d p1 = offsetPoly.GetPoint2dAt(i);

                    Point2d p2 = offsetPoly.GetPoint2dAt((i + 1) % offsetPoly.NumberOfVertices);

                    Vector2d dir = p2 - p1;

                    double len = dir.Length;

                    if (len < EPS)
                    {
                        continue;
                    }

                    dir = dir.GetNormal();

                    for (double d = 0; d <= len; d += sampleDist)
                    {
                        Point2d pt2d = p1 + (dir * d);

                        Point3d pt = new Point3d(pt2d.X, pt2d.Y, 0);

                        // sécurité
                        if (!pt.IsInsidePolyline(building))
                        {
                            continue;
                        }

                        double z = InterpolateZFromTriangles(pt, triangles);

                        result.Add(new Point3d(pt.X, pt.Y, z));
                    }
                }

                offsetPoly.Dispose();
            }

            return result;
        }

        private static List<Point3d> GenerateBuildingPeakPoints(List<Polyline> buildings, double sampleDist, double offsetDist, double z)
        {
            List<Point3d> result =
                new List<Point3d>();

            foreach (Polyline building in buildings)
            {
                Polyline offsetPoly = CreateInnerOffsetPolyline(building, offsetDist);
                offsetPoly.AddToDrawing(3);
                for (int i = 0; i < offsetPoly.NumberOfVertices; i++)
                {
                    Point2d p1 = offsetPoly.GetPoint2dAt(i);

                    Point2d p2 = offsetPoly.GetPoint2dAt((i + 1) % offsetPoly.NumberOfVertices);

                    Vector2d dir = p2 - p1;

                    double len = dir.Length;

                    if (len < EPS)
                    {
                        continue;
                    }

                    dir = dir.GetNormal();

                    for (double d = 0; d <= len; d += sampleDist)
                    {
                        Point2d pt2d = p1 + (dir * d);

                        Point3d pt = new Point3d(pt2d.X, pt2d.Y, z);

                        // sécurité anti points extérieurs
                        if (!pt.IsInsidePolyline(building))
                        {
                            continue;
                        }

                        if (building.GetClosestPointTo(pt, false).DistanceTo(pt) < offsetDist - .1)
                        {
                            continue;
                        }

                        result.Add(pt);
                    }
                }

                offsetPoly.Dispose();
            }

            return result;
        }


        private static double InterpolateZFromTriangles(Point3d pt, dynamic triangles)
        {
            foreach (var tri in triangles)
            {
                Point3d a = tri.Vertex1;
                Point3d b = tri.Vertex2;
                Point3d c = tri.Vertex3;

                if (PointInsideTriangleXY(pt, a, b, c))
                {
                    return InterpolateBarycentricZ(pt, a, b, c);
                }
            }

            return 0;
        }

        private static bool PointInsideTriangleXY(Point3d p, Point3d a, Point3d b, Point3d c)
        {
            double denominator =
                ((b.Y - c.Y) * (a.X - c.X)) +
                ((c.X - b.X) * (a.Y - c.Y));

            double alpha =
                (((b.Y - c.Y) * (p.X - c.X)) +
                ((c.X - b.X) * (p.Y - c.Y)))
                / denominator;

            double beta =
                (((c.Y - a.Y) * (p.X - c.X)) +
                ((a.X - c.X) * (p.Y - c.Y)))
                / denominator;

            double gamma = 1.0 - alpha - beta;

            return
                alpha >= -EPS &&
                beta >= -EPS &&
                gamma >= -EPS;
        }

        private static double InterpolateBarycentricZ(Point3d p, Point3d a, Point3d b, Point3d c)
        {
            double denominator =
                ((b.Y - c.Y) * (a.X - c.X)) +
                ((c.X - b.X) * (a.Y - c.Y));

            double alpha =
                (((b.Y - c.Y) * (p.X - c.X)) +
                ((c.X - b.X) * (p.Y - c.Y)))
                / denominator;

            double beta =
                (((c.Y - a.Y) * (p.X - c.X)) +
                ((a.X - c.X) * (p.Y - c.Y)))
                / denominator;

            double gamma = 1.0 - alpha - beta;

            return
                (alpha * a.Z) +
                (beta * b.Z) +
                (gamma * c.Z);
        }


        private static Dictionary<double, List<Tuple<Point3d, Point3d>>>
            GenerateContourSegments(
            dynamic triangles,
            double intervalle)
        {
            Dictionary<double, List<Tuple<Point3d, Point3d>>> result = new Dictionary<double, List<Tuple<Point3d, Point3d>>>();

            foreach (var tri in triangles)
            {
                Point3d p1 = tri.Vertex1;
                Point3d p2 = tri.Vertex2;
                Point3d p3 = tri.Vertex3;

                double minZ = Math.Min(p1.Z, Math.Min(p2.Z, p3.Z));

                double maxZ = Math.Max(p1.Z, Math.Max(p2.Z, p3.Z));

                double startZ = Math.Ceiling(minZ / intervalle) * intervalle;

                double endZ = Math.Floor(maxZ / intervalle) * intervalle;

                for (double z = startZ;
                     z <= endZ + EPS;
                     z += intervalle)
                {
                    double zKey = Math.Round(z, 4);

                    List<Point3d> intersections = GetTriangleContourIntersections(p1, p2, p3, z);

                    if (intersections.Count == 2)
                    {
                        if (!result.TryGetValue(zKey, out List<Tuple<Point3d, Point3d>> value))
                        {
                            value = new List<Tuple<Point3d, Point3d>>();
                            result[zKey] = value;
                        }

                        value.Add(new Tuple<Point3d, Point3d>(intersections[0], intersections[1]));
                    }
                }
            }

            return result;
        }


        private static bool IsContourInsideBuildings(Polyline3d poly, List<Polyline> buildings)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (PolylineVertex3d v in poly)
            {
                if (v != null)
                {
                    pts.Add(v.Position);
                }
            }

            foreach (Polyline building in buildings)
            {
                bool allInside = true;

                foreach (Point3d pt in pts)
                {
                    if (!pt.IsInsidePolyline(building))
                    {
                        allInside = false;
                        break;
                    }
                }

                if (allInside)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsClockwise(Polyline pl)
        {
            double area = 0;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p1 = pl.GetPoint2dAt(i);
                Point2d p2 = pl.GetPoint2dAt((i + 1) % pl.NumberOfVertices);

                area += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

            return area > 0;
        }

        private static List<Point3dCollection> ChainSegments(List<Tuple<Point3d, Point3d>> segments)
        {
            List<Point3dCollection> polylines = new List<Point3dCollection>();

            const double tol = 1e-5;

            List<Tuple<Point3d, Point3d>> pool = new List<Tuple<Point3d, Point3d>>(segments);

            while (pool.Count > 0)
            {
                List<Point3d> currentChain = new List<Point3d>();

                var firstSeg = pool[0];

                pool.RemoveAt(0);

                currentChain.Add(firstSeg.Item1);
                currentChain.Add(firstSeg.Item2);

                ExtendChain(currentChain, pool, tol);

                currentChain.Reverse();

                ExtendChain(currentChain, pool, tol);

                Point3dCollection pts = new Point3dCollection();

                foreach (Point3d p in currentChain)
                {
                    pts.Add(p);
                }

                polylines.Add(pts);
            }

            return polylines;
        }

        private static void ExtendChain(List<Point3d> chain, List<Tuple<Point3d, Point3d>> pool, double tol)
        {
            bool added = true;

            while (added)
            {
                added = false;

                Point3d tail = chain[chain.Count - 1];

                for (int i = 0; i < pool.Count; i++)
                {
                    var seg = pool[i];

                    if (seg.Item1.DistanceTo(tail) < tol)
                    {
                        chain.Add(seg.Item2);
                        pool.RemoveAt(i);
                        added = true;
                        break;
                    }

                    if (seg.Item2.DistanceTo(tail) < tol)
                    {
                        chain.Add(seg.Item1);
                        pool.RemoveAt(i);
                        added = true;
                        break;
                    }
                }
            }
        }

        private static List<Point3d> GetTriangleContourIntersections(Point3d p1, Point3d p2, Point3d p3, double z)
        {
            List<Point3d> pts = new List<Point3d>();

            void AddPt(Point3d pt)
            {
                foreach (Point3d p in pts)
                {
                    if (p.DistanceTo(pt) < EPS)
                    {
                        return;
                    }
                }

                pts.Add(pt);
            }

            void CheckEdge(Point3d a, Point3d b)
            {
                bool aOn = Math.Abs(a.Z - z) < EPS;

                bool bOn = Math.Abs(b.Z - z) < EPS;

                if (aOn && bOn)
                {
                    AddPt(new Point3d(a.X, a.Y, z));
                    AddPt(new Point3d(b.X, b.Y, z));
                }
                else if (aOn)
                {
                    AddPt(new Point3d(a.X, a.Y, z));
                }
                else if (bOn)
                {
                    AddPt(new Point3d(b.X, b.Y, z));
                }
                else if (
                    (a.Z < z && b.Z > z) ||
                    (b.Z < z && a.Z > z))
                {
                    double t = (z - a.Z) / (b.Z - a.Z);

                    AddPt(new Point3d(a.X + (t * (b.X - a.X)), a.Y + (t * (b.Y - a.Y)), z));
                }
            }

            CheckEdge(p1, p2);
            CheckEdge(p2, p3);
            CheckEdge(p3, p1);

            return pts;
        }
    }
}
