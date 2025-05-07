using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SioForgeCAD.Commun.Mist
{
    public static class DWGDataStorage
    {
        public static void SaveTextToDrawing(Database db, string key, string content)
        {
            using (DocumentLock docLock = Generic.GetDocument().LockDocument())

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                string myDictName = Generic.GetExtensionDLLName();

                DBDictionary myDict;
                if (!nod.Contains(myDictName))
                {
                    nod.UpgradeOpen();
                    myDict = new DBDictionary();
                    nod.SetAt(myDictName, myDict);
                    tr.AddNewlyCreatedDBObject(myDict, true);
                }
                else
                {
                    myDict = (DBDictionary)tr.GetObject(nod.GetAt(myDictName), OpenMode.ForWrite);
                }

                Xrecord record = new Xrecord
                {
                    Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, content))
                };

                if (myDict.Contains(key))
                {
                    myDict.UpgradeOpen();
                    myDict.Remove(key);
                }

                myDict.SetAt(key, record);
                tr.AddNewlyCreatedDBObject(record, true);
                tr.Commit();
            }

        }

        public static string LoadTextFromDrawing(Database db, string key)
        {
            using (DocumentLock docLock = Generic.GetDocument().LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                string myDictName = Generic.GetExtensionDLLName();
                if (!nod.Contains(myDictName)) return null;

                DBDictionary myDict = (DBDictionary)tr.GetObject(nod.GetAt(myDictName), OpenMode.ForRead);
                if (!myDict.Contains(key)) return null;

                Xrecord record = (Xrecord)tr.GetObject(myDict.GetAt(key), OpenMode.ForRead);
                TypedValue[] values = record.Data.AsArray();
                return values.Length > 0 ? values[0].Value.ToString() : null;
            }
        }

        public static void DeleteKey(Database db, string key)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                string myDictName = Generic.GetExtensionDLLName();
                if (!nod.Contains(myDictName)) return;

                DBDictionary myDict = (DBDictionary)tr.GetObject(nod.GetAt(myDictName), OpenMode.ForWrite);
                if (myDict.Contains(key)) myDict.Remove(key);

                tr.Commit();
            }
        }
    }
}
