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
                        if (ent is Polyline poly && poly?.IsSelfIntersecting() == true)
                        {
                            Generic.WriteMessage("Impossible de combiner une hachure qui se coupe elle-même.");
                            continue;
                        }
                        Curve curv = ent.Clone() as Curve;
                        Curves.Add(curv);
                    }
                }
                if (Curves.Count <= 0)
                {
                    return;
                }

                MergePolylinePoints(Curves);
                List<Curve> MergedCurves = Curves.Merge();

                if (MergedCurves.Count > 1)
                {
                    List<Curve> OffsetCurves = Curves.OffsetPolyline(Margin, false);
                    List<Curve> OffsetCurvesWithIntersections = OffsetCurves.Merge();
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


















        //private static void AdjustPointsAtIntersections(List<Polyline> polylines)
        //{
        //    List<Polyline> Parsed = new List<Polyline>();
        //    foreach (Polyline polyline in polylines)
        //    {
        //        Parsed.Add(polyline);
        //        for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
        //        {
        //            Point2d startPoint = polyline.GetPoint2dAt(i);
        //            Point2d endPoint = polyline.GetPoint2dAt(i + 1);

        //            Polyline segment = new Polyline();
        //            segment.AddVertex(startPoint.ToPoint3d());
        //            segment.AddVertex(endPoint.ToPoint3d());
        //            foreach (Polyline otherPolyline in polylines)
        //            {
        //                if (otherPolyline != polyline)
        //                {
        //                    for (int j = 0; j < otherPolyline.NumberOfVertices - 1; j++)
        //                    {
        //                        Point2d otherStartPoint = otherPolyline.GetPoint2dAt(j);
        //                        Point2d otherEndPoint = otherPolyline.GetPoint2dAt(j + 1);

        //                        Polyline otherSegment = new Polyline();
        //                        otherSegment.AddVertex(otherStartPoint.ToPoint3d());
        //                        otherSegment.AddVertex(otherEndPoint.ToPoint3d());

        //                        Point3dCollection intersectionsPoints = new Point3dCollection();
        //                        segment.IntersectWith(otherSegment, Intersect.ExtendBoth, intersectionsPoints, IntPtr.Zero, IntPtr.Zero);
        //                        if (intersectionsPoints?.Count > 0)
        //                        {
        //                            foreach (Point3d intersectionPoint3d in intersectionsPoints)
        //                            {
        //                                var intersectionPoint = intersectionPoint3d.ToPoint2d();
        //                                // Vérifier si l'intersection est suffisamment proche
        //                                if (startPoint.GetDistanceTo(intersectionPoint) < 0.1)
        //                                {
        //                                    AddVertex(intersectionPoint);
        //                                }
        //                                if (endPoint.GetDistanceTo(intersectionPoint) < 0.1)
        //                                {
        //                                    AddVertex(intersectionPoint);
        //                                }

        //                                bool AddVertex(Point2d point)
        //                                {
        //                                    if (!otherPolyline.GetPolyPoints().Contains(point))
        //                                    {
        //                                        otherPolyline.AddVertexAt(j + 1, point, 0, 0, 0);
        //                                        j = 0;
        //                                        return true;
        //                                    }
        //                                    return false;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private static void AdjustPointsAtIntersections(List<Polyline> polylines)
        //{
        //    foreach (Polyline polyline in polylines)
        //    {
        //        for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
        //        {
        //            Point2d startPoint = polyline.GetPoint2dAt(i);
        //            Point2d endPoint = polyline.GetPoint2dAt(i + 1);

        //            Polyline segment = new Polyline();
        //            segment.AddVertex(startPoint.ToPoint3d());
        //            segment.AddVertex(endPoint.ToPoint3d());

        //            foreach (Polyline otherPolyline in polylines)
        //            {
        //                if (otherPolyline != polyline)
        //                {
        //                    int j = 0; // Initialiser l'index j
        //                    while (j < otherPolyline.NumberOfVertices - 1)
        //                    {
        //                        Point2d otherStartPoint = otherPolyline.GetPoint2dAt(j);
        //                        Point2d otherEndPoint = otherPolyline.GetPoint2dAt(j + 1);

        //                        Polyline otherSegment = new Polyline();
        //                        otherSegment.AddVertex(otherStartPoint.ToPoint3d());
        //                        otherSegment.AddVertex(otherEndPoint.ToPoint3d());

        //                        Point3dCollection intersectionsPoints = new Point3dCollection();
        //                        segment.IntersectWith(otherSegment, Intersect.ExtendBoth, intersectionsPoints, IntPtr.Zero, IntPtr.Zero);
        //                        if (intersectionsPoints?.Count > 0)
        //                        {
        //                            foreach (Point3d intersectionPoint3d in intersectionsPoints)
        //                            {
        //                                Point2d intersectionPoint = intersectionPoint3d.ToPoint2d();
        //                                // Vérifier si l'intersection est suffisamment proche
        //                                if (startPoint.GetDistanceTo(intersectionPoint) < 0.1 ||
        //                                    endPoint.GetDistanceTo(intersectionPoint) < 0.1)
        //                                {
        //                                    // Ajouter le point à l'indice j + 1
        //                                    otherPolyline.AddVertexAt(j + 1, intersectionPoint, 0, 0, 0);
        //                                    // Mettre à jour j pour continuer à parcourir les segments
        //                                    j++;
        //                                }
        //                            }
        //                        }
        //                        // Passer au segment suivant
        //                        j++;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}


    }
}
