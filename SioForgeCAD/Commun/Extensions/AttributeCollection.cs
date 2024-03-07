using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class AttributeCollectionExtensions
    {
        /// <summary>
        /// Opens the attribute references in the given open mode.
        /// </summary>
        /// <param name="source">Attribute collection.</param>
        /// <param name="mode">Open mode to obtain in.</param>
        /// <param name="openErased">Value indicating whether to obtain erased objects.</param>
        /// <param name="forceOpenOnLockedLayers">Value indicating if locked layers should be opened.</param>
        /// <returns>The sequence of attribute references.</returns>
        public static IEnumerable<AttributeReference> GetObjects(this AttributeCollection source, OpenMode mode = OpenMode.ForRead, bool openErased = false, bool forceOpenOnLockedLayers = false)
        {
            Transaction tr = Generic.GetDatabase().TransactionManager.TopTransaction;
            if (source.Count > 0)
            {
                foreach (ObjectId id in source)
                {
                    if (!id.IsErased || openErased)
                    {
                        yield return (AttributeReference)tr.GetObject(id, mode, openErased, forceOpenOnLockedLayers);
                    }
                }
            }
        }
    }
}
