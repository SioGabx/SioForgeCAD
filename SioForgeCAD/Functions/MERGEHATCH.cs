using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class MERGEHATCH_V2
    {
        public static void Merge()
        {
            Editor ed = Generic.GetEditor();

            if (!ed.GetHatch(out Hatch FirstHachure, "Veuillez selectionner une première hachure"))
            {
                return;
            }
            if (!ed.GetHatch(out Hatch SecondHachure, "Veuillez selectionner une deuxième hachure"))
            {
                return;
            }

            if (!FirstHachure.GetPolyHole(out var FirstHachurePolyHole) || !SecondHachure.GetPolyHole(out var SecondHachurePolyHole))
            {
                return;
            }



            if (PolygonOperation.Union(new List<PolyHole>() { FirstHachurePolyHole, SecondHachurePolyHole }, out var unionResult))
            {
                Database db = Generic.GetDatabase();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var item in unionResult)
                    {
                        item.Holes.AddToDrawing(3);
                        item.Boundary.AddToDrawing(4);
                    }
                    tr.Commit();
                }
            }
            FirstHachurePolyHole.Dispose();
            SecondHachurePolyHole.Dispose();

        }



    }



    public static class MERGEHATCH
    {
        const double Margin = 0.01;
        public static void Merge()
        {
            Editor ed = Generic.GetEditor();

            // ed.TraceBoundary(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0), false);
            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
                return;

            SelectionSet sel = selRes.Value;
            List<Curve> Curves = new List<Curve>();

            //ed.GetPoint("Indiquez un point");
            //var CurrentViewSave = ed.GetCurrentView();

            Document doc = Generic.GetDocument();
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId selectedObjectId in sel.GetObjectIds())
                {
                    DBObject ent = selectedObjectId.GetDBObject();
                    if (ent is Polyline || ent is Circle)
                    {
                        Curve curv = ent.Clone() as Curve;
                        Curves.Add(curv);
                    }
                }
                if (Curves.Count <= 0)
                {
                    return;
                }

                MergePolylinePoints(Curves);
                List<Curve> MergedCurves = Curves.RegionMerge();

                if (MergedCurves.Count > 1)
                {
                    List<Curve> OffsetCurves = Curves.OffsetPolyline(Margin, false);
                    List<Curve> OffsetCurvesWithIntersections = OffsetCurves.RegionMerge();
                    List<Curve> UndoOffsetCurves = OffsetCurvesWithIntersections.OffsetPolyline(Margin, true);

                    MergedCurves = UndoOffsetCurves;
                }

                MergedCurves.AddToDrawing();
                MergePolylinePoints(MergedCurves);
                foreach (Curve curve in MergedCurves)
                {
                    if (curve is Polyline poly)
                    {
                        poly.Cleanup();
                    }
                }

                tr.Commit();
                return;
            }
        }


        private static void MergePolylinePoints(List<Curve> curves)
        {
            List<Polyline> polylines = curves.Where(ent => ent is Polyline).Cast<Polyline>().ToList();


            // Parcourir chaque paire de polylignes
            for (int i = 0; i < polylines.Count - 1; i++)
            {
                for (int j = i + 1; j < polylines.Count; j++)
                {
                    Polyline polylineA = polylines[i];
                    Polyline polylineB = polylines[j];

                    // Parcourir chaque point de la polyligne A
                    for (int k = 0; k < polylineA.NumberOfVertices; k++)
                    {
                        Point2d pointA = polylineA.GetPoint2dAt(k);

                        // Parcourir chaque point de la polyligne B
                        for (int l = 0; l < polylineB.NumberOfVertices; l++)
                        {
                            Point2d pointB = polylineB.GetPoint2dAt(l);

                            // Vérifier si la distance entre les points est inférieure à 0.005
                            if (pointA.GetDistanceTo(pointB) < Margin)
                            {
                                // Ajuster le point de la polyligne A pour coïncider avec celui de la polyligne B
                                polylineA.SetPointAt(k, pointB);
                                break; // Passer au point suivant dans la polyligne A
                            }
                        }
                    }
                }
            }
        }

    }
}
