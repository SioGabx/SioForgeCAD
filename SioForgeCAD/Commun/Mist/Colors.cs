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
    }
}
