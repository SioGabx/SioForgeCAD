using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class Points
    {
        public static Points Empty { get; set; } = new Points(new Point3d(0, 0, 0));
        public Point3d SCG { get; }
        public Point3d SCU
        {
            get
            {
                return ToCurrentSCU(SCG);
            }
        }

        public Points(Point3d SCG)
        {
            this.SCG = SCG;
        }

        public static Point3d ToCurrentSCU(Point3d OriginalPoint)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            Point3d ConvertedPoint = OriginalPoint.TransformBy(ed.CurrentUserCoordinateSystem);
            return ConvertedPoint;
        }
        public static Point3d ToSCGFromCurentSCU(Point3d OriginalPoint)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            Point3d ConvertedPoint = OriginalPoint.TransformBy(ed.CurrentUserCoordinateSystem);
            return ConvertedPoint;
        }

        public static Points GetFromPromptPointResult(PromptPointResult PromptPointResult)
        {
            //ed.GetPoints return a SCU location, we need to convert that to SCG
            Points PromptedPoint = new Points(ToSCGFromCurentSCU(PromptPointResult.Value));
            return PromptedPoint;
        }

        public static Points From3DPoint(double x, double y, double z)
        {
            return new Points(new Point3d(x, y, z));
        }
        public static Points From3DPoint(Point3d Point3d)
        {
            return new Points(Point3d);
        }

    }
}
