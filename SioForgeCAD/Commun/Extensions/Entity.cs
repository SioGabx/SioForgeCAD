using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class EntityExtensions
    {
        /// <summary>
        /// Intersects entities.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entOther">The other entity.</param>
        /// <param name="intersectType">The type.</param>
        /// <param name="points">The intersection points output.</param>
        internal static void IntersectWith3264bit(this Entity entity, Entity entOther, Intersect intersectType, Point3dCollection points)
        {
            // NOTE: Use runtime binding for difference between 32- and 64-bit APIs.
            var methodInfo = typeof(Entity).GetMethod("IntersectWith",
                new Type[] { typeof(Entity), typeof(Intersect), typeof(Point3dCollection), typeof(long), typeof(long) });
            methodInfo.Invoke(entity, new object[] { entOther, intersectType, points, 0, 0 });
        }
    }
}
