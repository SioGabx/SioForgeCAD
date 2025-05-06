using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class REMOVEALLPROXIES
    {
        public static void searchAndEraseProxy()
        {
            
            _NODEntriesForErase = new ObjectIdCollection();
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            EraseProxies(db);
            using (Transaction trans = db.TransactionManager.StartOpenCloseTransaction())
            {
                // open block table
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                // for each block table record
                // (mspace, pspace, other blocks)
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = trans.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    // for each entity on this block table record
                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;
                        if ((ent.IsAProxy))
                        {
                            ent.UpgradeOpen();
                            ent.Erase();
                            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(string.Format("\nProxy entity found: {0}", ent.GetType().Name));
                        }
                    }
                }

                // now search for NOD proxy entries
                DBDictionary nod = trans.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
                searchSubDictionary(trans, nod);
                RemoveProxiesFromDictionary(db.NamedObjectsDictionaryId, trans);

                // the HandOverTo operation must
                // be perfomed outside transactions
                foreach (ObjectId nodID in _NODEntriesForErase)
                {
                    //replace with an empty Dic Entry
                    DBDictionary newNODEntry = new DBDictionary();
                    DBObject NODEntry = trans.GetObject(nodID, OpenMode.ForRead) as DBObject;
                    try
                    {
                        NODEntry.HandOverTo(newNODEntry, true, true);
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex) { }
                    newNODEntry.Dispose();
                }
              
                trans.Commit();
            }

            
        }

        private static ObjectIdCollection _NODEntriesForErase;

        private static void searchSubDictionary(Transaction trans, DBDictionary dic)
        {
            foreach (DBDictionaryEntry dicEntry in dic)
            {
                DBObject subDicObj = trans.GetObject(dicEntry.Value, OpenMode.ForRead);
                if (subDicObj.IsAProxy)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        string.Format("\nProxy found at key {0}", dicEntry.Key));
                    subDicObj.UpgradeOpen();

                    // for several Proxy Entities the
                    // Erase is not allowed withyou the
                    // Object Enabler throw an exception,
                    // just treat
                    try { subDicObj.Erase(); continue; }
                    catch
                    {
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(" | Erase not allowed");
                        //try again latter
                        _NODEntriesForErase.Add(subDicObj.ObjectId);
                    }
                }
                else
                {
                    //if this key is not proxy, let go into sub keys
                    DBDictionary subDic = subDicObj as DBDictionary;
                    if (subDic != null)
                    {
                        searchSubDictionary(trans, subDic);
                    }

                }

            }

        }




















        private static void EraseProxies(Database db)
        {
            RXClass zombieEntity = RXClass.GetClass(typeof(ProxyEntity));
            RXClass zombieObject = RXClass.GetClass(typeof(ProxyObject));
            ObjectId id;
            for (long l = db.BlockTableId.Handle.Value; l < db.Handseed.Value; l++)
            {
                if (!db.TryGetObjectId(new Handle(l), out id))
                    continue;
                if (id.ObjectClass.IsDerivedFrom(zombieObject) && !id.IsErased)
                {
                    try
                    {
                        using (DBObject proxy = id.Open(OpenMode.ForWrite))
                        {
                            proxy.Erase();
                        }
                    }
                    catch
                    {
                        using (DBDictionary newDict = new DBDictionary())
                        using (DBObject proxy = id.Open(OpenMode.ForWrite))
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
                        using (DBObject proxy = id.Open(OpenMode.ForWrite))
                        {
                            proxy.Erase();
                        }
                    }
                    catch { }
                }
            }
        }
















        public static void RemoveEntry1(

      DBDictionary dict, ObjectId id, Transaction tr)

        {

            ProxyObject obj =

              (ProxyObject)tr.GetObject(id, OpenMode.ForRead);



            // If you want to check what exact proxy it is

            if (obj.OriginalClassName != "ProxyToRemove")

                return;



            dict.Remove(id);

        }



        public static void RemoveEntry2(

          DBDictionary dict, ObjectId id, Transaction tr)

        {

            ProxyObject obj =

              (ProxyObject)tr.GetObject(id, OpenMode.ForRead);



            // If you want to check what exact proxy it is

            if (obj.OriginalClassName != "ProxyToRemove")

                return;



            obj.UpgradeOpen();



            using (DBObject newObj = new Xrecord())

            {

                obj.HandOverTo(newObj, false, false);

                newObj.Erase();

            }

        }



        public static void RemoveProxiesFromDictionary( ObjectId dictId, Transaction tr)

        {

            using (ObjectIdCollection ids = new ObjectIdCollection())

            {

                DBDictionary dict =

                  (DBDictionary)tr.GetObject(dictId, OpenMode.ForRead);



                foreach (DBDictionaryEntry entry in dict)

                {

                    RXClass c1 = entry.Value.ObjectClass;

                    RXClass c2 = RXClass.GetClass(typeof(ProxyObject));



                    if (entry.Value.ObjectClass.Name == "AcDbZombieObject")

                        ids.Add(entry.Value);

                    else if (entry.Value.ObjectClass ==

                      RXClass.GetClass(typeof(DBDictionary)))

                        RemoveProxiesFromDictionary(entry.Value, tr);

                }



                if (ids.Count > 0)

                {

                    dict.UpgradeOpen();



                    foreach (ObjectId id in ids) { 

                        RemoveEntry2(dict, id, tr);
                        RemoveEntry1(dict, id, tr);
                    }

                }

            }

        }
















    }
}
