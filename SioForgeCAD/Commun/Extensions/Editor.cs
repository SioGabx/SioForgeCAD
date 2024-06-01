using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
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

            Point3d minPoint = new Point3d(cent.X - (w / 2.0), cent.Y - (h / 2.0), 0);
            Point3d maxPoint = new Point3d(cent.X + (w / 2.0), cent.Y + (h / 2.0), 0);

            return new Extents3d(minPoint, maxPoint);
        }

        public static List<Layout> GetAllLayout(this Editor _)
        {
            Database db = Generic.GetDatabase();
            List<Layout> AllLayout = new List<Layout>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId btrId in db.BlockTableId.GetDBObject(OpenMode.ForRead) as BlockTable)
                {
                    BlockTableRecord btr = btrId.GetDBObject(OpenMode.ForRead) as BlockTableRecord;

                    if (btr.IsLayout && btr.Name != BlockTableRecord.ModelSpace)
                    {
                        Layout layout = tr.GetObject(btr.LayoutId, OpenMode.ForRead) as Layout;
                        if (!layout.ModelType)
                        {
                            AllLayout.Add(layout);
                        }
                    }
                }
                tr.Commit();
                return AllLayout;
            }
        }

        public static Viewport GetViewport(this Editor ed)
        {
            Database db = ed.Document.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    while (true)
                    {
                        if (ed.IsInLayoutViewport())
                        {
                            return (Viewport)tr.GetObject(ed.CurrentViewportObjectId, OpenMode.ForWrite);
                        }
                        if (ed.IsInModel())
                        {
                            ed.SwitchToPaperSpace();
                        }

                        // Get the BlockTableRecord for the current layout
                        BlockTableRecord btr = tr.GetObject(Generic.GetDatabase().CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                        var Viewports = ed.GetAllViewportsInPaperSpace(btr);
                        if (Viewports.Count == 1)
                        {
                            ed.SwitchToModelSpace();
                            return (Viewport)tr.GetObject(Viewports.First(), OpenMode.ForWrite);
                        }


                        ed.SwitchToModelSpace();
                        PromptPointOptions promptPointOptions = new PromptPointOptions("Activez la fenêtre CIBLE et appuyez sur ENTREE pour continuer.")
                        {
                            AllowNone = true,
                            AllowArbitraryInput = true
                        };
                        var Validate = ed.GetPoint(promptPointOptions);
                        if (!Validate.Status.HasFlag(PromptStatus.OK) && !Validate.Status.HasFlag(PromptStatus.None)) { ed.SwitchToPaperSpace(); return null; }
                    }
                }
                finally
                {
                    tr.Commit();
                }

            }
        }


        public static bool GetBlocks(this Editor ed, out ObjectId[] objectId, bool SingleOnly = true, bool RejectObjectsOnLockedLayers = true)
        {
            objectId = Array.Empty<ObjectId>();
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



        public static PromptSelectionResult GetCurves(this Editor ed, string Message, bool SingleOnly = true, bool RejectObjectsOnLockedLayers = true)
        {
            PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
            {
                MessageForAdding = Message,
                SingleOnly = SingleOnly,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = RejectObjectsOnLockedLayers,
            };
            return ed.GetCurves(promptSelectionOptions);
        }

        public static PromptSelectionResult GetCurves(this Editor ed, PromptSelectionOptions promptSelectionOptions)
        {
            TypedValue[] filterList = new TypedValue[] {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), //Regular
                    new TypedValue((int)DxfCode.Start, "POLYLINE"), // 2D + 3D
                    new TypedValue((int)DxfCode.Start, "LINE"),
                    new TypedValue((int)DxfCode.Start, "ARC"),
                    new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                    new TypedValue((int)DxfCode.Start, "CIRCLE"),
                    new TypedValue((int)DxfCode.Start, "SPLINE"),
                    new TypedValue((int)DxfCode.Operator, "or>"),
                };
            return ed.GetSelection(promptSelectionOptions, new SelectionFilter(filterList));
        }

        public static bool GetImpliedSelection(this Editor ed, out PromptSelectionResult SelectionResult)
        {
            //Try to get the selection if PickFirst / implied selection is defined if true, we clear it
            SelectionResult = ed.SelectImplied();
            ed.SetImpliedSelection(Array.Empty<ObjectId>());
            return SelectionResult.Status == PromptStatus.OK;
        }

        public static void AddToImpliedSelection(this Editor ed, ObjectId objectId)
        {
            if (ed.GetImpliedSelection(out var CurrentSelectionResult) && CurrentSelectionResult.Status == PromptStatus.OK)
            {
                var CurrentSelectionSet = CurrentSelectionResult.Value.GetObjectIds().ToArray();
                _ = CurrentSelectionSet.Append(objectId);
                ed.SetImpliedSelection(CurrentSelectionSet.ToArray());
            }
            else
            {
                ed.SetImpliedSelection(new ObjectId[1] { objectId });
            }
        }

        public static Polyline GetPolyline(this Editor ed, string Message, bool RejectObjectsOnLockedLayers = true, bool Clone = true, bool AllowOtherCurveType = true)
        {
            return ed.GetPolyline(out _, Message, RejectObjectsOnLockedLayers, Clone, AllowOtherCurveType);
        }

        public static PromptSelectionResult GetSelectionRedraw(this Editor ed, string Message = null, bool RejectObjectsOnLockedLayers = true, bool SingleOnly = false, SelectionFilter selectionFilter = null)
        {
            if (Message is null)
            {
                Message = SingleOnly ? "Veuillez selectionner des entités" : "Veuillez selectionner une entité";
            }

            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = Message,
                SingleOnly = SingleOnly,
                SinglePickInSpace = SingleOnly,

                RejectObjectsOnLockedLayers = RejectObjectsOnLockedLayers
            };

            return ed.GetSelectionRedraw(selectionOptions, selectionFilter);
        }

        public static PromptSelectionResult GetSelectionRedraw(this Editor ed, PromptSelectionOptions selectionOptions, SelectionFilter selectionFilter = null)
        {
            PromptSelectionResult selectResult;
            for (int index = 0; true; index++)
            {
                if (index == 0)
                {
                    if (!ed.GetImpliedSelection(out selectResult))
                    {
                        selectResult = ed.GetSelection(selectionOptions, selectionFilter);
                    }
                }
                else
                {
                    selectResult = ed.GetSelection(selectionOptions, selectionFilter);
                }

                if (selectResult.Status == PromptStatus.Cancel)
                {
                    return selectResult;
                }
                else if (selectResult.Status == PromptStatus.OK)
                {
                    return selectResult;
                }
                else
                {
                    Generic.WriteMessage("Sélection invalide.");
                }
            }
        }

        public static Polyline GetPolyline(this Editor ed, out ObjectId EntObjectId, string Message, bool RejectObjectsOnLockedLayers = true, bool Clone = true, bool AllowOtherCurveType = true)
        {
            EntObjectId = ObjectId.Null;
            for (int index = 0; true; index++)
            {
                PromptSelectionResult polyResult;
                if (index == 0)
                {
                    if (!ed.GetImpliedSelection(out polyResult))
                    {
                        polyResult = ed.GetCurves(Message, true, RejectObjectsOnLockedLayers);
                    }
                }
                else
                {
                    polyResult = ed.GetCurves(Message, true, RejectObjectsOnLockedLayers);
                }

                if (polyResult.Status == PromptStatus.Error)
                {
                    continue;
                }
                if (polyResult.Status != PromptStatus.OK)
                {
                    return null;
                }
                EntObjectId = polyResult.Value[0].ObjectId;
                Entity SelectedEntity = EntObjectId.GetNoTransactionDBObject(OpenMode.ForRead) as Entity;
                if (SelectedEntity is Polyline ProjectionTargetPolyline)
                {
                    if (Clone)
                    {
                        return (Polyline)ProjectionTargetPolyline.Clone();
                    }
                    else
                    {
                        return ProjectionTargetPolyline;
                    }
                }
                else if (AllowOtherCurveType && SelectedEntity is Curve SelectedCurve)
                {
                    if (SelectedCurve.ToPolyline() is Polyline ConvertedCurveAsPoly)
                    {
                        return ConvertedCurveAsPoly;
                    }
                }


                Generic.WriteMessage("L'objet sélectionné n'est pas une polyligne. \n");
            }
        }

        public static bool GetHatch(this Editor ed, out ObjectId HatchObjectId, string AskText = null)
        {
            HatchObjectId = ObjectId.Null;
            SelectionSet BaseSelection = ed.SelectImplied()?.Value;
            if (BaseSelection?.Count > 0)
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
                var option = new PromptEntityOptions("\n" + (string.IsNullOrWhiteSpace(AskText) ? "Selectionnez une hachure" : AskText))
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

        public static bool GetHatch(this Editor ed, out Hatch Hachure, string AskText = null)
        {
            Hachure = null;
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    while (true)
                    {
                        if (!ed.GetHatch(out ObjectId HatchObjectId, AskText))
                        {
                            return false;
                        }

                        if (HatchObjectId.GetDBObject() is Hatch hatch)
                        {
                            Hachure = hatch;
                            break;
                        }
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }

            return true;
        }

        public static bool IsInLockedViewport(this Editor ed)
        {
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Viewport viewport = ed.ActiveViewportId.GetDBObject(OpenMode.ForRead) as Viewport;
                tr.Commit();
                return viewport?.Locked == true;
            }
        }
        public static bool IsInPaperSpace(this Editor ed)
        {
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Viewport viewport = ed.ActiveViewportId.GetDBObject(OpenMode.ForRead) as Viewport;
                tr.Commit();
                return viewport?.Number == 1;
            }
        }

        public static void ViewPlan(this Editor ed)
        {
            //From https://cadxp.com/topic/61249-net-c-%C3%A9quivalents-des-commandes-lisp-ucs-dview-plan/?do=findComment&comment=349377
            var ucs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;
            using (var view = ed.GetCurrentView())
            {
                var dcsToWcs =
                    Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) *
                    Matrix3d.Displacement(view.Target.GetAsVector()) *
                    Matrix3d.PlaneToWorld(view.ViewDirection);
                var centerPoint =
                    new Point3d(view.CenterPoint.X, view.CenterPoint.Y, 0.0)
                    .TransformBy(dcsToWcs);
                view.ViewDirection = ucs.Zaxis;
                view.ViewTwist = ucs.Xaxis.GetAngleTo(Vector3d.XAxis, ucs.Zaxis);
                var wcsToDcs =
                    Matrix3d.WorldToPlane(view.ViewDirection) *
                    Matrix3d.Displacement(view.Target.GetAsVector().Negate()) *
                    Matrix3d.Rotation(view.ViewTwist, view.ViewDirection, view.Target);
                centerPoint = centerPoint.TransformBy(wcsToDcs);
                view.CenterPoint = new Point2d(centerPoint.X, centerPoint.Y);
                ed.SetCurrentView(view);
            }
        }
    }
}
