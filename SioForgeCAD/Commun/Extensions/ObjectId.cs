using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using System.Linq;

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
            if (objectId.IsNull)
            {
                return null;
            }
            var db = Generic.GetDatabase();
            return db.TransactionManager.GetObject(objectId, openMode, false, true);
        }

        public static List<ObjectId> GetObjectIds(this IEnumerable<DBObject> dBObjects)
        {
            List<ObjectId> ObjectIds = new List<ObjectId>();
            foreach (DBObject obj in dBObjects)
            {
                ObjectIds.Add(obj.ObjectId);
            }
            return ObjectIds;
        }

        public static DBObject GetNoTransactionDBObject(this ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
        {
            var db = Generic.GetDatabase();
            if (db.TransactionManager.NumberOfActiveTransactions == 0)
            {
                using (db.TransactionManager.StartTransaction())
                {
                    return objectId.GetDBObject(openMode);
                }
            }
            else
            {
                return objectId.GetDBObject();
            }
        }

        public static DBObjectCollection Explode(this IEnumerable<ObjectId> ObjectsToExplode, bool EraseOriginal = true)
        {
            DBObjectCollection objs = new DBObjectCollection();

            // Loop through the selected objects
            foreach (ObjectId ObjectToExplode in ObjectsToExplode)
            {
                Entity ent = ObjectToExplode.GetEntity();
                // Explode the object into our collection
                ent.Explode(objs);
                if (EraseOriginal)
                {
                    ent.UpgradeOpen();
                    ent.Erase();
                }
            }
            return objs;
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

        public static ObjectIdCollection ToObjectIdCollection(this IEnumerable<ObjectId> objectId)
        {
            var NewObjectIdCollection = new ObjectIdCollection();
            foreach (ObjectId ObjectId in objectId)
            {
                NewObjectIdCollection.Add(ObjectId);
            }
            return NewObjectIdCollection;
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
            Document doc = Generic.GetDocument();
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                if (ObjectToErase.IsErased)
                {
                    return;
                }
                Entity ent = (Entity)tr.GetObject(ObjectToErase, OpenMode.ForWrite);
                if (!ent.IsErased)
                {
                    ent.Erase(true);
                }
                tr.Commit();
            }
        }

        public static void Join(this ObjectIdCollection A, ObjectIdCollection B)
        {
            foreach (ObjectId ent in B)
            {
                if (!A.Contains(ent))
                {
                    A.Add(ent);
                }
            }
        }
        public static void Add(this ObjectIdCollection col, ObjectId[] ids)
        {
            foreach (var id in ids)
            {
                if (!col.Contains(id))
                    col.Add(id);
            }
        }

        public static Hatch HatchObject(this ObjectId Obj, string Layer)
        {
            ObjectIdCollection acObjIdColl = new ObjectIdCollection
            {
                Obj
            };
            Hatch acHatch = new Hatch();

            acHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            acHatch.Associative = true;
            acHatch.Layer = Layer;
            acHatch.ColorIndex = 256;
            acHatch.Transparency = new Transparency(TransparencyMethod.ByBlock);
            acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
            acHatch.EvaluateHatch(true);
            return acHatch;
        }
    }
}
