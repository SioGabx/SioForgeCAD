using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class LINESAVERAGE
    {
        public static (Polyline First, Polyline Second) AcquirePoly()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var FirstPoly = ed.GetPolyline("Selectionnez une première polyline", false, true, true);
                if (FirstPoly is null) { return (null, null); }
                var SecondPoly = ed.GetPolyline("Selectionnez une deuxième polyline", false, true, true);
                if (SecondPoly is null) { return (null, null); }

                if (FirstPoly.IsClockwise() != SecondPoly.IsClockwise())
                {
                    Debug.WriteLine("Reversed");
                    SecondPoly.ReverseCurve();
                }
                tr.Commit();
                return (FirstPoly, SecondPoly);
            }

        }
        public static void Compute()
        {
            //https://github.com/Thought-Weaver/Frechet-Distance/blob/master/Program.cs
            //https://mathoverflow.net/questions/96415/polyline-averaging
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var Polys = AcquirePoly();
                var FirstPoly = Polys.First;
                var SecondPoly = Polys.Second;
                if (FirstPoly is null || SecondPoly is null) { return; }
                List<double> ListOfDistances = new List<double>();
                using (var LowFirstPoly = FirstPoly.ToPolygon(15))
                using (var LowSecondPoly = SecondPoly.ToPolygon(15))
                {
                    void AddDistances(Polyline TargetPoly)
                    {
                        for (int VerticeIndex = 0; VerticeIndex < TargetPoly.GetReelNumberOfVertices(); VerticeIndex++)
                        {
                            var LineSegment = TargetPoly.GetLineSegmentAt(VerticeIndex);
                            if (LineSegment.HasStartPoint)
                            {
                                ListOfDistances.Add(TargetPoly.GetDistAtPoint(LineSegment.StartPoint));
                            }
                            if (LineSegment.HasEndPoint)
                            {
                                ListOfDistances.Add(TargetPoly.GetDistAtPoint(LineSegment.EndPoint));
                            }
                        }
                    }
                    AddDistances(LowFirstPoly);
                    AddDistances(LowSecondPoly);

                    var UniquesListOfDistance = ListOfDistances.Distinct().OrderBy(x => x);

                    var newPoly = new Polyline();

                    foreach (var Distance in UniquesListOfDistance)
                    {
                        try
                        {
                            var MinAvRoadDist = Distance * Math.Min(SecondPoly.Length, FirstPoly.Length) / Math.Max(SecondPoly.Length, FirstPoly.Length);
                            var FirstPolyAvRoadDist = FirstPoly.Length >= SecondPoly.Length ? Distance : MinAvRoadDist;
                            var SecondPolyAvRoadDist = SecondPoly.Length >= FirstPoly.Length ? Distance : MinAvRoadDist;

                            Point3d pt1 = FirstPoly.GetPointAtDist(FirstPolyAvRoadDist);
                            Point3d pt2 = SecondPoly.GetPointAtDist(SecondPolyAvRoadDist);

                            Point3d avgPt = new Point3d(
                                (pt1.X + pt2.X) / 2,
                                (pt1.Y + pt2.Y) / 2,
                                (pt1.Z + pt2.Z) / 2
                            );
                            newPoly.AddVertex(avgPt);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            //Silent
                        }
                    }
                    newPoly.AddToDrawing();
                }
                FirstPoly.Dispose();
                SecondPoly.Dispose();
                tr.Commit();
            }
        }

        public static void ComputeFrechet()
        {
            var Polys = AcquirePoly();
            var FirstPoly = Polys.First;
            var SecondPoly = Polys.Second;
            if (FirstPoly is null || SecondPoly is null) { return; }


            List<double[]> P = new List<double[]>();
            List<double[]> Q = new List<double[]>();

            using (var LowFirstPoly = FirstPoly.ToPolygon(15))
            {
                foreach (var item in LowFirstPoly.GetPolyPoints())
                {
                    P.Add(new double[2] { item.X, item.Y });
                }
            }
            using (var LowSecondPoly = SecondPoly.ToPolygon(15))
            {
                foreach (var item in LowSecondPoly.GetPolyPoints())
                {
                    Q.Add(new double[2] { item.X, item.Y });
                }
            }

            //Frechet Distance Between P and Q
            double fDistance = FrechetDistance(P, Q);


        }


        public static double EuclideanDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static double ComputeDistance(double[,] distances, int i, int j, List<double[]> P, List<double[]> Q)
        {
            if (distances[i, j] > -1)
                return distances[i, j];

            if (i == 0 && j == 0)
                distances[i, j] = EuclideanDistance(P[0][0], P[0][1], Q[0][0], Q[0][1]);
            else if (i > 0 && j == 0)
                distances[i, j] = Math.Max(ComputeDistance(distances, i - 1, 0, P, Q),
                                           EuclideanDistance(P[i][0], P[i][1], Q[0][0], Q[0][1]));
            else if (i == 0 && j > 0)
                distances[i, j] = Math.Max(ComputeDistance(distances, 0, j - 1, P, Q),
                                           EuclideanDistance(P[0][0], P[0][1], Q[j][0], Q[j][1]));
            else if (i > 0 && j > 0)
                distances[i, j] = Math.Max(Math.Min(ComputeDistance(distances, i - 1, j, P, Q),
                                           Math.Min(ComputeDistance(distances, i - 1, j - 1, P, Q),
                                                    ComputeDistance(distances, i, j - 1, P, Q))),
                                                    EuclideanDistance(P[i][0], P[i][1], Q[j][0], Q[j][1]));
            else
                distances[i, j] = Double.PositiveInfinity;

            return distances[i, j];
        }

        public static double FrechetDistance(List<double[]> P, List<double[]> Q)
        {
            double[,] distances = new double[P.Count, Q.Count];
            for (int y = 0; y < P.Count; y++)
                for (int x = 0; x < Q.Count; x++)
                    distances[y, x] = -1;

            return ComputeDistance(distances, P.Count - 1, Q.Count - 1, P, Q);
        }




















































    }
}
