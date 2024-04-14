using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Hatchs
    {
        public static Hatch ApplyHatchV2(Polyline OutsidePolyline, IEnumerable<Curve> OuterMostCurves, Hatch hachure)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.TopTransaction)
            {
                BlockTableRecord btr = Generic.GetCurrentSpaceBlockTableRecord(tr);
                DrawOrderTable orderTable = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                ObjectIdCollection DrawOrderCollection = new ObjectIdCollection();
                if (OutsidePolyline.IsNewObject)
                {
                    OutsidePolyline.Closed = true;
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
                        (InsidePolyline as Polyline).Closed = true;
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
                try
                {
                    oHatch.AppendLoop(HatchLoopTypes.External, OutsideObjId);
                }
                catch (Exception ex)
                {
                    (OutsidePolyline.Clone() as Entity).AddToDrawing(5);
                    Generic.WriteMessage("Une erreur est survenue lors de la création de la hachure");
                    Debug.WriteLine(ex.ToString());
                    return null;
                }
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
                return oHatch;
            }
        }

    }
}
