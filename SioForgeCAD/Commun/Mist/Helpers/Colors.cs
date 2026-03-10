using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Commun.Mist
{
    public static class Colors
    {
        public static (double R, double G, double B) SetBrightness(double BrightnessFactor, double R, double G, double B)
        {
            //BrightnessFactor need to be between -1 and 1
            double SetBrignessChannel(double Channel)
            {
                double ScaledValue = Channel * (1 + BrightnessFactor);
                return ScaledValue.Clamp(0, 255);
            }

            return (SetBrignessChannel(R), SetBrignessChannel(G), SetBrignessChannel(B));
        }

        public static (double R, double G, double B) SetContrast(double ContrastFactor, double R, double G, double B)
        {
            //BrightnessFactor need to be between -1 and 1
            double ContrastLevel = Math.Pow((1.0 + ContrastFactor) / 1.0, 2);

            double SetContrastChannel(double Channel)
            {
                double ScaledValue = ((((Channel / 255.0) - 0.5) * ContrastLevel) + 0.5) * 255.0;
                return ScaledValue.Clamp(0, 255);
            }

            return (SetContrastChannel(R), SetContrastChannel(G), SetContrastChannel(B));
        }


        public static Autodesk.AutoCAD.Colors.Color GetTransGraphicsColor(Entity _, bool IsPrimary)
        {
            return Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, !IsPrimary ? (short)Settings.TransientSecondaryColorIndex : (short)Settings.TransientPrimaryColorIndex);
        }


        public static Color FromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = (hue / 60) - Math.Floor(hue / 60);

            value *= 255;
            byte v = (byte)value;
            byte p = (byte)(value * (1 - saturation));
            byte q = (byte)(value * (1 - (f * saturation)));
            byte t = (byte)(value * (1 - ((1 - f) * saturation)));

            switch (hi)
            {
                case 0:
                    return Color.FromRgb(v, t, p);
                case 1:
                    return Color.FromRgb(q, v, p);
                case 2:
                    return Color.FromRgb(p, v, t);
                case 3:
                    return Color.FromRgb(p, q, v);
                case 4:
                    return Color.FromRgb(t, p, v);
                default:
                    return Color.FromRgb(v, p, q);
            }
        }

    }
}
