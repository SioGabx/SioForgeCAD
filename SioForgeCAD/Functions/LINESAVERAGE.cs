using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SioForgeCAD.Functions
{
    public static class LINESAVERAGE
    {
        public static void Compute()
        {
            //https://github.com/Thought-Weaver/Frechet-Distance/blob/master/Program.cs
            //https://mathoverflow.net/questions/96415/polyline-averaging
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var FirstPoly = ed.GetPolyline("Selectionnez une première polyline", false, false, true);
                if (FirstPoly is null) { return; }
                var SecondPoly = ed.GetPolyline("Selectionnez une deuxième polyline", false, false, true);
                if (SecondPoly is null) { return; }

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
                        Point3d pt1 = FirstPoly.GetPointAtDist(Distance);
                        Point3d pt2 = SecondPoly.GetPointAtDist(Distance);

                        Point3d avgPt = new Point3d(
                            (pt1.X + pt2.X) / 2,
                            (pt1.Y + pt2.Y) / 2,
                            (pt1.Z + pt2.Z) / 2
                        );
                        newPoly.AddVertex(avgPt);
                    }
                    newPoly.AddToDrawing();

                }
                tr.Commit();
            }
        }
    }
}
