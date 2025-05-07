using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;

namespace SioForgeCAD.Functions
{
    public static class REMOVEALLPROXIES
    {
        public static void SearchAndEraseProxy()
        {
            Database db = Generic.GetDatabase();
            EraseProxiesObjects(db);
        }

        private static void EraseProxiesObjects(Database db)
        {
            RXClass zombieEntity = RXObject.GetClass(typeof(ProxyEntity));
            RXClass zombieObject = RXObject.GetClass(typeof(ProxyObject));
            using (OpenCloseTransaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                for (long l = db.BlockTableId.Handle.Value; l < db.Handseed.Value; l++)
                {
                    if (!db.TryGetObjectId(new Handle(l), out ObjectId id))
                    {
                        continue;
                    }

                    if (id.ObjectClass.IsDerivedFrom(zombieObject) && !id.IsErased)
                    {
                        try
                        {
                            using (DBObject proxy = tr.GetObject(id, OpenMode.ForWrite))   //id.Open(OpenMode.ForWrite))
                            {
                                proxy.Erase();
                            }
                        }
                        catch
                        {
                            using (DBDictionary newDict = new DBDictionary())
                            using (DBObject proxy = tr.GetObject(id, OpenMode.ForWrite))
                            {
                                try
                                {
                                    proxy.HandOverTo(newDict, true, true);
                                }
                                catch { }
                            }
                        }
                    }
                    else if (id.ObjectClass.IsDerivedFrom(zombieEntity) && !id.IsErased)
                    {
                        try
                        {
                            using (DBObject proxy = tr.GetObject(id, OpenMode.ForWrite))
                            {
                                proxy.Erase();
                            }
                        }
                        catch { }
                    }
                }

                tr.Commit();
            }
        }
    }
}
