using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Entities
    {
        public static List<ObjectId> AddToDrawing(this IEnumerable<Entity> entities)
        {
            List<ObjectId> objs = new List<ObjectId>();
            foreach (var entity in entities)
            {
                entity.ColorIndex = 5;
                objs.Add(entity.AddToDrawing());
            }
            return objs;
        }


        public static ObjectId AddToDrawing(this Entity entity)
        {
            var db = Generic.GetDatabase();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Check if the entity is already in the database
                if (entity.IsErased)
                {
                    acTrans.Abort();
                    return ObjectId.Null;
                }
                acBlkTblRec.AppendEntity(entity);
                acTrans.AddNewlyCreatedDBObject(entity, true);
                acTrans.Commit();
                return entity.ObjectId;
            }
        }


        public static ObjectId AddToDrawingCurrentTransaction(this Entity entity)
        {
            var db = Generic.GetDatabase();
            Transaction acTrans = db.TransactionManager.TopTransaction;

            BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            var objid = acBlkTblRec.AppendEntity(entity);
            acTrans.AddNewlyCreatedDBObject(entity, true);
            return objid;

        }
    }
}
