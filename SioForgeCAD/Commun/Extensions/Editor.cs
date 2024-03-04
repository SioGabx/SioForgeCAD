using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;


namespace SioForgeCAD.Commun.Extensions
{
    public enum AngleUnit { Radians, Degrees }
    public static class EditorExtensions
    {
        public static double GetUSCRotation(this Editor ed, AngleUnit angleUnit)
        {
            Matrix3d ucsCur = ed.CurrentUserCoordinateSystem;
            CoordinateSystem3d cs = ucsCur.CoordinateSystem3d;
            double ucs_rotAngle = cs.Xaxis.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
            if (angleUnit == AngleUnit.Radians)
            {
                return ucs_rotAngle;
            }
            double ucs_angle_degres = ucs_rotAngle * 180 / Math.PI;
            return ucs_angle_degres;
        }

        public static Size GetCurrentViewSize(this Editor _)
        {
            //https://drive-cad-with-code.blogspot.com/2013/04/how-to-get-current-view-size.html
            //Get current view height
            double h = (double)Application.GetSystemVariable("VIEWSIZE");
            //Get current view width,
            //by calculate current view's width-height ratio
            Point2d screen = (Point2d)Application.GetSystemVariable("SCREENSIZE");
            double w = h * (screen.X / screen.Y);
            return new Size(w, h);
        }


        public static Polyline GetPolyline(this Editor ed, string Message, bool RejectObjectsOnLockedLayers = true)
        {
            Database db = ed.Document.Database;
            while (true)
            {
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    MessageForAdding = Message,
                    SingleOnly = true,
                    SinglePickInSpace = true,
                    RejectObjectsOnLockedLayers = RejectObjectsOnLockedLayers,
                };
                TypedValue[] filterList = new TypedValue[] {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                    new TypedValue((int)DxfCode.Start, "LINE"),
                    new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                    new TypedValue((int)DxfCode.Start, "SPLINE"),
                    new TypedValue((int)DxfCode.Operator, "or>"),
                };
                PromptSelectionResult polyResult = ed.GetSelection(promptSelectionOptions, new SelectionFilter(filterList));
                if (polyResult.Status == PromptStatus.Error)
                {
                    continue;
                }
                if (polyResult.Status != PromptStatus.OK)
                {
                    return null;
                }
                Entity SelectedEntity = polyResult.Value[0].ObjectId.GetNoTransactionDBObject(OpenMode.ForRead) as Entity;
                if (SelectedEntity is Line ProjectionTargetLine)
                {
                    SelectedEntity = ProjectionTargetLine.ToPolyline();
                }

                if (SelectedEntity is Ellipse ProjectionTargetEllipse)
                {
                    SelectedEntity = ProjectionTargetEllipse.ToPolyline();
                }

                if (SelectedEntity is Spline ProjectionTargetSpline)
                {
                    SelectedEntity = ProjectionTargetSpline.ToPolyline();
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
