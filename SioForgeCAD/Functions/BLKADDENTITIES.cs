using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKADDENTITIES
    {
        public static void Add()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (!ed.GetBlocks(out ObjectId[] ObjectIds, null, true, true))
            {
                return;
            }

            var Selection = ed.GetSelectionRedraw("Selectionnez des entités à inclure au bloc", true, false);
            if (Selection.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId BlockRefObjId = ObjectIds.First();
                BlockReference BlockRef = BlockRefObjId.GetDBObject(OpenMode.ForWrite) as BlockReference;

                var SelectedIds = Selection.Value.GetObjectIds();
                if (BlockRef.IsXref())
                {
                    AddEntitiesToXref(BlockRefObjId, BlockRef, SelectedIds, tr, db);
                }
                else
                {
                    // Modifier le bloc dans le dessin courant
                    AddEntitiesToBlock(BlockRef, SelectedIds, tr);
                    tr.Commit();
                }

                BlockRef.RegenAllBlkDefinition();
            }
        }

        private static void AddEntitiesToBlock(BlockReference BlockRef, ObjectId[] selectedIds, Transaction tr)
        {
            BlockTableRecord BlockDef = BlockRef.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
            Matrix3d blockTransform = BlockRef.BlockTransform;
            Matrix3d inverseTransform = blockTransform.Inverse();

            foreach (ObjectId entId in selectedIds)
            {
                if (entId == BlockRef.ObjectId) { continue; }
                if (!(entId.GetDBObject(OpenMode.ForWrite) is Entity SelectedEnt)) { continue; }

                Entity SelectedEntClone = SelectedEnt.Clone() as Entity;
                SelectedEntClone.TransformBy(inverseTransform);
                BlockDef.AppendEntity(SelectedEntClone);
                tr.AddNewlyCreatedDBObject(SelectedEntClone, true);
                SelectedEnt.Erase();
            }
        }

        private static void AddEntitiesToXref(ObjectId BlockRefObjId, BlockReference XrefRef, ObjectId[] selectedIds, Transaction tr, Database db)
        {
            var XrefBtr = (XrefRef?.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord);
            var Xref = XrefBtr.GetXrefDatabase(false);

            if (Xref is null || string.IsNullOrEmpty(Xref.Filename))
            {
                return;
            }

            if (Files.IsFileLockedOrReadOnly(Xref.Filename))
            {
                Generic.WriteMessage("Impossible de modifier la XREF. Elle est peut-être ouverte dans l'éditeur ou en lecture seule.");
            }
            else
            { //https://www.keanw.com/2015/01/modifying-the-contents-of-an-autocad-xref-using-net.html
              //RestoreOriginalXrefSymbols(Xref);
                var SelectedIds = selectedIds.ToObjectIdCollection();
                SelectedIds.Remove(BlockRefObjId);
                using (var xf = XrefFileLock.LockFile(Xref.XrefBlockId))
                {
                    Matrix3d inverseTransform = XrefRef.BlockTransform.Inverse();
                    foreach (ObjectId entId in SelectedIds)
                    {
                        if (!(entId.GetDBObject(OpenMode.ForWrite) is Entity SelectedEnt))
                        {
                            continue;
                        }

                        SelectedEnt.TransformBy(inverseTransform);
                    }
                    using (Transaction xrefTr = Xref.TransactionManager.StartTransaction())
                    {
                        BlockTable blockTable = xrefTr.GetObject(Xref.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord xrefBlockDef = xrefTr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                        IdMapping acIdMap = new IdMapping();
                        Xref.RestoreOriginalXrefSymbols();
                        Xref.WblockCloneObjects(SelectedIds, xrefBlockDef.ObjectId, acIdMap, DuplicateRecordCloning.Ignore, false);
                        Xref.RestoreForwardingXrefSymbols();

                        //MergeDuplicateLayers(xrefTr, xrefBlockDef.Database);
                        xrefTr.Commit();
                        foreach (ObjectId entId in SelectedIds)
                        {
                            entId.EraseObject();
                        }

                        tr.Commit();
                    }
                }
            }
        }
    }
}