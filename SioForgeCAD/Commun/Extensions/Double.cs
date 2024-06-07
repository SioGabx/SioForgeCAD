using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DoubleExtensions
    {
        public static double Clamp(this double value, double MinValue, double MaxValue)
        {
            return Math.Max(Math.Min(value, MaxValue), MinValue);
        }
    }
}
