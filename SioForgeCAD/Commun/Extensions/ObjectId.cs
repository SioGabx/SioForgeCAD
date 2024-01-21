using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using System.Linq;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    static class ObjectIdExtensions
    {
        public static Entity GetEntity(this ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
        {
            Entity ObjectEntity = (Entity)objectId.GetDBObject(openMode);
            return ObjectEntity;
        }

        public static DBObject GetDBObject(this ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            DBObject ObjectEntity = db.TransactionManager.GetObject(objectId, openMode);
            return ObjectEntity;
        }

        public static DBObjectCollection ToDBObjectCollection(this IEnumerable<Entity> entities)
        {
            return entities.Cast<DBObject>().ToDBObjectCollection();
        }
        public static DBObjectCollection ToDBObjectCollection(this SelectionSet entities)
        {
            var ObjectIdsCollection = entities.GetObjectIds();
            var NewDBObjectCollection = new DBObjectCollection();
            foreach (var ObjectId in ObjectIdsCollection)
            {
                NewDBObjectCollection.Add(ObjectId.GetDBObject());
            }
            return NewDBObjectCollection;
        }

        public static DBObjectCollection ToDBObjectCollection(this IEnumerable<DBObject> entities)
        {
            var NewDBObjectCollection = new DBObjectCollection();
            foreach (var entity in entities)
            {
                NewDBObjectCollection.Add(entity);
            }
            return NewDBObjectCollection;
        }

        public static List<DBObject> ToList(this DBObjectCollection entities)
        {
            List<DBObject> list = new List<DBObject>();
            foreach (var ent in entities)
            {
                list.Add(ent as DBObject);
            }
            return list;
        }

        public static void EraseObject(this ObjectId ObjectToErase)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(ObjectToErase, OpenMode.ForWrite);
                if (!ent.IsErased)
                {
                    ent.Erase(true);
                }
                tr.Commit();
            }
        }


        public static ObjectIdCollection Join(this ObjectIdCollection A, ObjectIdCollection B)
        {
            foreach(ObjectId ent in B)
            {
                A.Add(ent);
            }
            return A;
        }

    }
}
