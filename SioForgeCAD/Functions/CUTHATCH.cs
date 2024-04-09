using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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
        public static void CutHoleHatch()
        {
            if (!GetHatch(out Hatch Hachure))
            {
                return;
            }
            if (!Hachure.GetHatchPolylineV2(out List<Curve> ExternalCurves, out List<(Curve curve, HatchLoopTypes looptype)> OtherCurves))
            {
                return;
            }
            //Get the existing boundary style (if is Associative we get the curve, if not, we copy the hatch style)
            Entity ExistingBoundaryStyle = Hachure;
            if (Hachure.Associative)
            {
                Hachure.GetAssociatedBoundary(out Curve AssociatedBoundary);
                ExistingBoundaryStyle = AssociatedBoundary;
            }
            List<Curve> ExternalMergedCurves = ExternalCurves.Join();
            ExternalCurves.RemoveCommun(ExternalMergedCurves).DeepDispose();
            List<Curve> InnerCurves = OtherCurves.Select(tuple => tuple.curve).ToList();
            if (Hachure.HatchStyle == HatchStyle.Ignore)
            {
                InnerCurves.DeepDispose();
                InnerCurves.Clear();
            }
            List<Curve> InnerMergedCurves = InnerCurves.Join();
            InnerCurves.RemoveCommun(InnerMergedCurves).DeepDispose();

            if (Hachure is null || ExternalMergedCurves is null || ExternalMergedCurves.Count == 0)
            {
                Generic.WriteMessage("Impossible de découpper cette hachure.");
                return;
            }
            if (ExternalMergedCurves.Count > 1)
            {
                Generic.WriteMessage("Impossible de découpper une hachure combinée.");
                return;
            }
            var Boundary = ExternalMergedCurves[0].ToPolyline();
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            ed.SetImpliedSelection(new ObjectId[0]);

            Polyline CutLine = GetCutPolyline(Boundary, out PromptStatus promptResult);
            if (promptResult == PromptStatus.Keyword)
            {
                CutLine = GetCutLine(Boundary);
            }

            if (CutLine == null)
            {
                return;
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    List<Polyline> CuttedPolyline = PolygonOperation.LastSliceResult is null ? Boundary.Slice(CutLine) : PolygonOperation.LastSliceResult;
                    int NumberOfPolyline = CuttedPolyline.Count;
                    if (NumberOfPolyline > 1)
                    {
                        int NumberOfSlice = 0;
                        while (CuttedPolyline.Count > 0)
                        {
                            Polyline NewBoundary = CuttedPolyline.FirstOrDefault();
                            List<Curve> NewBoundaryHoles = new List<Curve>();

                            //Subtract the new edge with each hole from the polybase. If the polyline is divided by two or more, we add it to the list of curves remaining to be processed
                            CuttedPolyline.Remove(NewBoundary);
                            PolygonOperation.Substraction(new PolyHole(NewBoundary, null), InnerMergedCurves.Cast<Polyline>(), out var SubResult);
                            //Weird case where Substraction return 0 
                            if (SubResult.Count == 0)
                            {
                                Generic.WriteMessage("Une erreur c'est produite : impossible de découpper cette hachure. L'opération a été annulée");
                                NewBoundary.Dispose();
                                tr.Commit();
                                return;
                            }
                            for (int i = 1; i < SubResult.Count; i++)
                            {
                                //Add back each cut
                                CuttedPolyline.Add(SubResult[i].Boundary);
                            }

                            //Parse the first in list, if there is no new cut, this is the same as NewBoundary
                            Polyline SubstractedNewBoundary = SubResult[0].Boundary;
                            NewBoundaryHoles = SubResult[0].Holes.Cast<Curve>().ToList();
                            if (NewBoundary != SubstractedNewBoundary) { NewBoundary.Dispose(); }

                            ExistingBoundaryStyle.CopyPropertiesTo(SubstractedNewBoundary);
                            ApplyHatchV2(SubstractedNewBoundary, NewBoundaryHoles, Hachure);

                            CuttedPolyline.Remove(SubstractedNewBoundary);
                            SubstractedNewBoundary.Dispose();

                            NumberOfSlice++;
                        }
                        Generic.WriteMessage($"La hachure à été divisée en {NumberOfSlice}");

                    }
                    else if (CheckIfHole(Boundary, CutLine))
                    {
                        ExistingBoundaryStyle.CopyPropertiesTo(CutLine);
                        ExistingBoundaryStyle.CopyPropertiesTo(Boundary);

                        //Hatch cutline -> remove the content if the cutline is cutting a hole

                        PolygonOperation.Substraction(new PolyHole(CutLine, null), InnerMergedCurves.Cast<Polyline>(), out var CutLineSubResult);
                        foreach (var item in CutLineSubResult)
                        {
                            ApplyHatchV2(item.Boundary.Clone() as Polyline, item.Holes.Cast<Curve>().ToList(), Hachure);
                        }


                        //Generate a union of existing hole + new one
                        InnerMergedCurves.Add(CutLine);
                        PolygonOperation.Union(PolyHole.CreateFromList(InnerMergedCurves.Cast<Polyline>()), out var MergedHoles);
                        var Holes = MergedHoles.GetBoundaries().Cast<Curve>().ToList();
                        //If hole is inside an hole, we add a new Hatch inside
                        foreach (var CurveA in Holes.ToArray())
                        {
                            var CurveAPoly = CurveA as Polyline;

                            foreach (var CurveB in Holes.ToArray())
                            {
                                if (CurveA != CurveB)
                                {
                                    if ((CurveA as Polyline).IsInside(CurveB as Polyline, true))
                                    {
                                        ApplyHatchV2(CurveAPoly, new List<Curve>(), Hachure);
                                        Holes.Remove(CurveA);
                                        CurveA.Dispose();
                                        break;
                                    }
                                }
                            }
                        }

                        //Apply hatch to the boundary with the union holes
                        ApplyHatchV2(Boundary, Holes, Hachure);
                        Generic.WriteMessage($"Un trou a été créé dans la hachure");
                        MergedHoles.DeepDispose();
                        CutLineSubResult.Dispose();
                    }

                    foreach (ObjectId item in Hachure.GetAssociatedObjectIds())
                    {
                        item.EraseObject();
                    }
                    Hachure.ObjectId.EraseObject();
                    tr.Commit();
                }
                finally
                {
                    //Cleanup
                    Hachure.Dispose();
                    InnerMergedCurves.DeepDispose();
                    ExternalMergedCurves.DeepDispose();
                    ExistingBoundaryStyle.Dispose();
                    CutLine.Dispose();
                    Boundary.Dispose();
                    PolygonOperation.SetSliceCache(null, null);
                }
            }
        }


        public static void ApplyHatchV2(Polyline OutsidePolyline, List<Curve> OuterMostCurves, Hatch hachure)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.TopTransaction)
            {
                BlockTableRecord btr = Generic.GetCurrentSpaceBlockTableRecord(tr); ;
                DrawOrderTable orderTable = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                ObjectIdCollection DrawOrderCollection = new ObjectIdCollection();
                if (OutsidePolyline.IsNewObject)
                {
                    OutsidePolyline.AddToDrawingCurrentTransaction();
                }

                List<ObjectId> Inside = new List<ObjectId>();
                foreach (var InsidePolyline in OuterMostCurves)
                {
                    if (InsidePolyline == OutsidePolyline) { continue; }
                    OutsidePolyline.CopyPropertiesTo(InsidePolyline);
                    ObjectId polylineObjectId = InsidePolyline.ObjectId;
                    if (InsidePolyline.IsNewObject)
                    {
                        polylineObjectId = btr.AppendEntity(InsidePolyline);
                        tr.AddNewlyCreatedDBObject(InsidePolyline, true);
                    }
                    if (!DrawOrderCollection.Contains(polylineObjectId))
                    {
                        DrawOrderCollection.Add(polylineObjectId);
                    }
                    Inside.Add(polylineObjectId);
                }
                if (!DrawOrderCollection.Contains(OutsidePolyline.ObjectId))
                {
                    DrawOrderCollection.Add(OutsidePolyline.ObjectId);
                }
                ObjectIdCollection OutsideObjId = new ObjectIdCollection { OutsidePolyline.ObjectId };


                Hatch oHatch = new Hatch();
                var oHatchObjectId = btr.AppendEntity(oHatch);
                tr.AddNewlyCreatedDBObject(oHatch, true);
                oHatch.Associative = true;
                oHatch.AppendLoop(HatchLoopTypes.External, OutsideObjId);

                foreach (var item in Inside)
                {
                    ObjectIdCollection InsideObjId = new ObjectIdCollection() { item };
                    bool TryAppendLoop(HatchLoopTypes hatchLoopTypes)
                    {
                        try
                        {
                            oHatch.AppendLoop(hatchLoopTypes, InsideObjId);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex + " hatchLoopTypes : " + hatchLoopTypes.ToString());

                        }
                        return false;
                    }

                    _ = TryAppendLoop(HatchLoopTypes.Default & HatchLoopTypes.Polyline) | TryAppendLoop(HatchLoopTypes.Outermost & HatchLoopTypes.Polyline) || TryAppendLoop(HatchLoopTypes.Derived & HatchLoopTypes.Polyline) ||
                        TryAppendLoop(HatchLoopTypes.Default);
                }


                oHatch.EvaluateHatch(true);
                hachure.CopyPropertiesTo(oHatch);
                //oHatch.HatchStyle = hachure.HatchStyle;
                oHatch.HatchStyle = HatchStyle.Normal;

                //Keep same draw order as old hatch
                orderTable.MoveAbove(DrawOrderCollection, oHatchObjectId);
                DrawOrderCollection.Insert(0, oHatchObjectId);
                try
                {
                    orderTable.MoveBelow(DrawOrderCollection, hachure.ObjectId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }


        static bool CheckIfHole(Polyline Boundary, Polyline CutLine)
        {
            if (!CutLine.Closed)
            {
                return false;
            }

            foreach (var Point in CutLine.GetPoints())
            {
                if (!Point.IsInsidePolyline(Boundary))
                {
                    return false;
                }
            }

            var IntersectionFound = new Point3dCollection();
            CutLine.IntersectWith(Boundary, Intersect.OnBothOperands, IntersectionFound, IntPtr.Zero, IntPtr.Zero);
            if (IntersectionFound.Count > 0)
            {
                return false;
            }
            return true;
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


        public static Polyline GetCutPolyline(Polyline Boundary, out PromptStatus promptStatus)
        {
            //To do : allow multiple selection
            Editor editor = Generic.GetEditor();
            TypedValue[] filterList = new TypedValue[] {
                new TypedValue((int)DxfCode.Operator, "<or"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.Start, "SPLINE"),
                new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                new TypedValue((int)DxfCode.Start, "ARC"),
                new TypedValue((int)DxfCode.Operator, "or>"),
            };

            const string NewLineKeyWord = "Nouvelle";
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions()
            {
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true,
            };
            selectionOptions.Keywords.Add(NewLineKeyWord);
            string kws = selectionOptions.Keywords.GetDisplayString(true);
            selectionOptions.MessageForAdding = "Selectionnez un polyline qui coupe la hachure ou " + kws;
            selectionOptions.KeywordInput += delegate (object sender, SelectionTextInputEventArgs e) { throw new Exception("Keyword") { }; };


            try
            {
                while (true)
                {
                    PromptSelectionResult promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));
                    promptStatus = promptResult.Status;
                    if (promptResult.Status == PromptStatus.Cancel || promptResult.Status == PromptStatus.Keyword)
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
                            if (Entity is Curve curve)
                            {
                                polyline = curve.ToPolyline();
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
            catch (Exception)
            {
                promptStatus = PromptStatus.Keyword;
                return null;
            }

        }


        public static bool GetHatch(out Hatch Hachure)
        {
            Hachure = null;
            var db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    while (true)
                    {
                        if (!ed.GetHatch(out ObjectId HatchObjectId))
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

        public static Point3d FoundNearestPointOnPolyline(Polyline polyline, Point3d point)
        {
            return polyline.GetClosestPointTo(point, false);
        }

        public static bool IsValidCutLine(Polyline Boundary, Polyline CutLine)
        {
            List<Polyline> CuttedPolyline = Boundary.Slice(CutLine);
            int NumberOfPolyline = CuttedPolyline.Count;
            //CuttedPolyline.Remove(Boundary);
            //CuttedPolyline.DeepDispose();
            return (NumberOfPolyline > 1) || CheckIfHole(Boundary, CutLine);
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