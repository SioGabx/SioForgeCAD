using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class MathExtensions
    {
        public static double RoundToNearestMultiple(this double value, int multiple)
        {
            return Math.Floor(value / multiple) * multiple;
        }

        public static bool IsBetween(this double value, double min, double max)
        {
            return (value >= min) && (value <= max);
        }

        public static double IntermediatePercentage(this double a, double b, double percentage)
        {
            // Calculate the intermediate percentage
            return (double)(a + (percentage * (b - a) / 100));
        }
    }
}
