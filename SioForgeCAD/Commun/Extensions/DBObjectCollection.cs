using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DBObjectCollectionExtensions
    {
        public static DBObjectCollection AddRange(this DBObjectCollection A, DBObjectCollection B)
        {
            foreach (DBObject ent in B)
            {
                if (!A.Contains(ent))
                {
                    A.Add(ent);
                }
            }
            return A;
        }

        public static void DeepDispose(this DBObjectCollection collection)
        {
            foreach (DBObject item in collection)
            {
                if (item?.IsDisposed == false)
                {
                    item.Dispose();
                }
            }
            collection.Dispose();
        }

        public static DBObject[] ToArray(this DBObjectCollection collection)
        {
            DBObject[] list = new DBObject[collection.Count];
            for (int i = 0; i < collection.Count; i++)
            {
                list.SetValue(collection[i], i);
            }
            return list;
        }
    }
}
