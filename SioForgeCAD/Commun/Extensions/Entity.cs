using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Diagnostics;

namespace SioForgeCAD.Commun.Extensions
{
    public static class EntityExtensions
    {

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
            }

            //Default for each Entities
            Target.Color = Origin.Color;
            Target.Layer = Origin.Layer;
            Target.Linetype = Origin.Linetype;
            Target.LinetypeScale = Origin.LinetypeScale;
            Target.LineWeight = Origin.LineWeight;
            Target.Material = Origin.Material;
            Target.OwnerId = Origin.OwnerId;
            Target.ReceiveShadows = Origin.ReceiveShadows;
            Target.Transparency = Origin.Transparency;
            Target.Visible = Origin.Visible;
            Target.XData = Origin.XData;
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

    }
}
