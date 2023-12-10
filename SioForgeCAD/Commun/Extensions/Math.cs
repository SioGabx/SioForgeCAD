using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class MathExtensions
    {
        public static double RoundToNearestMultiple(this double value, int multiple)
        {
            return Math.Floor(value / multiple) * multiple;
        }
    }
}
