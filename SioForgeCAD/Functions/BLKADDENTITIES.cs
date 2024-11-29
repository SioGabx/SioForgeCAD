using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System.IO;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKADDENTITIES
    {
        public static void Add()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (!ed.GetBlocks(out ObjectId[] ObjectIds, true, true))
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
                if (BlockRef.IsXref())
                {
                    var XrefBtr = (BlockRef?.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord);
                    var Xref = XrefBtr.GetXrefDatabase(false);

                    if (Xref is null || string.IsNullOrEmpty(Xref.Filename))
                    {
                        return;
                    }

                    if (Files.IsFileLockedOrReadOnly(new FileInfo(Xref.Filename)))
                    {
                        Generic.WriteMessage("Impossible de modifier la XREF. Elle est peut-être ouverte dans l'éditeur ou en lecture seule.");
                    }
                    else
                    {
                        //https://www.keanw.com/2015/01/modifying-the-contents-of-an-autocad-xref-using-net.html
                        //RestoreOriginalXrefSymbols(Xref);
                        var SelectedIds = Selection.Value.GetObjectIds().ToObjectIdCollection();
                        SelectedIds.Remove(BlockRefObjId);
                        using (var xf = XrefFileLock.LockFile(Xref.XrefBlockId))
                        {
                            Matrix3d inverseTransform = BlockRef.BlockTransform.Inverse();
                            foreach (ObjectId entId in SelectedIds)
                            {
                                Entity SelectedEnt = entId.GetDBObject(OpenMode.ForWrite) as Entity;
                                if (SelectedEnt == null) continue;

                                SelectedEnt.TransformBy(inverseTransform);
                            }
                            using (Transaction xrefTr = Xref.TransactionManager.StartTransaction())
                            {

                                BlockTable blockTable = xrefTr.GetObject(Xref.BlockTableId, OpenMode.ForRead) as BlockTable;
                                BlockTableRecord xrefBlockDef = xrefTr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                                IdMapping acIdMap = new IdMapping();


                                Xref.WblockCloneObjects(SelectedIds, xrefBlockDef.ObjectId, acIdMap, DuplicateRecordCloning.Replace, false);
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
                else
                {
                    // Modifier le bloc dans le dessin courant
                    BlockTableRecord blockDef = BlockRef.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
                    AddEntitiesToBlock(blockDef, BlockRef, Selection.Value.GetObjectIds(), tr);
                    tr.Commit();
                }

                BlockRef.RegenAllBlkDefinition();
            }
        }

        private static void AddEntitiesToBlock(BlockTableRecord BlockDef, BlockReference BlockRef, ObjectId[] selectedIds, Transaction tr)
        {
            Matrix3d blockTransform = BlockRef.BlockTransform;
            Matrix3d inverseTransform = blockTransform.Inverse();

            foreach (ObjectId entId in selectedIds)
            {
                if (entId == BlockRef.ObjectId) { continue; }
                Entity SelectedEnt = entId.GetDBObject(OpenMode.ForWrite) as Entity;
                if (SelectedEnt == null) continue;

                Entity SelectedEntClone = SelectedEnt.Clone() as Entity;
                SelectedEntClone.TransformBy(inverseTransform);
                BlockDef.AppendEntity(SelectedEntClone);
                tr.AddNewlyCreatedDBObject(SelectedEntClone, true);
                SelectedEnt.Erase();
            }
        }
    }
}
