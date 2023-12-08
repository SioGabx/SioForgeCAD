using Autodesk.AutoCAD.DatabaseServices;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    static class ObjectIdExtensions
    {
        public static Entity GetEntity(this ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

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
    }
}
