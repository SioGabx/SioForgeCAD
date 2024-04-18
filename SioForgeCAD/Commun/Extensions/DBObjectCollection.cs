using Autodesk.AutoCAD.DatabaseServices;

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
    }
}
