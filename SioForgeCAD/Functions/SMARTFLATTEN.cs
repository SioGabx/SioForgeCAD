using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class SMARTFLATTEN
    {
        public static void FlattenAll()
        {
            var db = Generic.GetDatabase();
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var BlkTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in BlkTable)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr.IsLayout)
                    {
                        continue;
                    }

                    Debug.WriteLine($"Inspecting block: {btr.Name}");

                    foreach (ObjectId objId in btr)
                    {
                        ids.Add(objId);
                    }
                }

                BlockTableRecord modelSpace = tr.GetObject(BlkTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId objId in modelSpace)
                {
                    ids.Add(objId);
                }

                FlatSCU();
                FlatObjects(ids.ToArray());
                tr.Commit();
            }
        }

        public static void Flatten()
        {
            var ed = Generic.GetEditor();
            var EntitiesSelection = ed.GetSelectionRedraw(Options: new string[] { "SCU" });
            if (EntitiesSelection.Status == PromptStatus.Keyword) { FlatSCU(); }
            if (EntitiesSelection.Status != PromptStatus.OK) { return; }

            FlatObjects(EntitiesSelection.Value.GetObjectIds());

        }

        private static void FlatSCU()
        {
            //Flaten USC

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                UcsTable ucsTable = (UcsTable)db.UcsTableId.GetDBObject();
                foreach (ObjectId ucsId in ucsTable)
                {
                    UcsTableRecord ucs = (UcsTableRecord)ucsId.GetDBObject(OpenMode.ForWrite);
                    if (ucs.Origin.Z != 0)
                    {
                        Generic.WriteMessage($"Le SCU \"{ucs.Name}\" à été aplati. {ucs.Origin.Z} -> 0");
                    }
                    ucs.Origin = ucs.Origin.Flatten();
                }
                tr.Commit();
            }
        }

        private static void FlatObjects(ObjectId[] objectIds)
        {
            Database db = Generic.GetDatabase();
            using (LongOperationProcess LongOperation = new LongOperationProcess("Flattening..."))
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LongOperation.SetTotalOperations(objectIds.Length);
                for (int i = 0; i < objectIds.Length; i++)
                {
                    if (LongOperation.IsCanceled) { return; }
                    LongOperation.UpdateProgress();

                    ObjectId entityObjectId = objectIds[i];
                    var DbObjEnt = tr.GetObject(entityObjectId, OpenMode.ForWrite, true, true);
                    if (!(DbObjEnt is Entity entity))
                    {
                        DbObjEnt.DowngradeOpen();
                        continue;
                    }

                    if (!entity.Flatten())
                    {
                        Debug.WriteLine($"Entité non traitée : \"{entity.GetType()}\"");
                    }
                }
                tr.Commit();
            }
        }
    }
}
