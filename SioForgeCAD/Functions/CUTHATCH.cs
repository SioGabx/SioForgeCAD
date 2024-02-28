using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ViewModel.PointCloudManager;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class CUTHATCH
    {
        public static void Cut()
        {
            if (!GetHatch(out Hatch Hachure, out Polyline Boundary))
            {
                return;
            }


            if (Hachure is null || Boundary is null || Math.Floor(Boundary.TryGetArea()) != Math.Floor(Hachure.TryGetArea()))
            {
                if (Boundary?.IsSelfIntersecting(out _) == true)
                {
                    Generic.WriteMessage("Impossible de découpper une hachure qui se coupe elle-même.");
                }
                else
                {
                    Generic.WriteMessage("Impossible de découpper cette hachure.");
                }
                return;
            }
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();


            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("Dessiner une ligne de coupe ou une ligne/polyligne existante ?");
            const string NewKeyword = "Nouvelle";
            const string ExistKeyword = "Existante";


            promptKeywordOptions.Keywords.Add(ExistKeyword);
            promptKeywordOptions.Keywords.Add(NewKeyword);
            promptKeywordOptions.Keywords.Default = ExistKeyword;
            promptKeywordOptions.AppendKeywordsToMessage = true;
            promptKeywordOptions.AllowArbitraryInput = false;
            var SelectOption = ed.GetKeywords(promptKeywordOptions);

            Polyline CutLine;
                if (SelectOption.StringResult == NewKeyword)
                {
                    CutLine = GetCutLine(Boundary);
                }
                else
                {
                    CutLine = GetCutPolyline(Boundary);
                }

            if (CutLine != null)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var Cuted = Boundary.Cut(CutLine);
                    ApplyCutting(Boundary, Hachure, Cuted);
                    Generic.WriteMessage($"La hachure à été divisée en {Cuted.Count}");
                    tr.Commit();
                }
            }
            CutLine?.Dispose();
        }

        public static Polyline GetCutLine(Polyline Boundary)
        {
            Database db = Generic.GetDatabase();

            using (GetCutHatchLinePointTransient getCutHatchLinePointTransient = new GetCutHatchLinePointTransient(null, null))
            {
                getCutHatchLinePointTransient.Polyline = Boundary;
                var getCutHatchLinePointResultOne = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", null);
                if (getCutHatchLinePointResultOne.PromptPointResult.Status == PromptStatus.OK)
                {
                    Points Origin = Points.GetFromPromptPointResult(getCutHatchLinePointResultOne.PromptPointResult).Flatten();

                    getCutHatchLinePointTransient.Origin = Origin;
                    var OriginNearestPt = FoundNearestPointOnPolyline(Boundary, Origin.SCG);
                    DBPoint dBPoint = new DBPoint(OriginNearestPt);
                    var dBPointObjectId = dBPoint.AddToDrawing();
                    (Points Point, PromptPointResult PromptPointResult) getCutHatchLinePointResultTwo;
                    try
                    {
                        getCutHatchLinePointResultTwo = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", Origin);
                    }
                    finally
                    {
                        dBPointObjectId.EraseObject();
                    }
                    if (getCutHatchLinePointResultTwo.PromptPointResult.Status == PromptStatus.OK)
                    {
                        Points EndPoint = Points.GetFromPromptPointResult(getCutHatchLinePointResultTwo.PromptPointResult).Flatten();
                        Polyline CutLine = GetPolylineFromNearestPointOnBoundary(Boundary, Origin, EndPoint);
                        return CutLine;
                    }
                }
            }
            return null;
        }

        public static Polyline GetPolylineFromNearestPointOnBoundary(Polyline Boundary, Points Origin, Points EndPoint)
        {
            var OriginNearestPt = FoundNearestPointOnPolyline(Boundary, Origin.SCG);
            var EndNearestPt = FoundNearestPointOnPolyline(Boundary, EndPoint.SCG);

            Vector3d LineVector = OriginNearestPt - EndNearestPt;

            using (Line line = new Line(OriginNearestPt.Displacement(LineVector, .1), EndNearestPt.Displacement(-LineVector, .1)))
            {
                return line.ToPolyline();
            }
        }


        public static Polyline GetCutPolyline(Polyline Boundary)
        {
            Editor editor = Generic.GetEditor();
            TypedValue[] filterList = new TypedValue[] {
                new TypedValue((int)DxfCode.Operator, "<or"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Operator, "or>"),
            };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un polyline qui coupe la hachure",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            while (true)
            {
                PromptSelectionResult promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));
                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return null;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count == 1)
                    {
                        ObjectId SelectedObjectId = promptResult.Value.GetObjectIds().First();
                        DBObject Entity = SelectedObjectId.GetNoTransactionDBObject(OpenMode.ForRead);
                        Polyline polyline;
                        if (Entity is Polyline)
                        {
                            polyline = (Polyline)Entity.Clone();
                        }
                        else if (Entity is Line)
                        {
                            polyline = ((Line)Entity).ToPolyline();
                        }
                        else
                        {
                            continue;
                        }
                        
                        if (IsValidCutLine(Boundary, polyline))
                        {
                            return polyline;
                        }
                        else
                        {
                            Generic.WriteMessage("La polyligne ne coupe pas la hachure");
                            continue;
                        }

                    }
                }
            }

        }



        public static bool AskSelectHatch(out ObjectId HatchObjectId)
        {
            Editor ed = Generic.GetEditor();
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
                    AllowNone = false,
                    AllowObjectOnLockedLayer = false,
                };
                option.SetRejectMessage("\nVeuillez selectionner seulement des hachures");
                option.AddAllowedClass(typeof(Hatch), false);
                var Result = ed.GetEntity(option);
                if (Result.Status != PromptStatus.OK)
                {
                    return false;
                }
                HatchObjectId = Result.ObjectId;
            }
            return true;
        }

        public static bool GetHatch(out Hatch Hachure, out Polyline Polyline)
        {
            Hachure = null;
            Polyline = null;
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {

                    if (!AskSelectHatch(out ObjectId HatchObjectId))
                    {
                        return true;
                    }

                    if (HatchObjectId.GetDBObject() is Hatch hatch)
                    {
                        Hachure = hatch;
                    }
                    else
                    {
                        return false;
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }

            Hachure?.GetHatchPolyline(out Polyline);
            return true;
        }


        public static void ApplyCutting(Polyline polyline, Hatch hachure, List<Polyline> Cuts)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.TopTransaction)
            {
                ObjectId ModelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                BlockTableRecord btr = tr.GetObject(ModelSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                DrawOrderTable orderTable = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                ObjectIdCollection DrawOrderCollection = new ObjectIdCollection();

                foreach (var item in Cuts)
                {
                    polyline.CopyPropertiesTo(item);
                    var polylineObjectId = btr.AppendEntity(item);
                    DrawOrderCollection.Add(polylineObjectId);
                    tr.AddNewlyCreatedDBObject(item, true);

                    ObjectIdCollection ObjIds = new ObjectIdCollection
                    {
                        polylineObjectId
                    };
                    Hatch oHatch = new Hatch();
                    var oHatchObjectId = btr.AppendEntity(oHatch);
                    tr.AddNewlyCreatedDBObject(oHatch, true);
                    oHatch.Associative = true;
                    oHatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                    oHatch.EvaluateHatch(true);
                    hachure.CopyPropertiesTo(oHatch);
                    DrawOrderCollection.Add(oHatchObjectId);
                    orderTable.MoveAbove(ObjIds, oHatchObjectId);
                }
                //Keep same draw order as old hatch
                orderTable.MoveBelow(DrawOrderCollection, hachure.ObjectId);

                polyline.ObjectId.EraseObject();
                hachure.ObjectId.EraseObject();
            }
        }

        public static Point3d FoundNearestPointOnPolyline(Polyline polyline, Point3d point)
        {
            return polyline.GetClosestPointTo(point, false);
        }

        public static bool IsValidCutLine(Polyline Boundary, Polyline CutLine)
        {
            List<Polyline> CuttedPolyline = Boundary.Cut(CutLine);
            int NumberOfPolyline = CuttedPolyline.Count;
            CuttedPolyline.DeepDispose();
            return NumberOfPolyline > 1;
        }

        public class GetCutHatchLinePointTransient : GetPointTransient
        {
            public Polyline Polyline { get; set; }
            public Points Origin { get; set; }

            public GetCutHatchLinePointTransient(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction)
            {
            }

            public override PromptPointOptions SetPromptPointOptions(PromptPointOptions PromptPointOptions)
            {
                PromptPointOptions.UseBasePoint = false;
                PromptPointOptions.UseDashedLine = false;
                return base.SetPromptPointOptions(PromptPointOptions);
            }

            public override void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
            {
                Editor ed = Generic.GetEditor();

                var ObjectCollection = new DBObjectCollection();
                Polyline boundaryClone = Polyline.Clone() as Polyline;
                var NearestPt = FoundNearestPointOnPolyline(boundaryClone, moveToPt);

                //Set the diameter to 1.5% of the current View height
                double CircleRadius = ed.GetCurrentViewSize().Height * (1.5 / 100) / 2;

                Circle Circle = new Circle(NearestPt, Vector3d.ZAxis, CircleRadius);
                DBPoint Point = new DBPoint(NearestPt);
                ObjectCollection.Add(Point);
                ObjectCollection.Add(Circle);
                ObjectCollection.Add(boundaryClone);

                if (!(Origin is Points.Null))
                {
                    var OriginNearestPt = FoundNearestPointOnPolyline(boundaryClone, Origin.SCG);
                    Line Line = new Line(OriginNearestPt, NearestPt);
                    Circle OriginCircle = new Circle(OriginNearestPt, Vector3d.ZAxis, CircleRadius);
                    DBPoint OriginPoint = new DBPoint(OriginNearestPt);
                    ObjectCollection.Add(Line);
                    ObjectCollection.Add(OriginCircle);
                    ObjectCollection.Add(OriginPoint);
                    ObjectCollection.Add(Circle);
                    ObjectCollection.Add(Point);
                }

                DisposeStaticEntities();
                DisposeStaticDrawable();

                SetStaticEntities = ObjectCollection;
                foreach (Autodesk.AutoCAD.GraphicsInterface.Drawable entity in StaticDrawable)
                {
                    RedrawTransEntities(entity);
                }
            }
            public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
            {
                return Color.FromColorIndex(ColorMethod.ByColor, 0);
            }

            public override bool IsValidPoint(PromptPointResult pointResult)
            {
                if (Origin is Points.Null)
                {
                    return base.IsValidPoint(pointResult);
                }
                var Pt = Points.GetFromPromptPointResult(pointResult);

                Polyline CutLine = GetPolylineFromNearestPointOnBoundary(Polyline, Origin, Pt);
                bool ValidPoint = IsValidCutLine(Polyline, CutLine);
                CutLine.Dispose();
                return ValidPoint;
            }
        }
    }

}