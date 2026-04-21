using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class ADDPOINTSATPOLYLIGNEVERTICES
    {
        public static void Execute()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            var selRes = ed.GetSelectionRedraw("\nSélectionnez des courbes :", true, false, ed.GetCurvesFilter());

            if (selRes.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    int totalPoints = 0;

                    foreach (ObjectId id in selRes.Value.GetObjectIds())
                    {
                        // Cast to the base Curve class
                        Curve ent = tr.GetObject(id, OpenMode.ForRead) as Curve;
                        if (ent == null) continue;

                        List<Point3d> pointsToCreate = new List<Point3d>();

                        // 2. Determine points based on specific curve type
                        if (ent is Polyline pline)
                        {
                            for (int i = 0; i < pline.NumberOfVertices; i++)
                                pointsToCreate.Add(pline.GetPoint3dAt(i));
                        }
                        else if (ent is Polyline3d pline3d)
                        {
                            // For old-style 3D polylines
                            foreach (ObjectId vId in pline3d)
                            {
                                var v3d = (PolylineVertex3d)tr.GetObject(vId, OpenMode.ForRead);
                                pointsToCreate.Add(v3d.Position);
                            }
                        }
                        else if (ent is Spline spline)
                        {
                            // For Splines, we usually want Control Points
                            for (int i = 0; i < spline.NumControlPoints; i++)
                                pointsToCreate.Add(spline.GetControlPointAt(i));
                        }
                        else
                        {
                            // For Line, Arc, Ellipse: Use Start and End points
                            pointsToCreate.Add(ent.StartPoint);
                            // Avoid adding the same point twice if it's a closed curve (like a Circle)
                            if (!ent.Closed)
                            {
                                pointsToCreate.Add(ent.EndPoint);
                            }
                        }

                        // 3. Create the points in the drawing
                        foreach (Point3d pt in pointsToCreate)
                        {
                            DBPoint dbPoint = new DBPoint(pt);
                            dbPoint.AddToDrawing();
                            totalPoints++;
                        }
                    }

                    tr.Commit();
                    Generic.WriteMessage($"\nSuccès : {totalPoints} points créés.");
                }
                catch (System.Exception ex)
                {
                    Generic.WriteMessage("\nErreur : " + ex.Message);
                    tr.Abort();
                }
            }
        }
    }
}