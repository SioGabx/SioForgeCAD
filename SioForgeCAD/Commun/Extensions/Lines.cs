using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace SioForgeCAD.Commun.Extensions
{
    public static class LinesExtentions
    {
        public static void Cleanup(this Polyline polyline)
        {
            if (polyline != null)
            {
                int vertexCount = polyline.NumberOfVertices;

                if (vertexCount > 2)
                {
                    Point2d lastPoint = polyline.GetPoint2dAt(0);
                    int index = 1;
                    while ((polyline.NumberOfVertices - 1) > index)
                    {
                        Point2d currentPoint = polyline.GetPoint2dAt(index);
                        Point2d nextPoint = polyline.GetPoint2dAt(index + 1);
                        Vector2d vector1 = currentPoint.GetVectorTo(lastPoint);
                        Vector2d vector2 = nextPoint.GetVectorTo(currentPoint);

                        // Calculer la normal du vecteur en utilisant le produit vectoriel
                        double crossProduct = vector1.X * vector2.Y - vector1.Y * vector2.X;

                        if (Math.Abs(crossProduct) < Tolerance.Global.EqualPoint || currentPoint == nextPoint)
                        {
                            polyline.RemoveVertexAt(index);
                            // Décrémenter l'index pour réexaminer le point actuel lors de la prochaine itération
                            index--;
                        }
                        lastPoint = currentPoint;
                        index++;
                    }


                    if (polyline.Closed == false && polyline.GetPoint3dAt(0).IsEqualTo(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1)))
                    {
                        polyline.RemoveVertexAt(polyline.NumberOfVertices - 1);
                        polyline.Closed = true;
                    }

                }
            }
        }



        public static IEnumerable<Point2d> GetPolyPoints(this Polyline poly)
        {
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                yield return poly.GetPoint2dAt(i);
            }
        }

               
        
        /// <summary>
        /// Determines if the polyline is self-intersecting.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <returns>The result.</returns>
        public static bool IsSelfIntersecting(this Polyline poly, out Point3dCollection IntersectionFound)
        {
            IntersectionFound = new Point3dCollection();
            DBObjectCollection entities = new DBObjectCollection();
            poly.Explode(entities);
            for (int i = 0; i < entities.Count; ++i)
            {
                for (int j = i + 1; j < entities.Count; ++j)
                {
                    Curve curve1 = entities[i] as Curve;
                    Curve curve2 = entities[j] as Curve;
                    Point3dCollection points = new Point3dCollection();
                    curve1.IntersectWith(curve2,Intersect.OnBothOperands,points,IntPtr.Zero,IntPtr.Zero);

                    foreach (Point3d point in points)
                    {
                        // Make a check to skip the start/end points
                        // since they are connected vertices
                        if (point == curve1.StartPoint || point == curve1.EndPoint)
                        {
                            if (point == curve2.StartPoint || point == curve2.EndPoint)
                            {
                                continue;
                            }
                        }

                        // If two consecutive segments, then skip
                        if (j == i + 1)
                        {
                            continue;
                        }
                        IntersectionFound.Add(point);
                    }

                }
                // Need to be disposed explicitely
                // since entities are not DB resident
                entities[i].Dispose();
            }

            if (IntersectionFound.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        /// <summary>
        /// Determines if two line segments intersect.
        /// </summary>
        /// <param name="a1">Line a point 1.</param>
        /// <param name="a2">Line a point 2.</param>
        /// <param name="b1">Line b point 1.</param>
        /// <param name="b2">Line b point 2.</param>
        /// <returns>The result.</returns>
        public static bool IsLineSegIntersect(Point2d a1, Point2d a2, Point2d b1, Point2d b2)
        {
            if ((a1 - a2).CrossProduct(b1 - b2) == 0)
            {
                return false;
            }

            double lambda;
            double miu;

            if (b1.X == b2.X)
            {
                lambda = (b1.X - a1.X) / (a2.X - b1.X);
                double Y = (a1.Y + lambda * a2.Y) / (1 + lambda);
                miu = (Y - b1.Y) / (b2.Y - Y);
            }
            else if (a1.X == a2.X)
            {
                miu = (a1.X - b1.X) / (b2.X - a1.X);
                double Y = (b1.Y + miu * b2.Y) / (1 + miu);
                lambda = (Y - a1.Y) / (a2.Y - Y);
            }
            else if (b1.Y == b2.Y)
            {
                lambda = (b1.Y - a1.Y) / (a2.Y - b1.Y);
                double X = (a1.X + lambda * a2.X) / (1 + lambda);
                miu = (X - b1.X) / (b2.X - X);
            }
            else if (a1.Y == a2.Y)
            {
                miu = (a1.Y - b1.Y) / (b2.Y - a1.Y);
                double X = (b1.X + miu * b2.X) / (1 + miu);
                lambda = (X - a1.X) / (a2.X - X);
            }
            else
            {
                lambda = (b1.X * a1.Y - b2.X * a1.Y - a1.X * b1.Y + b2.X * b1.Y + a1.X * b2.Y - b1.X * b2.Y) / (-b1.X * a2.Y + b2.X * a2.Y + a2.X * b1.Y - b2.X * b1.Y - a2.X * b2.Y + b1.X * b2.Y);
                miu = (-a2.X * a1.Y + b1.X * a1.Y + a1.X * a2.Y - b1.X * a2.Y - a1.X * b1.Y + a2.X * b1.Y) / (a2.X * a1.Y - b2.X * a1.Y - a1.X * a2.Y + b2.X * a2.Y + a1.X * b2.Y - a2.X * b2.Y); // from Mathematica
            }

            bool result = false;
            if (lambda >= 0 || double.IsInfinity(lambda))
            {
                if (miu >= 0 || double.IsInfinity(miu))
                {
                    result = true;
                }
            }
            return result;
        }



        /// <summary>
        /// Gets the bulge between two parameters within the same arc segment of a polyline.
        /// </summary>
        /// <param name="poly">The polyline.</param>
        /// <param name="startParam">The start parameter.</param>
        /// <param name="endParam">The end parameter.</param>
        /// <returns>The bulge.</returns>
        public static double GetBulgeBetween(this Polyline poly, double startParam, double endParam)
        {
            double total = poly.GetBulgeAt((int)Math.Floor(startParam));
            return (endParam - startParam) * total;
        }

        public static void AddVertex(this Polyline Poly, Point3d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            Poly.AddVertexAt(Poly.NumberOfVertices, point.ToPoint2d(), bulge, startWidth, endWidth);
        }

        public static void AddVertex(this Polyline3d Poly, Point3d point)
        {
            var Vertex = new PolylineVertex3d(point);
            Poly.AppendVertex(Vertex);
        }

        public static bool HasAtLeastOnPointInCommun(this Polyline PolyA, Polyline PolyB)
        {
            return (
                PolyA.StartPoint.IsEqualTo(PolyB.StartPoint) ||
                PolyA.StartPoint.IsEqualTo(PolyB.EndPoint) ||
                PolyA.EndPoint.IsEqualTo(PolyB.StartPoint) ||
                PolyA.EndPoint.IsEqualTo(PolyB.EndPoint));
        }

        /*
         if ((cutedPolyligne.StartPoint.IsEqualTo(PolySegment.StartPoint) && cutedPolyligne.EndPoint.IsEqualTo(PolySegment.EndPoint)) ||
                        (cutedPolyligne.EndPoint.IsEqualTo(PolySegment.StartPoint) && cutedPolyligne.StartPoint.IsEqualTo(PolySegment.EndPoint)))
        */
        public static bool IsLineCanCloseAPolyline(this Polyline PolyA, Polyline PolyB)
        {
            Point3d StartPointA = PolyA.StartPoint.Flatten();
            Point3d EndPointA = PolyA.EndPoint.Flatten();

            Point3d StartPointB = PolyB.StartPoint.Flatten();
            Point3d EndPointB = PolyB.EndPoint.Flatten();

            return (StartPointA.IsEqualTo(StartPointB) && EndPointA.IsEqualTo(EndPointB)) ||
                 (StartPointA.IsEqualTo(EndPointB) && EndPointA.IsEqualTo(StartPointB))
                ;
        }


        public static void AddVertexIfNotExist(this Polyline Poly, Point3d point, double bulge = 0, double startWidth = 0, double endWidth = 0)
        {
            for (int i = 0; i < Poly.NumberOfVertices; i++)
            {
                if (Poly.GetPoint3dAt(i) == point)
                {
                    return;
                }
            }
            AddVertex(Poly, point, bulge, startWidth, endWidth);
        }
        public static bool IsClockwise(this Polyline poly)
        {
            double sum = 0;
            for (var i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var cur = poly.GetPoint2dAt(i);
                var next = poly.GetPoint2dAt(i + 1);
                sum += (next.X - cur.X) * (next.Y + cur.Y);
            }
            return sum > 0;
        }




        /// <summary>
        /// Converts line to polyline.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>A polyline.</returns>
        public static Polyline ToPolyline(this Line line)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, line.StartPoint.ToPoint2d(), 0, 0, 0);
            poly.AddVertexAt(1, line.EndPoint.ToPoint2d(), 0, 0, 0);
            return poly;
        }

        /// <summary>
        /// Converts arc to polyline.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <returns>A polyline.</returns>
        public static Polyline ToPolyline(this Arc arc)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, arc.StartPoint.ToPoint2d(), arc.GetArcBulge(arc.StartPoint), 0, 0);
            poly.AddVertexAt(1, arc.EndPoint.ToPoint2d(), 0, 0, 0);
            return poly;
        }

        /// <summary>
        /// Connects polylines.
        /// </summary>
        /// <param name="poly">The base polyline.</param>
        /// <param name="poly1">The other polyline.</param>
        public static void JoinPolyline(this Polyline poly, Polyline poly1)
        {
            int index = poly.GetPolyPoints().Count();
            int index1 = 0;
            var Points = poly1.GetPoints();
            if (!poly.IsWriteEnabled)
            {
                poly.UpgradeOpen();
            }
            foreach (var point in Points)
            {
                poly.AddVertexAt(index, point.ToPoint2d(), poly1.GetBulgeAt(index1), 0, 0);
                index++;
                index1++;
            }
        }

        public static Polyline AskForSelection(string Message)
        {
            while (true)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    MessageForAdding = Message,
                    SingleOnly = true,
                };
                PromptSelectionResult polyResult = ed.GetSelection(promptSelectionOptions);
                if (polyResult.Status == PromptStatus.Error)
                {
                    continue;
                }
                if (polyResult.Status != PromptStatus.OK)
                {
                    return null;
                }
                Entity SelectedEntity;

                using (Transaction GlobalTrans = db.TransactionManager.StartTransaction())
                {
                    SelectedEntity = polyResult.Value[0].ObjectId.GetEntity();
                }
                if (SelectedEntity is Line ProjectionTargetLine)
                {
                    SelectedEntity = ProjectionTargetLine.ToPolyline();
                }
                if (!(SelectedEntity is Polyline ProjectionTarget))
                {
                    Generic.WriteMessage("L'objet sélectionné n'est pas une polyligne. \n");
                    continue;
                }
                return ProjectionTarget;
            }
        }

    }
}
