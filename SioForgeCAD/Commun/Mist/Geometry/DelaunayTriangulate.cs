using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static class DelaunayTriangulate
    {
        public struct Triangle3d
        {
            public Point3d Vertex1 { get; }
            public Point3d Vertex2 { get; }
            public Point3d Vertex3 { get; }

            public Triangle3d(Point3d v1, Point3d v2, Point3d v3)
            {
                Vertex1 = v1;
                Vertex2 = v2;
                Vertex3 = v3;
            }
        }

        // Internal helper structure to keep the algorithm fast and clean
        private struct InternalTriangle
        {
            public int P1, P2, P3;
            public double CentroidX, CentroidY, RadiusSq;
            public bool IsValid;
        }

        private struct Edge
        {
            public int P1, P2;
            public Edge(int p1, int p2) { P1 = p1; P2 = p2; }
        }

        public static void TriangulateCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            TypedValue[] filterVal = { new TypedValue(0, "POINT") };
            PromptSelectionResult selResult = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "Sélectionnez les points :" }, new SelectionFilter(filterVal));
            if (selResult.Status != PromptStatus.OK) return;

            List<Point3d> Nuage = new List<Point3d>();

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selResult.Value.GetObjectIds())
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is DBPoint point)
                    {
                        Nuage.Add(point.Position);
                    }
                }

                List<Triangle3d> trianglesCalcules = Triangulate(Nuage);

                ed.WriteMessage($"\nCalcul terminé. Nombre de triangles générés en mémoire : {trianglesCalcules.Count}");

                foreach (var triangle in trianglesCalcules)
                {
                    using (Polyline3d poly = new Polyline3d())
                    {
                        poly.AddToDrawingCurrentTransaction();
                        poly.AddVertex(triangle.Vertex1);
                        poly.AddVertex(triangle.Vertex2);
                        poly.AddVertex(triangle.Vertex3);
                        poly.Closed = true;
                    }
                }
                tr.Commit();
            }
        }

        public static List<Triangle3d> Triangulate(List<Point3d> nuagePoints)
        {
            List<Triangle3d> Resultat = new List<Triangle3d>();

            // Clean up duplicates
            List<Point3d> PtsFiltres = nuagePoints
                .GroupBy(p => new { p.X, p.Y })
                .Select(g => g.First())
                .ToList();

            int numberOfPoints = PtsFiltres.Count;
            if (numberOfPoints < 3)
            {
                return Resultat;
            }

            // Coordinate arrays containing input points + 3 points for the super-triangle
            double[] xCoordinates = new double[numberOfPoints + 3];
            double[] yCoordinates = new double[numberOfPoints + 3];
            double[] zCoordinates = new double[numberOfPoints + 3];

            for (int i = 0; i < numberOfPoints; i++)
            {
                xCoordinates[i] = PtsFiltres[i].X;
                yCoordinates[i] = PtsFiltres[i].Y;
                zCoordinates[i] = PtsFiltres[i].Z;
            }

            // Calculate Super-Triangle bounds
            double xMin = xCoordinates[0], xMax = xMin;
            double yMin = yCoordinates[0], yMax = yMin;
            for (int i = 1; i < numberOfPoints; i++)
            {
                if (xCoordinates[i] < xMin) xMin = xCoordinates[i];
                if (xCoordinates[i] > xMax) xMax = xCoordinates[i];
                if (yCoordinates[i] < yMin) yMin = yCoordinates[i];
                if (yCoordinates[i] > yMax) yMax = yCoordinates[i];
            }

            double deltaX = xMax - xMin;
            double deltaY = yMax - yMin;
            double xMid = (xMin + xMax) / 2;
            double yMid = (yMin + yMax) / 2;
            double dMax = Math.Max(deltaX, deltaY);

            // Append Super-Triangle coordinates at the end of our coordinate arrays
            int stIdx1 = numberOfPoints;
            int stIdx2 = numberOfPoints + 1;
            int stIdx3 = numberOfPoints + 2;

            xCoordinates[stIdx1] = xMid - 20 * dMax; yCoordinates[stIdx1] = yMid - dMax; zCoordinates[stIdx1] = 0;
            xCoordinates[stIdx2] = xMid; yCoordinates[stIdx2] = yMid + 20 * dMax; zCoordinates[stIdx2] = 0;
            xCoordinates[stIdx3] = xMid + 20 * dMax; yCoordinates[stIdx3] = yMid - dMax; zCoordinates[stIdx3] = 0;

            // Using a dynamic List of structs prevents IndexOutOfRange errors completely
            List<InternalTriangle> triangles = new List<InternalTriangle>();

            // Create initial super-triangle
            InternalTriangle superTriangle = new InternalTriangle { P1 = stIdx1, P2 = stIdx2, P3 = stIdx3, IsValid = true };
            CalculateCircumscribedCircle(
                xCoordinates[stIdx1], yCoordinates[stIdx1],
                xCoordinates[stIdx2], yCoordinates[stIdx2],
                xCoordinates[stIdx3], yCoordinates[stIdx3],
                ref superTriangle.CentroidX, ref superTriangle.CentroidY, ref superTriangle.RadiusSq
            );
            triangles.Add(superTriangle);

            List<Edge> edgeBuffer = new List<Edge>();

            // Main loop: Incremental insertion
            for (int i = 0; i < numberOfPoints; i++)
            {
                double px = xCoordinates[i];
                double py = yCoordinates[i];
                edgeBuffer.Clear();

                // 1. Identify and invalidate triangles whose circumcircles contain the current point
                for (int j = 0; j < triangles.Count; j++)
                {
                    InternalTriangle t = triangles[j];
                    if (!t.IsValid) continue;

                    double dx = t.CentroidX - px;
                    double dy = t.CentroidY - py;
                    if ((dx * dx + dy * dy) < t.RadiusSq)
                    {
                        // Add edges to buffer
                        edgeBuffer.Add(new Edge(t.P1, t.P2));
                        edgeBuffer.Add(new Edge(t.P2, t.P3));
                        edgeBuffer.Add(new Edge(t.P3, t.P1));

                        t.IsValid = false;
                        triangles[j] = t; // Struct updating
                    }
                }

                // 2. Remove duplicate edges (shared edges within the polygonal hole)
                for (int j = 0; j < edgeBuffer.Count - 1; j++)
                {
                    for (int k = j + 1; k < edgeBuffer.Count; k++)
                    {
                        Edge e1 = edgeBuffer[j];
                        Edge e2 = edgeBuffer[k];
                        if ((e1.P1 == e2.P2 && e1.P2 == e2.P1) || (e1.P1 == e2.P1 && e1.P2 == e2.P2))
                        {
                            e1.P1 = -1; e1.P2 = -1;
                            e2.P1 = -1; e2.P2 = -1;
                            edgeBuffer[j] = e1;
                            edgeBuffer[k] = e2;
                        }
                    }
                }

                // 3. Form new triangles from unique boundary edges to the new point
                for (int j = 0; j < edgeBuffer.Count; j++)
                {
                    Edge e = edgeBuffer[j];
                    if (e.P1 < 0 || e.P2 < 0) continue;

                    InternalTriangle newTri = new InternalTriangle { P1 = e.P1, P2 = e.P2, P3 = i, IsValid = true };
                    CalculateCircumscribedCircle(
                        xCoordinates[e.P1], yCoordinates[e.P1],
                        xCoordinates[e.P2], yCoordinates[e.P2],
                        xCoordinates[i], yCoordinates[i],
                        ref newTri.CentroidX, ref newTri.CentroidY, ref newTri.RadiusSq
                    );
                    triangles.Add(newTri);
                }

                // Clean up invalid items to keep memory footprint low
                triangles.RemoveAll(t => !t.IsValid);
            }

            // 4. Convert internal valid triangles back to AutoCAD Point3d elements, ignoring super-triangle vertices
            foreach (var t in triangles)
            {
                if (t.IsValid && t.P1 < numberOfPoints && t.P2 < numberOfPoints && t.P3 < numberOfPoints)
                {
                    Point3d v1 = new Point3d(xCoordinates[t.P1], yCoordinates[t.P1], zCoordinates[t.P1]);
                    Point3d v2 = new Point3d(xCoordinates[t.P2], yCoordinates[t.P2], zCoordinates[t.P2]);
                    Point3d v3 = new Point3d(xCoordinates[t.P3], yCoordinates[t.P3], zCoordinates[t.P3]);
                    Resultat.Add(new Triangle3d(v1, v2, v3));
                }
            }

            return Resultat;
        }

        private static bool CalculateCircumscribedCircle(double x1, double y1, double x2, double y2, double x3, double y3, ref double xc, ref double yc, ref double r)
        {
            const double eps = 1e-6;
            double m1, m2, mx1, mx2, my1, my2;

            if (Math.Abs(y2 - y1) < eps)
            {
                m2 = -(x3 - x2) / (y3 - y2);
                mx2 = (x2 + x3) / 2; my2 = (y2 + y3) / 2;
                xc = (x2 + x1) / 2; yc = (m2 * (xc - mx2)) + my2;
            }
            else if (Math.Abs(y3 - y2) < eps)
            {
                m1 = -(x2 - x1) / (y2 - y1);
                mx1 = (x1 + x2) / 2; my1 = (y1 + y2) / 2;
                xc = (x3 + x2) / 2; yc = (m1 * (xc - mx1)) + my1;
            }
            else
            {
                m1 = -(x2 - x1) / (y2 - y1);
                m2 = -(x3 - x2) / (y3 - y2);
                if (Math.Abs(m1 - m2) < eps) { xc = x1; yc = y1; return false; }
                mx1 = (x1 + x2) / 2; mx2 = (x2 + x3) / 2;
                my1 = (y1 + y2) / 2; my2 = (y2 + y3) / 2;
                xc = ((m1 * mx1) - (m2 * mx2) + my2 - my1) / (m1 - m2);
                yc = (m1 * (xc - mx1)) + my1;
            }
            r = ((x2 - xc) * (x2 - xc)) + ((y2 - yc) * (y2 - yc));
            return true;
        }
    }
}