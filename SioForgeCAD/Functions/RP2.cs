using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class RP2
    {
        public static void RotateUCS()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            if (ed.IsInLockedViewport())
            {
                Generic.WriteMessage("Cette commande n'est pas autorisée dans les espaces objets verouillés");
                return;
            }

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                //TODO : Check if viewport is locked => abord
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
