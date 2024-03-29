﻿using Autodesk.AutoCAD.DatabaseServices;
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

        public static Extents3d GetCurrentViewBound(this Editor ed, double shrinkScale = 1.0)
        {

            //Get current view size
            Size vSize = GetCurrentViewSize(ed);

            double w = vSize.Width * shrinkScale;
            double h = vSize.Height * shrinkScale;

            //Get current view's centre.
            //Note, the centre point from VIEWCTR is in UCS and
            //need to be transformed back to World CS
            Point3d cent = ((Point3d)Application.GetSystemVariable("VIEWCTR")).TransformBy(ed.CurrentUserCoordinateSystem);

            Point3d minPoint = new Point3d(cent.X - w / 2.0, cent.Y - h / 2.0, 0);
            Point3d maxPoint = new Point3d(cent.X + w / 2.0, cent.Y + h / 2.0, 0);

            return new Extents3d(minPoint, maxPoint);
        }


        public static bool GetBlocks(this Editor ed, out ObjectId[] objectId, bool SingleOnly = true, bool RejectObjectsOnLockedLayers = true)
        {
            objectId = new ObjectId[0];
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
                SingleOnly = SingleOnly,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = RejectObjectsOnLockedLayers
            };

            PromptSelectionResult promptResult;

            while (true)
            {
                promptResult = ed.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return false;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count > 0)
                    {
                        objectId = promptResult.Value.GetObjectIds();
                        return true;
                    }
                }
            }
        }

        public static Polyline GetPolyline(this Editor ed, string Message, bool RejectObjectsOnLockedLayers = true)
        {
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
                    new TypedValue((int)DxfCode.Start, "CIRCLE"),
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

                if (SelectedEntity is Circle ProjectionTargetCircle)
                {
                    SelectedEntity = ProjectionTargetCircle.ToPolyline();
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

        public static bool GetHatch(this Editor ed, out ObjectId HatchObjectId)
        {
            HatchObjectId = ObjectId.Null;
            SelectionSet BaseSelection = ed.SelectImplied()?.Value;
            if (BaseSelection != null && BaseSelection.Count > 0)
            {
                foreach (ObjectId item in BaseSelection.GetObjectIds())
                {
                    DBObject Obj = item.GetDBObject();
                    if (Obj is Hatch)
                    {
                        HatchObjectId = item;
                        break;
                    }
                }
            }

            if (HatchObjectId == ObjectId.Null)
            {
                var option = new PromptEntityOptions("\nSelectionnez une hachure")
                {
                    AllowNone = true,
                    AllowObjectOnLockedLayer = false,
                };
                option.SetRejectMessage("\nVeuillez selectionner seulement des hachures");
                option.AddAllowedClass(typeof(Hatch), false);
                var Result = ed.GetEntity(option);
                if (Result.Status == PromptStatus.None)
                {
                    return true;
                }
                else if (Result.Status != PromptStatus.OK)
                {
                    return false;
                }
                HatchObjectId = Result.ObjectId;
            }
            return true;
        }

    }
}
