using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
