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
                var FirstPoly = ed.GetPolyline(out _, "Selectionnez une première polyline", false, true, true);
                if (FirstPoly is null) { return (null, null); }
                var SecondPoly = ed.GetPolyline(out _, "Selectionnez une deuxième polyline", false, true, true);
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
    }
}
