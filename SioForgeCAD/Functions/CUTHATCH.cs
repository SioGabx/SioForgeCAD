using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class CUTHATCH
    {
        public static void Cut()
        {
            Editor ed = Generic.GetEditor();
            var option = new PromptSelectionOptions()
            {
                MessageForAdding = "Selectionnez une polyligne",
            };
            var Result = ed.GetSelection(option);
            if (Result.Status != PromptStatus.OK)
            {
                return;
            }

            Hatch hachure = null;
            Polyline polyline = null;
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var objectId in Result.Value.GetObjectIds())
                {
                    DBObject ent = objectId.GetDBObject();
                    if (ent is Polyline)
                    {
                        polyline = ent as Polyline;
                    }
                    else if (ent is Hatch)
                    {
                        hachure = ent as Hatch;
                    }
                }
                if (polyline is null)
                {
                    return;
                }
                Generic.WriteMessage("Polyligne is clockwise : " + polyline.IsClockwise());

                using (GetCutHatchLinePointTransient getCutHatchLinePointTransient = new GetCutHatchLinePointTransient(null, null))
                {
                    getCutHatchLinePointTransient.polyline = polyline;
                    var getCutHatchLinePointResultOne = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", null);
                    if (getCutHatchLinePointResultOne.PromptPointResult.Status == PromptStatus.OK)
                    {
                        Points Origin = Points.GetFromPromptPointResult(getCutHatchLinePointResultOne.PromptPointResult);
                        getCutHatchLinePointTransient.Origin = Origin;
                        var getCutHatchLinePointResultTwo = getCutHatchLinePointTransient.GetPoint("Selectionnez un point", null);
                        if (getCutHatchLinePointResultTwo.PromptPointResult.Status == PromptStatus.OK)
                        {
                            Points EndPoint = Points.GetFromPromptPointResult(getCutHatchLinePointResultTwo.PromptPointResult);
                            /*var Cuted = GetCutPolyline(polyline, Origin, EndPoint);
                            Generic.WriteMessage(Cuted.Length);
                            foreach (var item in Cuted)
                            {
                               // item.AddToDrawingCurrentTransaction();
                            }
                            */
                            //TODO
                        }
                    }
                }


                tr.Commit();
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
            using (Line line = new Line(OriginNearestPt, NearestPt))
            {
                polyline.Cut(line);
                //return polyline.CutLoopToHalves(line);
            }
            return new Polyline[] { polyline, polyline };
        }

        public class GetCutHatchLinePointTransient : GetPointTransient
        {
            public Polyline polyline { get; set; }
            public Points Origin { get; set; }

            public GetCutHatchLinePointTransient(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction)
            {
            }


            public override void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
            {
                var ObjectCollection = new DBObjectCollection();

                var NearestPt = FoundNearestPointOnPolyline(polyline, moveToPt);

                if (Origin is Points.Null)
                {
                    Circle circle = new Circle(NearestPt, Vector3d.ZAxis, 0.5);
                    ObjectCollection.Add(circle);
                }
                else
                {
                    var OriginNearestPt = FoundNearestPointOnPolyline(polyline, Origin.SCG);
                    Line line = new Line(OriginNearestPt, NearestPt);
                    ObjectCollection.Add(line);
                }

                foreach(DBObject Ent in GetStaticEntities)
                {
                    Ent.Dispose();
                }


                SetStaticEntities = ObjectCollection;
                foreach (Autodesk.AutoCAD.GraphicsInterface.Drawable entity in StaticDrawable)
                {
                    RedrawTransEntities(entity);
                }
            }


            public override bool IsValidPoint(PromptPointResult pointResult)
            {
                if (Origin is Points.Null)
                {
                    return base.IsValidPoint(pointResult);
                }
                var Pt = Points.GetFromPromptPointResult(pointResult);
                return GetCutPolyline(polyline, Origin, Pt).Length > 1;
            }




        }



    }

}