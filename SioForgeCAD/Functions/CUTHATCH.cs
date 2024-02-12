using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class CUTHATCH
    {

        public static bool GetHatch(out Hatch Hachure, out Polyline Polyline)
        {

            Hachure = null;
            Polyline = null;

            Editor ed = Generic.GetEditor();
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
                return true;
            }

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (Result.ObjectId.GetDBObject() is Hatch hatch)
                {
                    Hachure = hatch;
                }
                else
                {
                    return false;
                }

                if (!Hachure.Associative)
                {
                    Hachure.ReGenerateBoundaryCommand();
                    Hachure.GetAssociatedBoundary(out Polyline);
                    Hachure.CopyPropertiesTo(Polyline);
                }
                else
                {

                    var NumberOfBoundary = Hachure.GetAssociatedBoundary(out Polyline BaseBoundary);
                    if (NumberOfBoundary > 1)
                    {
                        Hachure.ReGenerateBoundaryCommand();
                        double NewNumberOfBoundary = Hachure.GetAssociatedBoundary(out Polyline);
                        if (NewNumberOfBoundary > 1)
                        {
                            var objectIdCollection = Hachure.GetAssociatedObjectIds();
                            Polyline = null;
                            foreach (ObjectId BoundaryElementObjectId in objectIdCollection)
                            {
                                var BoundaryElementEntity = BoundaryElementObjectId.GetEntity() as Polyline;
                                if (Polyline == null)
                                {
                                    Polyline = BoundaryElementEntity;
                                }
                                else
                                {
                                    Polyline.JoinPolyline(BoundaryElementEntity);
                                }
                            }
                            Polyline.Cleanup();
                        }

                        BaseBoundary.CopyPropertiesTo(Polyline);
                    }
                    else
                    {
                        Polyline = BaseBoundary;
                    }
                }
                tr.Commit();

                if (Polyline is null)
                {
                    return false;
                }

            }
            return true;
        }



        public static void Cut()
        {
            if (!GetHatch(out Hatch hachure, out Polyline polyline))
            {
                return;
            }
            if (hachure is null || polyline is null)
            {
                return;
            }


            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                using (GetCutHatchLinePointTransient getCutHatchLinePointTransient = new GetCutHatchLinePointTransient(null, null))
                {
                    getCutHatchLinePointTransient.Polyline = polyline;
                    var getCutHatchLinePointResultOne = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", null);
                    if (getCutHatchLinePointResultOne.PromptPointResult.Status == PromptStatus.OK)
                    {
                        Points Origin = Points.GetFromPromptPointResult(getCutHatchLinePointResultOne.PromptPointResult);
                        getCutHatchLinePointTransient.Origin = Origin;
                        var getCutHatchLinePointResultTwo = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", null);
                        if (getCutHatchLinePointResultTwo.PromptPointResult.Status == PromptStatus.OK)
                        {
                            Points EndPoint = Points.GetFromPromptPointResult(getCutHatchLinePointResultTwo.PromptPointResult);
                            var Cuted = GetCutPolyline(polyline, Origin, EndPoint);
                            Generic.WriteMessage(Cuted.Length);
                            ApplyCutting(polyline, hachure, Cuted);
                        }
                    }
                }


                tr.Commit();
            }
        }


        public static void ApplyCutting(Polyline polyline, Hatch hachure, Polyline[] Cuts)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.TopTransaction)
            {
                foreach (var item in Cuts)
                {
                    ObjectId ModelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                    BlockTableRecord btr = tr.GetObject(ModelSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    polyline.CopyPropertiesTo(item);

                    var Loop = btr.AppendEntity(item);
                    tr.AddNewlyCreatedDBObject(item, true);

                    ObjectIdCollection ObjIds = new ObjectIdCollection
                    {
                        Loop
                    };
                    Hatch oHatch = new Hatch();
                    btr.AppendEntity(oHatch);
                    tr.AddNewlyCreatedDBObject(oHatch, true);
                    oHatch.Associative = true;
                    oHatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                    oHatch.EvaluateHatch(true);
                    hachure.CopyPropertiesTo(oHatch);
                }

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


            public override void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
            {
                var ObjectCollection = new DBObjectCollection();

                var NearestPt = FoundNearestPointOnPolyline(Polyline, moveToPt);
                double CircleRadius = 0.05;
                if (Origin is Points.Null)
                {
                    Circle Circle = new Circle(NearestPt, Vector3d.ZAxis, CircleRadius);
                    DBPoint Point = new DBPoint(NearestPt);
                    ObjectCollection.Add(Point);
                    ObjectCollection.Add(Circle);
                }
                else
                {
                    var OriginNearestPt = FoundNearestPointOnPolyline(Polyline, Origin.SCG);
                    Line Line = new Line(OriginNearestPt, NearestPt);
                    Circle OriginCircle = new Circle(OriginNearestPt, Vector3d.ZAxis, CircleRadius);
                    DBPoint OriginPoint = new DBPoint(OriginNearestPt);

                    Circle Circle = new Circle(NearestPt, Vector3d.ZAxis, CircleRadius);
                    DBPoint Point = new DBPoint(NearestPt);

                    ObjectCollection.Add(Line);
                    ObjectCollection.Add(OriginCircle);
                    ObjectCollection.Add(OriginPoint);
                    ObjectCollection.Add(Circle);
                    ObjectCollection.Add(Point);
                }

                foreach (DBObject Ent in GetStaticEntities)
                {
                    Ent.Dispose();
                }


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
                return GetCutPolyline(Polyline, Origin, Pt).Length > 1;
            }
        }
    }

}