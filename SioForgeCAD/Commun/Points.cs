using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class Points
    {
        public static Points Empty { get; set; } = new Points(new Point3d(0, 0, 0));
        public Point3d SCG { get; }
        public Point3d SCU { get; }

        public Points(Point3d SCG)
        {
            this.SCG = SCG;
            this.SCU = ToCurentSCU(SCG);
        }

        public static Point3d ToCurentSCU(Point3d OriginalPoint)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            Point3d ConvertedPoint = OriginalPoint.TransformBy(ed.CurrentUserCoordinateSystem);
            return ConvertedPoint;
        }

        public static Points GetFromPromptPointResult(PromptPointResult PromptPointResult)
        {
            Points PromptedPoint = new Points(PromptPointResult.Value);
            return PromptedPoint;
        }



    }
}
