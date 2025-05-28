using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Commun
{
    public class Points
    {
        public static Points Empty { get; } = new Points(new Point3d(0, 0, 0));
        public const Points Null = null;
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
           var ed = Generic.GetEditor();
            return OriginalPoint.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());
        }
        public static Point3d ToSCGFromCurentSCU(Point3d OriginalPoint)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
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

        public static Points From3DPoint(Point3d Point3d)
        {
            return new Points(Point3d);
        }

        public static bool GetPoint(out Points Points, string Message, Points BasePoint = Null)
        {
            Editor ed = Generic.GetEditor();

            PromptPointOptions promptPointOptions = new PromptPointOptions(Message)
            {
                AllowNone = false,
                AllowArbitraryInput = false,
                UseDashedLine = true,
                UseBasePoint = false,
            };

            if (BasePoint != Null)
            {
                promptPointOptions.UseBasePoint = true;
                promptPointOptions.UseDashedLine = true;
                promptPointOptions.BasePoint = BasePoint.SCU;
            }

            var SelectedPoint = ed.GetPoint(promptPointOptions);
            if (SelectedPoint.Status == PromptStatus.OK)
            {
                Points = GetFromPromptPointResult(SelectedPoint);
                return true;
            }
            Points = Empty;
            return false;
        }
    }

    public static class PointsExtensions
    {
        public static Points Flatten(this Points point)
        {
            return new Points(point.SCG.Flatten());
        }
    }
}
