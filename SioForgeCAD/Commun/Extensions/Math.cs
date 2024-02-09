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
            //For A = 100, B = 200, Percentage = 25, this return 125
            // Ensure 'a' is the smaller value and 'b' is the larger one
            //if (a > b)
            //{
            //    (b, a) = (a, b);
            //}

            // Calculate the intermediate percentage
            double intermediatePercentage = a + (percentage * (b - a) / 100);

            return intermediatePercentage;
        }



    }
}
