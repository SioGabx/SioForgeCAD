using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Polylines
    {
        public static Polyline GetPolylineFromPoints(IEnumerable<Points> listOfPoints)
        {
            Polyline polyline = new Polyline();
            foreach (Points point in listOfPoints)
            {
                polyline.AddVertexAt(polyline.NumberOfVertices, point.SCG.ToPoint2d(), 0, 0, 0);
            }
            return polyline;
        }

        public static Polyline GetPolylineFromPoints(Point3dCollection listOfPoints)
        {
            Polyline polyline = new Polyline();
            foreach (Point3d point in listOfPoints)
            {
                polyline.AddVertexAt(polyline.NumberOfVertices, point.ToPoint2d(), 0, 0, 0);
            }
            return polyline;
        }

        public static Polyline GetPolylineFromPoints(params Points[] listOfPoints)
        {
            return GetPolylineFromPoints(listOfPoints as IEnumerable<Points>);
        }

        public enum PolylineSide
        {
            Right,
            Left,
            Collinear
        }


        public static PolylineSide CheckPointSide(this Polyline BasePolyline, Point3d TargetPoint)
        {
            for (int segmentIndex = 0; segmentIndex < BasePolyline.NumberOfVertices - 1; segmentIndex++)
            {
                Point3d startPoint = BasePolyline.GetPoint3dAt(segmentIndex);
                Point3d endPoint = BasePolyline.GetPoint3dAt(segmentIndex + 1);

                Vector2d polylineVector = new Vector2d(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
                Vector2d pointVector = new Vector2d(TargetPoint.X - startPoint.X, TargetPoint.Y - startPoint.Y);

                //cross product
                double crossProduct = polylineVector.X * pointVector.Y - polylineVector.Y * pointVector.X;

                if (crossProduct < 0)
                {
                    //left
                    return PolylineSide.Left;
                }
                else if (crossProduct > 0)
                {
                    // Right
                    return PolylineSide.Right;
                }
            }
            //collinear
            return PolylineSide.Collinear;
        }


        public static bool IsAtLeftSide(this Polyline BasePolyline, Point3d TargetPoint)
        {
            return CheckPointSide(BasePolyline, TargetPoint) == PolylineSide.Left;
        }
        public static bool IsAtRightSide(this Polyline BasePolyline, Point3d TargetPoint)
        {
            return CheckPointSide(BasePolyline, TargetPoint) == PolylineSide.Right;
        }


        public static ObjectId Draw(IEnumerable<Points> listOfPoints)
        {
            using (Polyline polyline = GetPolylineFromPoints(listOfPoints))
            {
                polyline.Cleanup();
                if (polyline.Length > 0)
                {
                    return polyline.AddToDrawing();
                }
                else
                {
                    return ObjectId.Null;
                }
            }
        }

    }
}
