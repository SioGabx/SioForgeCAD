using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun.Drawing
{
     public static class Polylines
    {
        public static int getVerticesMaximum(Polyline TargetPolyline)
        {
            int NumberOfVertices = (TargetPolyline.NumberOfVertices - 1);
            if (TargetPolyline.Closed)
            {
                NumberOfVertices++;
            }
            return NumberOfVertices;
        }

        public static (Point3d PolylineSegmentStart, Point3d PolylineSegmentEnd) GetSegmentPoint(Polyline TargetPolyline, int Index)
        {
            int NumberOfVertices = TargetPolyline.NumberOfVertices;
            var PolylineSegmentStart = TargetPolyline.GetPoint3dAt(Index);
            Index += 1;
            if (Index >= NumberOfVertices)
            {
                Index = 0;
            }
            var PolylineSegmentEnd = TargetPolyline.GetPoint3dAt(Index);
            return (PolylineSegmentStart, PolylineSegmentEnd);
        }


        public static ObjectId Draw(IEnumerable<Points> listOfPoints)
        {
            Polyline polyline = new Polyline();
            foreach (Points point in listOfPoints)
            {
                polyline.AddVertexAt(polyline.NumberOfVertices, point.SCG.ToPoint2d(), 0, 0, 0);
            }
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
