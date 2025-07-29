using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Entities
    {
        public static List<ObjectId> AddToDrawing(this IEnumerable<Entity> entities, int? ColorIndex = null, bool Clone = false)
        {
            List<ObjectId> objs = new List<ObjectId>();
            foreach (var entity in entities)
            {
                Entity ent = entity;
                if (Clone)
                {
                    ent = (Entity)ent.Clone();
                }
                if (ColorIndex != null)
                {
                    ent.ColorIndex = (int)ColorIndex;
                }
                objs.Add(ent.AddToDrawing());
            }
            return objs;
        }

        public static List<ObjectId> AddToDrawing(this DBObjectCollection entities, int? ColorIndex = null, bool Clone = false)
        {
            return entities.Cast<Entity>().AddToDrawing(ColorIndex, Clone);
        }

        public static ObjectId AddToDrawing(this Entity entity, int? ColorIndex = null, bool Clone = false)
        {
            var db = Generic.GetDatabase();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord acBlkTblRec = Generic.GetCurrentSpaceBlockTableRecord(acTrans);

                // Check if the entity is already in the database
                if (entity?.IsErased != false)
                {
                    acTrans.Abort();
                    return ObjectId.Null;
                }
                if (Clone)
                {
                    entity = entity.Clone() as Entity;
                }

                if (ColorIndex != null)
                {
                    entity.ColorIndex = (int)ColorIndex;
                }
                try
                {
                    acBlkTblRec.AppendEntity(entity);
                    acTrans.AddNewlyCreatedDBObject(entity, true);
                    acTrans.Commit();
                    return entity.ObjectId;
                }
                catch
                {
                    return ObjectId.Null;
                }
            }
        }

        public static ObjectId ReplaceInDrawing(this Entity OriginalEntity, Entity ReplaceEntity)
        {
            var db = Generic.GetDatabase();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ownerBtr = acTrans.GetObject(OriginalEntity.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                if (ReplaceEntity?.IsErased != false || ownerBtr is null)
                {
                    acTrans.Abort();
                    return ObjectId.Null;
                }
                OriginalEntity.TryUpgradeOpen();
                ReplaceEntity.TryUpgradeOpen();

                ownerBtr.AppendEntity(ReplaceEntity);
                acTrans.AddNewlyCreatedDBObject(ReplaceEntity, true);
                OriginalEntity.Erase();
                acTrans.Commit();
                return ReplaceEntity.ObjectId;
            }
        }


        public static ObjectId AddToDrawingCurrentTransaction(this Entity entity)
        {
            var db = Generic.GetDatabase();
            Transaction acTrans = db.TransactionManager.TopTransaction;
            BlockTableRecord acBlkTblRec = Generic.GetCurrentSpaceBlockTableRecord(acTrans);

            var objid = acBlkTblRec.AppendEntity(entity);
            acTrans.AddNewlyCreatedDBObject(entity, true);
            return objid;
        }
    }
}
