using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class RP2
    {
        public static void RotateUCS()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                double viewSize = (double)Application.GetSystemVariable("VIEWSIZE");
                Point3d viewCenter = (Point3d)Application.GetSystemVariable("VIEWCTR");
                ed.Command("_WORLDUCS");
                ed.Command("_PLAN", "");
                ed.Command("_ZOOM", "C", viewCenter, viewSize);
                tr.Commit();
            }
        }
    }
}
