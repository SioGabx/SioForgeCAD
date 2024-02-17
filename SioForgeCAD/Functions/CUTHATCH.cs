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
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class CUTHATCH
    {
        public static void Cut()
        {
            if (!GetHatch(out Hatch hachure, out Polyline polyline))
            {
                return;
            }
            if (hachure is null || polyline is null || Math.Floor(polyline.Area) != Math.Floor(hachure.Area))
            {
                Generic.WriteMessage("Impossible de découpper cette hachure.");
                return;
            }
            Database db = Generic.GetDatabase();

            using (GetCutHatchLinePointTransient getCutHatchLinePointTransient = new GetCutHatchLinePointTransient(null, null))
            {
                getCutHatchLinePointTransient.Polyline = polyline;
                var getCutHatchLinePointResultOne = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", null);
                if (getCutHatchLinePointResultOne.PromptPointResult.Status == PromptStatus.OK)
                {
                    Points Origin = Points.GetFromPromptPointResult(getCutHatchLinePointResultOne.PromptPointResult).Flatten();
                    
                    getCutHatchLinePointTransient.Origin = Origin;
                    var OriginNearestPt = FoundNearestPointOnPolyline(polyline, Origin.SCG);
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
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Points EndPoint = Points.GetFromPromptPointResult(getCutHatchLinePointResultTwo.PromptPointResult).Flatten();
                            var Cuted = GetCutPolyline(polyline, Origin, EndPoint);
                            ApplyCutting(polyline, hachure, Cuted);
                            Generic.WriteMessage($"La hachure à été divisée en {Cuted.Length}");
                            tr.Commit();
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


        public static void ApplyCutting(Polyline polyline, Hatch hachure, Polyline[] Cuts)
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

        public static Polyline[] GetCutPolyline(Polyline polyline, Points Origin, Points EndPoint)
        {
            var OriginNearestPt = FoundNearestPointOnPolyline(polyline, Origin.SCG);
            var NearestPt = FoundNearestPointOnPolyline(polyline, EndPoint.SCG);

            Vector3d LineVector = OriginNearestPt - NearestPt;

            using (Line line = new Line(OriginNearestPt.Displacement(LineVector, .1), NearestPt.Displacement(-LineVector, .1)))
            {
                return polyline.Cut(line).ToArray();
            }
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
                var ObjectCollection = new DBObjectCollection();

                var NearestPt = FoundNearestPointOnPolyline(Polyline, moveToPt);

                //Set the diameter to 1.5% of the current View height
                double CircleRadius = Generic.GetCurrentViewSize().Height * (1.5 / 100) / 2;

                Circle Circle = new Circle(NearestPt, Vector3d.ZAxis, CircleRadius);
                DBPoint Point = new DBPoint(NearestPt);
                ObjectCollection.Add(Point);
                ObjectCollection.Add(Circle);
                ObjectCollection.Add(Polyline.Clone() as Polyline);

                if (!(Origin is Points.Null))
                {
                    var OriginNearestPt = FoundNearestPointOnPolyline(Polyline, Origin.SCG);
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
                Polyline[] CuttedPolyline = GetCutPolyline(Polyline, Origin, Pt);
                int NumberOfPolyline = CuttedPolyline.Length;
                CuttedPolyline.DeepDispose();
                return NumberOfPolyline > 1;
            }
        }
    }

}