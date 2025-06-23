using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Commun.Extensions
{
    public static class EntityExtensions
    {
        public static List<T> Clone<T>(this IEnumerable<T> list)
        {
            List<T> NewList = new List<T>();
            foreach (var item in list)
            {
                if (item is Entity)
                {
                    NewList.Add((T)(item as Entity).Clone());
                }
            }
            return NewList;
        }

        public static void EraseObject(this Entity ObjectToErase)
        {
            Document doc = Generic.GetDocument();
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                if (ObjectToErase.IsErased)
                {
                    return;
                }
                try
                {
                    if (!ObjectToErase.IsWriteEnabled)
                    {
                        ObjectToErase.UpgradeOpen();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                if (!ObjectToErase.IsErased && ObjectToErase.ObjectId != ObjectId.Null)
                {
                    ObjectToErase.Erase(true);
                }
                tr.Commit();
            }
        }

        public static void CopyPropertiesTo(this Entity Origin, Entity Target)
        {
            if (Origin == null || Target == null)
            {
                return;
            }
            if (Origin.GetType() == Target.GetType())
            {
                if (Origin is Hatch OriginHatch)
                {
                    Hatch TargetHatch = Target as Hatch;
                    if (OriginHatch.IsGradient)
                    {
                        TargetHatch.SetHatchPattern(OriginHatch.PatternType, OriginHatch.PatternName);
                        TargetHatch.SetGradient(OriginHatch.GradientType, OriginHatch.GradientName);
                        TargetHatch.GradientOneColorMode = OriginHatch.GradientOneColorMode;
                        TargetHatch.GradientShift = OriginHatch.GradientShift;
                        TargetHatch.GradientAngle = OriginHatch.GradientAngle;
                        TargetHatch.SetGradientColors(OriginHatch.GetGradientColors());
                    }
                    if (OriginHatch.IsHatch)
                    {
                        TargetHatch.SetHatchPattern(OriginHatch.PatternType, OriginHatch.PatternName);
                        TargetHatch.HatchStyle = OriginHatch.HatchStyle;
                        TargetHatch.PatternSpace = OriginHatch.PatternSpace;
                        TargetHatch.PatternAngle = OriginHatch.PatternAngle;
                        TargetHatch.PatternDouble = OriginHatch.PatternDouble;
                    }
                    TargetHatch.ShadeTintValue = OriginHatch.ShadeTintValue;
                    TargetHatch.HatchObjectType = OriginHatch.HatchObjectType;
                    TargetHatch.BackgroundColor = OriginHatch.BackgroundColor;
                    TargetHatch.Normal = OriginHatch.Normal;
                    TargetHatch.Origin = OriginHatch.Origin;
                    TargetHatch.Elevation = OriginHatch.Elevation;
                }

                if (Origin is BlockReference OriginBlkRef)
                {
                    BlockReference TargetBlockReference = Target as BlockReference;
                    TargetBlockReference.Rotation = OriginBlkRef.Rotation;
                    TargetBlockReference.ScaleFactors = OriginBlkRef.ScaleFactors;
                }

                if (Origin is Polyline OriginPolyline)
                {
                    Polyline TargetPolyline = Target as Polyline;
                    TargetPolyline.Elevation = OriginPolyline.Elevation;
                    try
                    {
                        TargetPolyline.ConstantWidth = OriginPolyline.ConstantWidth;
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception)
                    {
                        //eInvalidInput
                        TargetPolyline.ConstantWidth = 0;
                    }
                    TargetPolyline.Thickness = OriginPolyline.Thickness;
                }
                if (Origin is Polyline2d OriginPolyline2d)
                {
                    Polyline TargetPolyline = Target as Polyline;
                    TargetPolyline.Elevation = OriginPolyline2d.Elevation;
                }

                if (Origin is Ole2Frame OriginOle2Frame)
                {
                    Ole2Frame TargetOle2Frame = Target as Ole2Frame;
                    TargetOle2Frame.AutoOutputQuality = OriginOle2Frame.AutoOutputQuality;
                    TargetOle2Frame.LockAspect = OriginOle2Frame.LockAspect;
                    TargetOle2Frame.WcsHeight = OriginOle2Frame.WcsHeight;
                    TargetOle2Frame.WcsWidth = OriginOle2Frame.WcsWidth;
                    TargetOle2Frame.ScaleHeight = OriginOle2Frame.ScaleHeight;
                    TargetOle2Frame.ScaleWidth = OriginOle2Frame.ScaleWidth;
                }
            }

            //Default for each Entities
            if (Origin.EntityColor.IsNone)
            {
                Target.Color = Color.FromColorIndex(ColorMethod.ByLayer, 0);
            }
            else if (Origin.EntityColor.IsByColor)
            {
                Target.Color = Origin.Color;
            }
            else
            {
                Target.ColorIndex = Origin.ColorIndex;
            }

            if (Origin.LayerId != ObjectId.Null)
            {
                Target.LayerId = Origin.LayerId;
            }

            if (Origin.Linetype != "")
            {
                Target.Linetype = Origin.Linetype;
            }

            Target.LinetypeScale = Origin.LinetypeScale;
            Target.LineWeight = Origin.LineWeight;
            if (Origin.Material != "")
            {
                Target.Material = Origin.Material;
            }

            if (Origin.OwnerId != ObjectId.Null)
            {
                Target.OwnerId = Origin.OwnerId;
            }

            Target.ReceiveShadows = Origin.ReceiveShadows;
            Target.Transparency = Origin.Transparency;
            Target.Visible = Origin.Visible;
            Target.XData = Origin.XData;
            Target.CastShadows = Origin.CastShadows;
            //Target.PlotStyleName = Origin.PlotStyleName;
            //Target.VisualStyleId = Origin.VisualStyleId;  
            //Target.Annotative = Origin.Annotative;
            //Target.MergeStyle = Origin.MergeStyle;
            //Target.MaterialMapper = Origin.MaterialMapper;
            //Target.ForceAnnoAllVisible = Origin.ForceAnnoAllVisible;
            //Target.FaceStyleId = Origin.FaceStyleId;
            //Target.EdgeStyleId = Origin.EdgeStyleId;
            //Target.DrawStream = Origin.DrawStream;

        }

        public static void CopyDrawOrderTo(this Entity Origin, Entity Target)
        {
            var db = Generic.GetDatabase();
            Transaction tr = db.TransactionManager.TopTransaction;
            BlockTableRecord btr = Generic.GetCurrentSpaceBlockTableRecord(tr);
            DrawOrderTable orderTable = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

            ObjectIdCollection DrawOrderCollection = new ObjectIdCollection();
            DrawOrderCollection.Insert(0, Target.ObjectId);
            try
            {
                orderTable.MoveBelow(DrawOrderCollection, Origin.ObjectId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public static double TryGetArea(this Entity ent)
        {
            if (ent?.IsDisposed != false)
            {
                return 0;
            }
            try
            {
                switch (ent)
                {
                    case Polyline _:
                        return ((Polyline)ent).Area;
                    case Hatch _:
                        return ((Hatch)ent).Area;
                    case Circle _:
                        return ((Circle)ent).Area;
                    case Ellipse _:
                        return ((Ellipse)ent).Area;
                    case Region _:
                        return ((Region)ent).Area;
                    case Arc _:
                        return ((Arc)ent).Area;
                    case Spline _:
                        return ((Spline)ent).Area;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return 0;
        }


        public static bool Flatten(this Entity entity)
        {
            if (entity is Polyline2d polyline2d)
            {
                polyline2d.Elevation = 0;
                using (Polyline pline = polyline2d.ToPolyline())
                {
                    polyline2d.CopyPropertiesTo(pline);
                    pline.AddToDrawing();
                    polyline2d.Erase();
                }
            }
            if (entity is Polyline3d polyline3d)
            {
                using (Polyline pline = polyline3d.ToPolyline())
                {
                    polyline3d.CopyPropertiesTo(pline);
                    pline.AddToDrawing();
                    polyline3d.Erase();
                }
            }
            else if (entity is Polyline polyline)
            {
                polyline.Elevation = 0;
                polyline.FixNormals();
                polyline.Flatten();
            }
            else if (entity is Ellipse ellipse)
            {
                ellipse.Center = ellipse.Center.Flatten();
            }
            else if (entity is BlockReference blockreference)
            {
                blockreference.Position = blockreference.Position.Flatten();
            }
            else if (entity is Circle circle)
            {
                circle.Center = circle.Center.Flatten();
            }
            else if (entity is Hatch hatch)
            {
                hatch.Elevation = 0;
            }
            else if (entity is DBText dbtext)
            {
                dbtext.Position = dbtext.Position.Flatten();
            }
            else if (entity is DBPoint dbpoint)
            {
                dbpoint.Position = dbpoint.Position.Flatten();
            }
            else if (entity is Leader)
            {
                //not implemented
            }
            else if (entity is Line line)
            {
                line.StartPoint = line.StartPoint.Flatten();
                line.EndPoint = line.EndPoint.Flatten();
            }
            else if (entity is MText mtext)
            {
                mtext.Location = mtext.Location.Flatten();
            }
            else if (entity is Ray ray)
            {
                ray.BasePoint = ray.BasePoint.Flatten();
                ray.SecondPoint = ray.SecondPoint.Flatten();
            }
            else if (entity is Xline xline)
            {
                xline.BasePoint = xline.BasePoint.Flatten();
                xline.SecondPoint = xline.SecondPoint.Flatten();
            }
            else if (entity is Helix helix)
            {
                helix.StartPoint = helix.StartPoint.Flatten();
                //helix.EndPoint = helix.EndPoint.Flatten(); //System.NotImplementedException 
                helix.Height = 0;
                helix.SetAxisPoint(helix.GetAxisPoint().Flatten(), true);
            }
            else if (entity is Arc arc)
            {
                arc.Center = arc.Center.Flatten();
            }
            else if (entity is Spline spline)
            {
                for (int i = 0; i < spline.NumControlPoints; i++)
                {
                    var point = spline.GetControlPointAt(i);
                    spline.SetControlPointAt(i, point.Flatten());
                }
            }
            else if (entity is Table table)
            {
                table.Position = table.Position.Flatten();
            }
            else if (entity is Wipeout wipeout)
            {
                var orient = wipeout.Orientation;
                wipeout.Orientation = new CoordinateSystem3d(orient.Origin.Flatten(), orient.Xaxis, orient.Yaxis);
            }
            else if (entity is RasterImage rasterimage)
            {
                var orient = rasterimage.Orientation;
                rasterimage.Orientation = new CoordinateSystem3d(orient.Origin.Flatten(), orient.Xaxis, orient.Yaxis);
            }
            else if (entity is RotatedDimension rotatedDimension)
            {
                rotatedDimension.TextPosition = rotatedDimension.TextPosition.Flatten();
                rotatedDimension.DimLinePoint = rotatedDimension.DimLinePoint.Flatten();
                rotatedDimension.XLine1Point = rotatedDimension.XLine1Point.Flatten();
                rotatedDimension.XLine2Point = rotatedDimension.XLine1Point.Flatten();
            }
            else if (entity is AlignedDimension alignedDimension)
            {
                alignedDimension.TextPosition = alignedDimension.TextPosition.Flatten();
                alignedDimension.DimLinePoint = alignedDimension.DimLinePoint.Flatten();
                alignedDimension.XLine1Point = alignedDimension.XLine1Point.Flatten();
                alignedDimension.XLine2Point = alignedDimension.XLine1Point.Flatten();
            }
            /*

                                else if (id_platf.ObjectClass.Name == "AcDbRotatedDimension")
            {//Размер повернутый
                RotatedDimension dim = (RotatedDimension)ent;

                //Проверяем, имеют ли задающие точки размера ненулевую координату Z
                if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.DimLinePoint.Z != 0 || dim.TextPosition.Z != 0)
                {
                    dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                    dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                    dim.DimLinePoint = new Point3d(dim.DimLinePoint.X, dim.DimLinePoint.Y, 0);
                    dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                    //ed.WriteMessage("DEBUG: Преобразован объект: повернутый размер");

                    result = true;
                    dimcount++;
                };
            }
            else if (id_platf.ObjectClass.Name == "AcDbPoint3AngularDimension")
            {//Угловой размер по 3 точкам
                Point3AngularDimension dim = (Point3AngularDimension)ent;
                if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.CenterPoint.Z != 0 || dim.TextPosition.Z != 0)
                {

                    dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                    dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                    dim.CenterPoint = new Point3d(dim.CenterPoint.X, dim.CenterPoint.Y, 0);

                    dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                    //ed.WriteMessage("DEBUG: Преобразован объект: Угловой размер по трем точкам");

                    result = true;
                    dimcount++;
                };
            }
            else if (id_platf.ObjectClass.Name == "AcDbLineAngularDimension2")
            {//Еще угловой размер по точкам
                LineAngularDimension2 dim = (LineAngularDimension2)ent;

                if (dim.XLine1Start.Z != 0 || dim.XLine1End.Z != 0 || dim.XLine1Start.Z != 0 || dim.XLine2End.Z != 0 || dim.ArcPoint.Z != 0 || dim.TextPosition.Z != 0)
                {

                    dim.XLine1Start = new Point3d(dim.XLine1Start.X, dim.XLine1Start.Y, 0);
                    dim.XLine1End = new Point3d(dim.XLine1End.X, dim.XLine1End.Y, 0);
                    dim.XLine2Start = new Point3d(dim.XLine2Start.X, dim.XLine2Start.Y, 0);
                    dim.XLine2End = new Point3d(dim.XLine2End.X, dim.XLine2End.Y, 0);
                    dim.ArcPoint = new Point3d(dim.ArcPoint.X, dim.ArcPoint.Y, 0);

                    dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                    //ed.WriteMessage("DEBUG: Преобразован объект: Угловой размер по 5 точкам");

                    result = true;
                    dimcount++;
                };
            }
            else if (id_platf.ObjectClass.Name == "AcDbDiametricDimension")
            {  //Размер диаметра окружности
                DiametricDimension dim = (DiametricDimension)ent;

                if (dim.FarChordPoint.Z != 0 || dim.ChordPoint.Z != 0 || dim.TextPosition.Z != 0)
                {
                    dim.FarChordPoint = new Point3d(dim.FarChordPoint.X, dim.FarChordPoint.Y, 0);
                    dim.ChordPoint = new Point3d(dim.ChordPoint.X, dim.ChordPoint.Y, 0);
                    dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                    //ed.WriteMessage("DEBUG: Преобразован объект: Диаметральный размер");

                    result = true;
                    dimcount++;
                };
            }
            else if (id_platf.ObjectClass.Name == "AcDbArcDimension")
            {  //Дуговой размер
                ArcDimension dim = (ArcDimension)ent;

                if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.ArcPoint.Z != 0 || dim.TextPosition.Z != 0)
                {
                    dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                    dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                    dim.ArcPoint = new Point3d(dim.ArcPoint.X, dim.ArcPoint.Y, 0);
                    dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                    //ed.WriteMessage("DEBUG: Преобразован объект: Дуговой размер");

                    result = true;
                    dimcount++;
                };

            }
            else if (id_platf.ObjectClass.Name == "AcDbRadialDimension")
            {  //Радиальный размер
                RadialDimension dim = (RadialDimension)ent;

                if (dim.Center.Z != 0 || dim.ChordPoint.Z != 0 || dim.TextPosition.Z != 0)
                {
                    dim.Center = new Point3d(dim.Center.X, dim.Center.Y, 0);
                    dim.ChordPoint = new Point3d(dim.ChordPoint.X, dim.ChordPoint.Y, 0);
                    dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                    //ed.WriteMessage("DEBUG: Преобразован объект: Радиальный размер");

                    result = true;
                    dimcount++;
                };

            }

            */
            else if (entity is MLeader mLeader)
            {
                //IN PROGRESS DOES NOT WORK ???
                mLeader.TextLocation = mLeader.TextLocation.Flatten();
                for (int LeaderLineCount = 0; LeaderLineCount < mLeader.LeaderLineCount; LeaderLineCount++)
                {
                    mLeader.SetFirstVertex(LeaderLineCount, mLeader.GetFirstVertex(LeaderLineCount).Flatten());
                    mLeader.SetLastVertex(LeaderLineCount, mLeader.GetLastVertex(LeaderLineCount).Flatten());
                    for (int LeaderVerticesCount = 0; LeaderVerticesCount < mLeader.VerticesCount(LeaderLineCount); LeaderVerticesCount++)
                    {
                        Point3d Point = mLeader.GetVertex(LeaderLineCount, LeaderVerticesCount);
                        mLeader.SetVertex(LeaderLineCount, LeaderVerticesCount, Point.Flatten());

                        Debug.WriteLine($"Flatten point {mLeader.GetVertex(LeaderLineCount, LeaderVerticesCount)}");
                    }
                }
            }
            else if (entity is Leader ld)
            {  //Выноска Autocad

                if (ld.EndPoint.Z != 0 || ld.StartPoint.Z != 0)
                {
                    ld.EndPoint = new Point3d(ld.EndPoint.X, ld.EndPoint.Y, 0);
                    ld.StartPoint = new Point3d(ld.StartPoint.X, ld.StartPoint.Y, 0);
                };

            }
            else if (entity is Solid)
            {
                //Not supported yet
            }
            else if (entity is Solid3d)
            {
                //Not supported yet
            }
            else
            {
                return false;
            }
            return true;
        }


        public static void AddXData(this Entity ent, string value)
        {
            if (value.Length > 255)
            {
                throw new ArgumentException("La chaine est trop longue");
            }
            AddXData(ent, new TypedValue((int)DxfCode.ExtendedDataAsciiString, value));
        }

        public static void AddXData(this Entity ent, int value)
        {
            AddXData(ent, new TypedValue((int)DxfCode.ExtendedDataInteger16, value));
        }

        public static void RemoveXData(this Entity ent)
        {
            AddXData(ent, null);
        }
        public static List<object> ReadXData(this Entity ent)
        {
            string AppName = Generic.GetExtensionDLLName();
            Database db = Generic.GetDatabase();
            List<object> list = new List<object>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ResultBuffer rb = ent.GetXDataForApplication(AppName);
                if (rb != null)
                {
                    foreach (TypedValue tv in rb.AsArray())
                    {
                        switch ((DxfCode)tv.TypeCode)
                        {
                            case DxfCode.ExtendedDataAsciiString:
                                string asciiStr = (string)tv.Value;
                                list.Add(asciiStr);
                                break;
                            case DxfCode.ExtendedDataInteger16:
                                int int16 = (short)tv.Value;
                                list.Add(int16);
                                break;
                        }
                    }
                }
                tr.Commit();
            }
            return list;
        }

        public static void AddXData(this Entity ent, TypedValue typedValue)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ent.TryUpgradeOpen();

                RegAppTable regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
                string AppName = Generic.GetExtensionDLLName();
                if (!regTable.Has(AppName))
                {
                    regTable.UpgradeOpen();
                    RegAppTableRecord app = new RegAppTableRecord
                    {
                        Name = AppName
                    };
                    regTable.Add(app);
                    tr.AddNewlyCreatedDBObject(app, true);
                }
                //https://help.autodesk.com/view/OARX/2023/ENU/?guid=GUID-A2A628B0-3699-4740-A215-C560E7242F63
                ent.XData = new ResultBuffer(new TypedValue(1001, AppName), typedValue);
                tr.Commit();
            }
        }

        public static void TransformToFitBoundingBox(this Entity ent, Extents3d FitBoundingBox)
        {
            var entExtent = ent.GetExtents();
            var entExtentSize = entExtent.Size();
            var FitBoundingBoxSize = FitBoundingBox.Size();

            double scaleX = entExtentSize.Width / FitBoundingBoxSize.Width;
            double scaleY = entExtentSize.Height / FitBoundingBoxSize.Height;
            ent.TransformBy(Matrix3d.Scaling(Math.Min(scaleX, scaleY), entExtent.MinPoint));
        }
    }
}
