using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
