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

            var pkr = ed.GetOptions("Voulez-vous lisser les courbes de niveau (Splines) ? ", false, "Oui", "Non");
            if (pkr.Status != PromptStatus.OK)
            {
                return;
            }

            bool createSplines = (pkr.StringResult == "Oui");

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (LongOperationProcess op = new LongOperationProcess("Création des courbes..."))
            {
                try
                {
                    List<Entity> createdEntities = new List<Entity>();


                    List<Polyline> buildings = ReadBuildings(tr, selBuildings.Value);
                    List<Point3d> terrainPoints = ReadTopoPoints(tr, ObjIds, buildings);
                    if (terrainPoints.Count < 3)
                    {
                        Generic.WriteMessage("Vous devez sélectionner au moins 3 points.");
                        return;
                    }

                    // estimation progression
                    int totalOps =
                        terrainPoints.Count +
                        terrainPoints.Count + // triangulation approx
                        (buildings.Count * 200) +
                        5000;

                    op.SetTotalOperations(totalOps);

                    var baseTriangles = DelaunayTriangulate.Triangulate(terrainPoints, op);

                    List<Point3d> sampledPoints = new List<Point3d>();
                    List<Point3d> roofPoints = new List<Point3d>();

                    foreach (Polyline building in buildings)
                    {
                        op.CheckCanceled();

                        var basePts = GenerateSingleBuildingSamplePoints(building, baseTriangles, 0.25, 0.50, op);
                        sampledPoints.AddRange(basePts);

                        double maxLocalZ = basePts.Count > 0 ? basePts.Max(p => p.Z) : terrainPoints.Max(p => p.Z);
                        double targetRoofZ = maxLocalZ + 1.0;

                        roofPoints.AddRange(
                            GenerateSingleBuildingPeakPoints(building, 1, 1.5, targetRoofZ, op)
                        );

                        op.UpdateProgress();
                    }

                    List<Point3d> finalPoints = new List<Point3d>();
                    finalPoints.AddRange(terrainPoints);
                    finalPoints.AddRange(sampledPoints);
                    finalPoints.AddRange(roofPoints);

                    finalPoints.ForEach(pt => pt.AddToDrawing(5));
                    var finalTriangles = DelaunayTriangulate.Triangulate(finalPoints, op);

                    var segmentsByZ = GenerateContourSegments(finalTriangles, intervalle);

                    List<Entity> resultEntities = new List<Entity>();

                    foreach (var kvp in segmentsByZ)
                    {
                        op.CheckCanceled();

                        double reste = Math.Abs(kvp.Key % intervallePrincipal);
                        bool main = reste < 1e-4 || Math.Abs(reste - intervallePrincipal) < 1e-4;

                        LineWeight lw = main ? LineWeight.LineWeight050 : LineWeight.LineWeight000;

                        foreach (Point3dCollection path in ChainSegments(kvp.Value))
                        {
                            if (path.Count <= 2)
                            {
                                continue;
                            }

                            bool closed = path[0].DistanceTo(path[path.Count - 1]) < 1e-5;

                            if (closed)
                            {
                                path.RemoveAt(path.Count - 1);
                            }

                            Polyline3d poly = new Polyline3d(
                                createSplines ? Poly3dType.QuadSplinePoly : Poly3dType.SimplePoly,
                                path,
                                closed)
                            {
                                LineWeight = lw
                            };

                            if (IsContourInsideBuildings(poly, buildings))
                            {
                                poly.Dispose();
                                continue;
                            }

                            resultEntities.Add(poly);
                        }

                        op.UpdateProgress();
                    }

                    if (resultEntities.Count > 0)
                    {
                        var blkDefId = BlockReferences.Create(
                            nameof(CREATECONTOURSLINESFROMPOINTS) + "_" + DateTime.Now.Ticks,
                            "Contours générés",
                            resultEntities.ToDBObjectCollection(),
                            Points.Empty,
                            true,
                            BlockScaling.Uniform);

                        if (blkDefId.IsValid)
                        {
                            BlockReference br = new BlockReference(Point3d.Origin, blkDefId);
                            br.AddToDrawingCurrentTransaction();
                            ed.SetImpliedSelection(new[] { br.ObjectId });
                        }
                    }

                    Generic.WriteMessage($"{resultEntities.Count} courbes générées.");
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    Debug.WriteLine(ex.InnerException);
                    Debug.WriteLine(ex.StackTrace);
                    Debug.WriteLine(ex.Source);
                    Generic.WriteMessage($"Erreur : {ex.Message}");
                    tr.Abort();
                }
            }
        }

        private static List<Point3d> GenerateSingleBuildingSamplePoints(Polyline building, List<DelaunayTriangulate.Triangle3d> triangles, double sampleDist, double offsetDist, LongOperationProcess op)
        {
            List<Point3d> result = new List<Point3d>();

            Polyline offset = CreateInnerOffsetPolyline(building, offsetDist);
            var buildingExtends = building.GetExtents();
            for (int i = 0; i < offset.NumberOfVertices; i++)
            {
                op.CheckCanceled();

                Point2d p1 = offset.GetPoint2dAt(i);
                Point2d p2 = offset.GetPoint2dAt((i + 1) % offset.NumberOfVertices);

                Vector2d dir = (p2 - p1);
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

                    if (!buildingExtends.ContainsIgnoreZ(pt))
                    {
                        continue;
                    }

                    if (!pt.IsInsidePolyline(building))
                    {
                        continue;
                    }

                    var distanceFromBuilding = building.GetClosestPointTo(pt, false).ToPoint2d().GetDistanceTo(pt.ToPoint2d());
                    if (distanceFromBuilding < offsetDist - 0.1)
                    {
                        continue;
                    }

                    if (distanceFromBuilding > offsetDist + 0.1)
                    {
                        continue;
                    }

                    double z = InterpolateZFromTriangles(pt, triangles);
                    result.Add(new Point3d(pt.X, pt.Y, z));

                    op.UpdateProgress();
                }
            }

            offset.Dispose();
            return result;
        }

        private static List<Point3d> GenerateSingleBuildingPeakPoints(Polyline building, double sampleDist, double offsetDist, double z, LongOperationProcess op)
        {
            List<Point3d> result = new List<Point3d>();

            Polyline offset = CreateInnerOffsetPolyline(building, offsetDist);
            var buildingExtends = building.GetExtents();
            for (int i = 0; i < offset.NumberOfVertices; i++)
            {
                op.CheckCanceled();

                Point2d p1 = offset.GetPoint2dAt(i);
                Point2d p2 = offset.GetPoint2dAt((i + 1) % offset.NumberOfVertices);

                Vector2d dir = (p2 - p1);
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

                    if (!buildingExtends.ContainsIgnoreZ(pt))
                    {
                        continue;
                    }

                    if (!pt.IsInsidePolyline(building))
                    {
                        continue;
                    }

                    var distanceFromBuilding = building.GetClosestPointTo(pt, false).ToPoint2d().GetDistanceTo(pt.ToPoint2d());
                    Debug.WriteLine($"distanceFromBuilding : {distanceFromBuilding}\noffsetDist : {offsetDist}\n\n");
                    if (distanceFromBuilding < offsetDist - 0.1)
                    {
                        continue;
                    }

                    if (distanceFromBuilding > offsetDist + 1)
                    {
                        continue;
                    }

                    result.Add(pt);
                    op.UpdateProgress();
                }
            }

            offset.Dispose();
            return result;
        }

        private static double InterpolateZFromTriangles(Point3d pt, List<DelaunayTriangulate.Triangle3d> triangles)
        {
            foreach (var tri in triangles)
            {
                if (PointInsideTriangleXY(pt, tri.Vertex1, tri.Vertex2, tri.Vertex3))
                {
                    return InterpolateBarycentricZ(pt, tri.Vertex1, tri.Vertex2, tri.Vertex3);
                }
            }
            return 0;
        }

        private static bool PointInsideTriangleXY(Point3d p, Point3d a, Point3d b, Point3d c)
        {
            double denom = ((b.Y - c.Y) * (a.X - c.X)) + ((c.X - b.X) * (a.Y - c.Y));
            double alpha = (((b.Y - c.Y) * (p.X - c.X)) + ((c.X - b.X) * (p.Y - c.Y))) / denom;
            double beta = (((c.Y - a.Y) * (p.X - c.X)) + ((a.X - c.X) * (p.Y - c.Y))) / denom;
            double gamma = 1 - alpha - beta;

            return alpha >= -EPS && beta >= -EPS && gamma >= -EPS;
        }

        private static double InterpolateBarycentricZ(Point3d p, Point3d a, Point3d b, Point3d c)
        {
            double denom = ((b.Y - c.Y) * (a.X - c.X)) + ((c.X - b.X) * (a.Y - c.Y));
            double alpha = (((b.Y - c.Y) * (p.X - c.X)) + ((c.X - b.X) * (p.Y - c.Y))) / denom;
            double beta = (((c.Y - a.Y) * (p.X - c.X)) + ((a.X - c.X) * (p.Y - c.Y))) / denom;
            double gamma = 1 - alpha - beta;

            return (alpha * a.Z) + (beta * b.Z) + (gamma * c.Z);
        }

        private static List<Point3d> ReadTopoPoints(Transaction tr, ObjectId[] ids, List<Polyline> buildings)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (ObjectId id in ids)
            {
                if (!(tr.GetObject(id, OpenMode.ForRead) is BlockReference br))
                {
                    continue;
                }

                foreach (ObjectId attId in br.AttributeCollection)
                {
                    if (!(tr.GetObject(attId, OpenMode.ForRead) is AttributeReference att))
                    {
                        continue;
                    }

                    if (Regex.IsMatch(att.TextString, @"^\d+\.\d{2,}$"))
                    {
                        double z = Convert.ToDouble(att.TextString);
                        pts.Add(new Point3d(br.Position.X, br.Position.Y, z));
                        break;
                    }
                }
            }
            foreach (Polyline building in buildings)
            {
                var buildingExtends = building.GetExtents();
                foreach (var pt in pts.ToArray())
                {

                    if (!buildingExtends.ContainsIgnoreZ(pt))
                    {
                        continue;
                    }

                    if (pt.IsInsidePolyline(building))
                    {
                        var distanceFromBuilding = building.GetClosestPointTo(pt, false).ToPoint2d().GetDistanceTo(pt.ToPoint2d());
                        if (distanceFromBuilding > 0.5)
                        {
                            pts.Remove(pt);
                        }
                    }

                }
            }

            return pts;
        }

        private static List<Polyline> ReadBuildings(Transaction tr, object sel)
        {
            List<Polyline> result = new List<Polyline>();

            if (!(sel is SelectionSet set))
            {
                return result;
            }

            foreach (SelectedObject so in set)
            {
                if (so == null)
                {
                    continue;
                }

                if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is Polyline pl && pl.Closed)
                {
                    result.Add(pl);
                }
            }

            return result;
        }

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

                dir /= len;
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
                if (inters != null)
                {
                    Point2d inter = inters.Length == 0 ? current.StartPoint : inters.FirstOrDefault();
                    result.AddVertexAt(result.NumberOfVertices, inter, 0, 0, 0);
                }
            }
            result.Closed = true;
            return result;
        }

        private static Dictionary<double, List<Tuple<Point3d, Point3d>>> GenerateContourSegments(dynamic triangles, double intervalle)
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

                for (double z = startZ; z <= endZ + EPS; z += intervalle)
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
                var buildingExtend = building.GetExtents();
                if (poly.GetExtents().IsFullyInside(buildingExtend))
                {
                    continue;
                }

                foreach (Point3d pt in pts)
                {
                    if (!buildingExtend.ContainsIgnoreZ(pt))
                    {
                        allInside = false;
                        break;
                    }

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
            Vector3d normalPlane = Vector3d.ZAxis;

            while (pool.Count > 0)
            {
                List<Point3d> currentChain = new List<Point3d>();
                var firstSeg = pool[0];
                pool.RemoveAt(0);

                currentChain.Add(firstSeg.Item1);
                currentChain.Add(firstSeg.Item2);

                // --- 1ÈRE ÉTAPE : Extension vers l'avant (Tail) ---
                ExtendChain(currentChain, pool, tol, normalPlane);

                // --- 2ÈME ÉTAPE : Extension vers l'arrière (Head) ---
                currentChain.Reverse();
                ExtendChain(currentChain, pool, tol, normalPlane);

                Point3dCollection pts = new Point3dCollection();
                foreach (Point3d p in currentChain)
                {
                    pts.Add(p);
                }
                polylines.Add(pts);
            }
            return polylines;
        }

        private static void ExtendChain(List<Point3d> chain, List<Tuple<Point3d, Point3d>> pool, double tol, Vector3d normalPlane)
        {
            bool added = true;
            while (added)
            {
                added = false;
                int bestIdx = -1;
                bool reverseSeg = false;
                double maxAngle = double.MaxValue;

                Point3d tail = chain[chain.Count - 1];

                // Si la chaîne n'a qu'un point (ce qui ne devrait pas arriver ici, mais par sécurité)
                if (chain.Count < 2)
                {
                    break;
                }

                Point3d previousPoint = chain[chain.Count - 2];
                Vector3d currentDir = previousPoint.GetVectorTo(tail).GetNormal();

                for (int i = 0; i < pool.Count; i++)
                {
                    var seg = pool[i];
                    Vector3d candidateDir = new Vector3d();
                    bool isCandidate = false;
                    bool tempReverse = false;

                    if (seg.Item1.DistanceTo(tail) < tol)
                    {
                        candidateDir = seg.Item1.GetVectorTo(seg.Item2).GetNormal();
                        isCandidate = true;
                        tempReverse = false;
                    }
                    else if (seg.Item2.DistanceTo(tail) < tol)
                    {
                        candidateDir = seg.Item2.GetVectorTo(seg.Item1).GetNormal();
                        isCandidate = true;
                        tempReverse = true;
                    }

                    if (isCandidate)
                    {
                        // Évite les demi-tours brutaux
                        if (candidateDir.DotProduct(currentDir) < -0.999)
                        {
                            continue;
                        }

                        double angle = GetSignedAngle(currentDir, candidateDir, normalPlane);

                        if (angle < maxAngle)
                        {
                            maxAngle = angle;
                            bestIdx = i;
                            reverseSeg = tempReverse;
                        }
                    }
                }

                if (bestIdx != -1)
                {
                    var bestSeg = pool[bestIdx];
                    pool.RemoveAt(bestIdx);
                    chain.Add(reverseSeg ? bestSeg.Item1 : bestSeg.Item2);
                    added = true;
                }
            }
        }

        private static double GetSignedAngle(Vector3d v1, Vector3d v2, Vector3d normal)
        {
            Vector3d v1Proj = v1 - (normal * v1.DotProduct(normal));
            Vector3d v2Proj = v2 - (normal * v2.DotProduct(normal));

            double dot = v1Proj.GetNormal().DotProduct(v2Proj.GetNormal());
            Vector3d cross = v1Proj.CrossProduct(v2Proj);

            return Math.Atan2(cross.DotProduct(normal), dot);
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
                else if ((a.Z < z && b.Z > z) || (b.Z < z && a.Z > z))
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