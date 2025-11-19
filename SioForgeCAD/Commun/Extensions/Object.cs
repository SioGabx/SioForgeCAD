using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows.Data;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ObjectExtensions
    {
        public static bool TryGetDoubleValue(this object obj, out double value)
        {
            if (obj is double)
            {
                value = (double)obj;
            }
            else if (obj is float)
            {
                value = (float)obj;
            }
            else if (obj is int)
            {
                value = (int)obj;
            }
            else if (obj is short)
            {
                value = (short)obj;
            }
            else
            {
                value = 0;
                return false;
            }
            return true;
        }

        public static ObjectId[] GetObjectIds(this object obj)
        {
            if (obj is SelectionSet selectionSet)
            {
                return selectionSet.GetObjectIds();
            }
            return System.Array.Empty<ObjectId>();
        }

        public static IEnumerable<ObjectId> GetSelectionSet(this object obj)
        {
            if (obj is SelectionSet selectionSet)
            {
                foreach (ObjectId item in selectionSet)
                {
                    yield return item;
                }
            }
        }

    }
}
