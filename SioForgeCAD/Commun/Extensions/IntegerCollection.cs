using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class IntegerCollectionExtensions
    {
        public static IntegerCollection ToIntegerCollection(this IEnumerable<int> list)
        {
            return new IntegerCollection(list.ToArray());
        }



    }
}
