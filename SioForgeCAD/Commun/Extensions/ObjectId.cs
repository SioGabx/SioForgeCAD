using Autodesk.AutoCAD.DatabaseServices;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    static class ObjectIdExtensions
    {
        public static Entity GetEntity(this ObjectId objectId)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            Entity ObjectEntity = (Entity)db.TransactionManager.GetObject(objectId, OpenMode.ForRead);
            return ObjectEntity;
        }
        public static DBObject GetDBObject(this ObjectId objectId)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            DBObject ObjectEntity = (DBObject)db.TransactionManager.GetObject(objectId, OpenMode.ForRead);
            return ObjectEntity;
        }
    }


}
