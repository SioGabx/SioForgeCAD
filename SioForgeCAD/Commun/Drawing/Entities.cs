﻿using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Entities
    {
        public static List<ObjectId> AddToDrawing(this IEnumerable<Entity> entities, int ColorIndex = 7, bool Clone = false)
        {
            List<ObjectId> objs = new List<ObjectId>();
            foreach (var entity in entities)
            {
                Entity ent = entity;
                if (Clone)
                {
                    ent = (Entity)ent.Clone();
                }
                ent.ColorIndex = ColorIndex;
                objs.Add(ent.AddToDrawing());
            }
            return objs;
        }

        public static List<ObjectId> AddToDrawing(this DBObjectCollection entities, int ColorIndex = 7, bool Clone = false)
        {
            return entities.Cast<Entity>().AddToDrawing(ColorIndex, Clone);
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