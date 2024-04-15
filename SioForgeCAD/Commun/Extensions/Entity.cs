using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
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
                NewList.Add((T)(item as Entity).Clone());
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
                ObjectToErase.UpgradeOpen();
                if (!ObjectToErase.IsErased)
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
                        TargetHatch.SetGradient(OriginHatch.GradientType, OriginHatch.GradientName);
                        TargetHatch.GradientOneColorMode = OriginHatch.GradientOneColorMode;
                        TargetHatch.GradientShift = OriginHatch.GradientShift;
                        TargetHatch.GradientAngle = OriginHatch.GradientAngle;
                    }
                    if (OriginHatch.IsHatch)
                    {
                        TargetHatch.SetHatchPattern(OriginHatch.PatternType, OriginHatch.PatternName);
                        TargetHatch.HatchStyle = OriginHatch.HatchStyle;
                        TargetHatch.PatternSpace = OriginHatch.PatternSpace;
                        TargetHatch.PatternAngle = OriginHatch.PatternAngle;
                        TargetHatch.PatternDouble = OriginHatch.PatternDouble;
                    }
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

            Target.LayerId = Origin.LayerId;
            Target.Linetype = Origin.Linetype;
            Target.LinetypeScale = Origin.LinetypeScale;
            Target.LineWeight = Origin.LineWeight;
            Target.Material = Origin.Material;
            Target.OwnerId = Origin.OwnerId;
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

        public static double TryGetArea(this Entity ent)
        {
            try
            {
                if (ent is Polyline) { return ((Polyline)ent).Area; }
                if (ent is Hatch) { return ((Hatch)ent).Area; }
                if (ent is Circle) { return ((Circle)ent).Area; }
                if (ent is Ellipse) { return ((Ellipse)ent).Area; }
                if (ent is Region) { return ((Region)ent).Area; }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return 0;
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
                    TypedValue[] rvArr = rb.AsArray();
                    foreach (TypedValue tv in rvArr)
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



        public static void AddXData(Entity ent, TypedValue typedValue)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ent.UpgradeOpen();

                RegAppTable regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
                string AppName = Generic.GetExtensionDLLName();
                if (!regTable.Has(AppName))
                {
                    regTable.UpgradeOpen();
                    RegAppTableRecord app = new RegAppTableRecord();
                    app.Name = AppName;
                    regTable.Add(app);
                    tr.AddNewlyCreatedDBObject(app, true);
                }
                //https://help.autodesk.com/view/OARX/2023/ENU/?guid=GUID-A2A628B0-3699-4740-A215-C560E7242F63
                if (typedValue == null)
                {
                    ent.XData = new ResultBuffer(new TypedValue(1001, AppName));
                }
                else
                {
                    ent.XData = new ResultBuffer(new TypedValue(1001, AppName), typedValue);
                }

                tr.Commit();
            }
        }






    }
}
