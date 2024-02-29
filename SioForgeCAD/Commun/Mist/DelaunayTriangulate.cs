using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Commun
{
    public static class Triangulate
    {
        public static bool CalculateCircumscribedCircle(double x1, double y1, double x2, double y2, double x3, double y3, ref double xc, ref double yc, ref double r)
        {
            // Calculation of circumscribed circle coordinates and
            // squared radius
            const double eps = 1e-6;
            bool result = true;
            double m1, m2, mx1, mx2, my1, my2, dx, dy;
            if ((Math.Abs(y1 - y2) < eps) && (Math.Abs(y2 - y3) < eps))
            {
                result = false;
                xc = x1; yc = y1;
            }
            else
            {
                if (Math.Abs(y2 - y1) < eps)
                {
                    m2 = -(x3 - x2) / (y3 - y2);
                    mx2 = (x2 + x3) / 2;
                    my2 = (y2 + y3) / 2;
                    xc = (x2 + x1) / 2;
                    yc = m2 * (xc - mx2) + my2;
                }
                else if (Math.Abs(y3 - y2) < eps)
                {
                    m1 = -(x2 - x1) / (y2 - y1);
                    mx1 = (x1 + x2) / 2;
                    my1 = (y1 + y2) / 2;
                    xc = (x3 + x2) / 2;
                    yc = m1 * (xc - mx1) + my1;
                }
                else
                {
                    m1 = -(x2 - x1) / (y2 - y1);
                    m2 = -(x3 - x2) / (y3 - y2);
                    if (Math.Abs(m1 - m2) < eps)
                    {
                        result = false;
                        xc = x1;
                        yc = y1;
                    }
                    else
                    {
                        mx1 = (x1 + x2) / 2;
                        mx2 = (x2 + x3) / 2;
                        my1 = (y1 + y2) / 2;
                        my2 = (y2 + y3) / 2;
                        xc = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2);
                        yc = m1 * (xc - mx1) + my1;
                    }
                }
            }
            dx = x2 - xc;
            dy = y2 - yc;
            r = dx * dx + dy * dy;
            return result;
        }

        public static void TriangulateCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            TypedValue[] PointTypedValue = { new TypedValue(0, "POINT") };
            SelectionFilter PointSelectionFilter = new SelectionFilter(PointTypedValue);
            PromptSelectionOptions SelectPointsPromptOption = new PromptSelectionOptions
            {
                MessageForAdding = "Select Points:",
                AllowDuplicates = false
            };
            PromptSelectionResult pointSelectionResult = ed.GetSelection(SelectPointsPromptOption, PointSelectionFilter);
            if (pointSelectionResult.Status == PromptStatus.Error) return;
            if (pointSelectionResult.Status == PromptStatus.Cancel) return;
            SelectionSet pointSelectionSet = pointSelectionResult.Value;
            int numberOfPoints = pointSelectionSet.Count;
            if (numberOfPoints < 3)
            {
                ed.WriteMessage("Minimum 3 points must be selected!");
                return;
            }
            int PtsIndex, j, k, numberOfTriangles, numberOfEdges, thinTriangleFoundCount = 0, IgnoredPointWithSameCoordinatesCount = 0;

            // Point coordinates
            double[] xCoordinates = new double[numberOfPoints + 3];
            double[] yCoordinates = new double[numberOfPoints + 3];
            double[] zCoordinates = new double[numberOfPoints + 3];
            // Triangle definitions
            int[] vertexPoint1 = new int[numberOfPoints * 2 + 1];
            int[] vertexPoint2 = new int[numberOfPoints * 2 + 1];
            int[] vertexPoint3 = new int[numberOfPoints * 2 + 1];
            // Circumscribed circle
            double[] centerXValues = new double[numberOfPoints * 2 + 1];
            double[] centerYValues = new double[numberOfPoints * 2 + 1];
            double[] radiusValues = new double[numberOfPoints * 2 + 1];
            double xMin, yMin, xMax, yMax, deltaX, deltaY, xMid, yMid;
            int[] edge1 = new int[numberOfPoints * 2 + 1];
            int[] edge2 = new int[numberOfPoints * 2 + 1];
            ObjectId[] idArray = pointSelectionSet.GetObjectIds();
            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                DBPoint PointElement;
                k = 0;
                for (PtsIndex = 0; PtsIndex < numberOfPoints; PtsIndex++)
                {
                    PointElement = (DBPoint)tr.GetObject(idArray[k], OpenMode.ForRead, false);
                    xCoordinates[PtsIndex] = PointElement.Position[0];
                    yCoordinates[PtsIndex] = PointElement.Position[1];
                    zCoordinates[PtsIndex] = PointElement.Position[2];
                    for (j = 0; j < PtsIndex; j++)
                    {
                        if ((xCoordinates[PtsIndex] == xCoordinates[j]) && (yCoordinates[PtsIndex] == yCoordinates[j]))
                        {
                            PtsIndex--; numberOfPoints--; IgnoredPointWithSameCoordinatesCount++;
                        }
                    }
                    k++;
                }
                tr.Commit();
            }
            if (IgnoredPointWithSameCoordinatesCount > 0)
                ed.WriteMessage(
                  "\nIgnored {0} point(s) with same coordinates.",
                  IgnoredPointWithSameCoordinatesCount
                );


            // Supertriangle
            xMin = xCoordinates[0]; xMax = xMin;
            yMin = yCoordinates[0]; yMax = yMin;
            for (PtsIndex = 0; PtsIndex < numberOfPoints; PtsIndex++)
            {
                if (xCoordinates[PtsIndex] < xMin) xMin = xCoordinates[PtsIndex];
                if (xCoordinates[PtsIndex] > xMax) xMax = xCoordinates[PtsIndex];
                if (yCoordinates[PtsIndex] < xMin) yMin = yCoordinates[PtsIndex];
                if (yCoordinates[PtsIndex] > xMin) yMax = yCoordinates[PtsIndex];
            }
            deltaX = xMax - xMin; deltaY = yMax - yMin;
            xMid = (xMin + xMax) / 2; yMid = (yMin + yMax) / 2;
            PtsIndex = numberOfPoints;
            xCoordinates[PtsIndex] = xMid - (90 * (deltaX + deltaY)) - 100;
            yCoordinates[PtsIndex] = yMid - (50 * (deltaX + deltaY)) - 100;
            zCoordinates[PtsIndex] = 0;
            vertexPoint1[0] = PtsIndex;
            PtsIndex++;
            xCoordinates[PtsIndex] = xMid + (90 * (deltaX + deltaY)) + 100;
            yCoordinates[PtsIndex] = yMid - (50 * (deltaX + deltaY)) - 100;
            zCoordinates[PtsIndex] = 0;
            vertexPoint2[0] = PtsIndex;
            PtsIndex++;
            xCoordinates[PtsIndex] = xMid;
            yCoordinates[PtsIndex] = yMid + 100 * (deltaX + deltaY + 1);
            zCoordinates[PtsIndex] = 0;
            vertexPoint3[0] = PtsIndex;
            numberOfTriangles = 1;
            CalculateCircumscribedCircle(
              xCoordinates[vertexPoint1[0]], yCoordinates[vertexPoint1[0]], xCoordinates[vertexPoint2[0]],
              yCoordinates[vertexPoint2[0]], xCoordinates[vertexPoint3[0]], yCoordinates[vertexPoint3[0]],
              ref centerXValues[0], ref centerYValues[0], ref radiusValues[0]
            );

            // main loop
            for (PtsIndex = 0; PtsIndex < numberOfPoints; PtsIndex++)
            {
                numberOfEdges = 0;
                xMin = xCoordinates[PtsIndex]; yMin = yCoordinates[PtsIndex];
                j = 0;
                while (j < numberOfTriangles)
                {
                    deltaX = centerXValues[j] - xMin; deltaY = centerYValues[j] - yMin;
                    if (((deltaX * deltaX) + (deltaY * deltaY)) < radiusValues[j])
                    {
                        edge1[numberOfEdges] = vertexPoint1[j]; edge2[numberOfEdges] = vertexPoint2[j];
                        numberOfEdges++;
                        edge1[numberOfEdges] = vertexPoint2[j]; edge2[numberOfEdges] = vertexPoint3[j];
                        numberOfEdges++;
                        edge1[numberOfEdges] = vertexPoint3[j]; edge2[numberOfEdges] = vertexPoint1[j];
                        numberOfEdges++;
                        numberOfTriangles--;
                        vertexPoint1[j] = vertexPoint1[numberOfTriangles];
                        vertexPoint2[j] = vertexPoint2[numberOfTriangles];
                        vertexPoint3[j] = vertexPoint3[numberOfTriangles];
                        centerXValues[j] = centerXValues[numberOfTriangles];
                        centerYValues[j] = centerYValues[numberOfTriangles];
                        radiusValues[j] = radiusValues[numberOfTriangles];
                        j--;
                    }
                    j++;
                }

                for (j = 0; j < numberOfEdges - 1; j++)
                {
                    for (k = j + 1; k < numberOfEdges; k++)
                    {
                        if ((edge1[j] == edge2[k]) && (edge2[j] == edge1[k]))
                        {
                            edge1[j] = -1; edge2[j] = -1; edge1[k] = -1; edge2[k] = -1;
                        }
                    }
                }
                for (j = 0; j < numberOfEdges; j++)
                {
                    if ((edge1[j] >= 0) && (edge2[j] >= 0))
                    {
                        vertexPoint1[numberOfTriangles] = edge1[j]; vertexPoint2[numberOfTriangles] = edge2[j]; vertexPoint3[numberOfTriangles] = PtsIndex;
                        bool IsThinTriangle =
                          CalculateCircumscribedCircle(
                            xCoordinates[vertexPoint1[numberOfTriangles]], yCoordinates[vertexPoint1[numberOfTriangles]], xCoordinates[vertexPoint2[numberOfTriangles]],
                            yCoordinates[vertexPoint2[numberOfTriangles]], xCoordinates[vertexPoint3[numberOfTriangles]], yCoordinates[vertexPoint3[numberOfTriangles]],
                            ref centerXValues[numberOfTriangles], ref centerYValues[numberOfTriangles], ref radiusValues[numberOfTriangles]
                          );
                        if (!IsThinTriangle)
                        {
                            thinTriangleFoundCount++;
                        }
                        numberOfTriangles++;
                    }
                }
            }

            //removal of outer triangles
            PtsIndex = 0;
            while (PtsIndex < numberOfTriangles)
            {
                if ((vertexPoint1[PtsIndex] >= numberOfPoints) || (vertexPoint2[PtsIndex] >= numberOfPoints) || (vertexPoint3[PtsIndex] >= numberOfPoints))
                {
                    numberOfTriangles--;
                    vertexPoint1[PtsIndex] = vertexPoint1[numberOfTriangles];
                    vertexPoint2[PtsIndex] = vertexPoint2[numberOfTriangles];
                    vertexPoint3[PtsIndex] = vertexPoint3[numberOfTriangles];
                    centerXValues[PtsIndex] = centerXValues[numberOfTriangles];
                    centerYValues[PtsIndex] = centerYValues[numberOfTriangles];
                    radiusValues[PtsIndex] = radiusValues[numberOfTriangles];
                    PtsIndex--;
                }
                PtsIndex++;
            }
            tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt =
                  (BlockTable)tr.GetObject(
                    db.BlockTableId,
                    OpenMode.ForRead,
                    false
                  );
                BlockTableRecord btr =
                  (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite,
                    false
                  );
                //PolyFaceMesh pfm = new PolyFaceMesh();
                //btr.AppendEntity(pfm);
                //tr.AddNewlyCreatedDBObject(pfm, true);
                //for (i = 0; i < npts; i++)
                //{
                //    PolyFaceMeshVertex vert =
                //      new PolyFaceMeshVertex(
                //        new Point3d(ptx[i], pty[i], ptz[i])
                //      );
                //    pfm.AppendVertex(vert);
                //    tr.AddNewlyCreatedDBObject(vert, true);
                //}
                //for (i = 0; i < ntri; i++)
                //{
                //    FaceRecord face =
                //      new FaceRecord(
                //        (short)(pt1[i] + 1),
                //        (short)(pt2[i] + 1),
                //        (short)(pt3[i] + 1),
                //        0
                //      );
                //    pfm.AppendFaceRecord(face);
                //    tr.AddNewlyCreatedDBObject(face, true);
                //}
                for (PtsIndex = 0; PtsIndex < numberOfTriangles; PtsIndex++)
                {
                    Point3d vertex1 = new Point3d(xCoordinates[vertexPoint1[PtsIndex]], yCoordinates[vertexPoint1[PtsIndex]], zCoordinates[vertexPoint1[PtsIndex]]);
                    Point3d vertex2 = new Point3d(xCoordinates[vertexPoint2[PtsIndex]], yCoordinates[vertexPoint2[PtsIndex]], zCoordinates[vertexPoint2[PtsIndex]]);
                    Point3d vertex3 = new Point3d(xCoordinates[vertexPoint3[PtsIndex]], yCoordinates[vertexPoint3[PtsIndex]], zCoordinates[vertexPoint3[PtsIndex]]);
                    using (Polyline3d poly = new Polyline3d())
                    {
                        poly.AddToDrawingCurrentTransaction();
                        poly.AddVertex(vertex1);
                        poly.AddVertex(vertex2);
                        poly.AddVertex(vertex3);
                        poly.Closed = true;
                    }

                    //Line line1 = new Line(vertex1, vertex2);
                    //Line line2 = new Line(vertex2, vertex3);
                    //Line line3 = new Line(vertex3, vertex1);

                    //btr.AppendEntity(line1);
                    //btr.AppendEntity(line2);
                    //btr.AppendEntity(line3);

                    //tr.AddNewlyCreatedDBObject(line1, true);
                    //tr.AddNewlyCreatedDBObject(line2, true);
                    //tr.AddNewlyCreatedDBObject(line3, true);
                }

                tr.Commit();
            }
            if (thinTriangleFoundCount > 0)
                ed.WriteMessage(
                  "\nWarning! {0} thin triangle(s) found!" +
                  " Wrong result possible!",
                  thinTriangleFoundCount
                );
            Application.UpdateScreen();
        }
    }
}
