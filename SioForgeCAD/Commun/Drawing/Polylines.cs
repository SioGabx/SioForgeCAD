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
