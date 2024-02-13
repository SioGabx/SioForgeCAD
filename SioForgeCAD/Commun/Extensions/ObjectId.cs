using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
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
            var db = Generic.GetDatabase();
            if (db.TransactionManager.NumberOfActiveTransactions == 0)
            {
                using (db.TransactionManager.StartTransaction())
                {
                    return db.TransactionManager.GetObject(objectId, openMode);
                }
            }
            return db.TransactionManager.GetObject(objectId, openMode);

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
