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

        public static Autodesk.AutoCAD.Colors.Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsPrimary)
        {
            if (!IsPrimary)
            {
                const byte Alpha = (255 * (100 - 50) / 100);
                Drawable.Transparency = new Autodesk.AutoCAD.Colors.Transparency(Alpha);
            }
            return Drawable.Transparency;
        }

        // Convertit une couleur RGB en Lab
        public static (double L, double A, double B) RgbToLab(byte r, byte g, byte b)
        {
            // 1. Normalisation
            double R = r / 255.0, G = g / 255.0, B = b / 255.0;

            // 2. Correction gamma
            R = (R > 0.04045) ? Math.Pow((R + 0.055) / 1.055, 2.4) : R / 12.92;
            G = (G > 0.04045) ? Math.Pow((G + 0.055) / 1.055, 2.4) : G / 12.92;
            B = (B > 0.04045) ? Math.Pow((B + 0.055) / 1.055, 2.4) : B / 12.92;

            // 3. RGB → XYZ
            double X = ((R * 0.4124) + (G * 0.3576) + (B * 0.1805)) / 0.95047;
            double Y = ((R * 0.2126) + (G * 0.7152) + (B * 0.0722)) / 1.00000;
            double Z = ((R * 0.0193) + (G * 0.1192) + (B * 0.9505)) / 1.08883;

            // 4. XYZ → Lab
            Func<double, double> f = t => (t > 0.008856) ? Math.Pow(t, 1.0 / 3) : (7.787 * t) + (16.0 / 116);
            double fx = f(X), fy = f(Y), fz = f(Z);

            double L = (116 * fy) - 16;
            double A = 500 * (fx - fy);
            double Bval = 200 * (fy - fz);

            return (L, A, Bval);
        }

        // Calcul de la différence perceptuelle entre deux couleurs (Delta E)
        public static double DeltaE((double L, double A, double B) lab1, (double L, double A, double B) lab2)
        {
            double dL = lab1.L - lab2.L;
            double dA = lab1.A - lab2.A;
            double dB = lab1.B - lab2.B;
            return Math.Sqrt((dL * dL) + (dA * dA) + (dB * dB));
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
