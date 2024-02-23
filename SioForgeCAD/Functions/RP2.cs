using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;

namespace SioForgeCAD.Functions
{
    public static class RP2
    {
        public static void RotateUCS()
        {
            Document doc = Generic.GetDocument();

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                double viewSize = (double)Application.GetSystemVariable("VIEWSIZE");
                Point3d viewCenter = (Point3d)Application.GetSystemVariable("VIEWCTR");
                Generic.Command("_WORLDUCS");
                Generic.Command("_PLAN", "");
                Generic.Command("_ZOOM", "_C", viewCenter, viewSize);
                tr.Commit();
            }
        }
    }
}
