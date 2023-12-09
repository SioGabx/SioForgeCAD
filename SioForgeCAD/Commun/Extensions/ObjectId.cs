using Autodesk.AutoCAD.DatabaseServices;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Documents;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using System;
using System.IO;
using System.Reflection;

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

        public static DBObjectCollection ToDBObjectCollection(this List<Entity> entities)
        {
            var NewDBObjectCollection = new DBObjectCollection();
            foreach (var entity in entities)
            {
                NewDBObjectCollection.Add(entity);
            }
            return NewDBObjectCollection;
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

    }
}
