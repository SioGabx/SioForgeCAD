using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKADDENTITIES
    {

        public static void Add()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!TryGetValidBlockReference(ed, tr, out ObjectId blockRefId, out BlockReference blockRef))
                    {
                        return;
                    }

                    var selResult = ed.GetSelectionRedraw("Selectionnez des entités à inclure au bloc", true, false);

                    if (selResult.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    ObjectId[] selectedIds = selResult.Value.GetObjectIds();

                    if (blockRef.IsXref())
                    {
                        AddEntitiesToXref(blockRefId, blockRef, selectedIds);
                    }
                    else
                    {
                        AddEntitiesToBlock(blockRef, selectedIds);
                    }

                    blockRef.RegenAllBlkDefinition();
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\nErreur : {ex.Message}");
                }
            }
        }
        private static bool TryGetValidBlockReference(Editor ed, Transaction tr, out ObjectId blockRefId, out BlockReference blockRef)
        {
            bool IsValid;
            do
            {
                blockRefId = ObjectId.Null;
                blockRef = null;

                if (!ed.GetBlocks(out ObjectId[] ids, null, true, true) || ids.Length == 0)
                {
                    return false;
                }

                blockRefId = ids.First();
                blockRef = tr.GetObject(blockRefId, OpenMode.ForWrite) as BlockReference;

                if (blockRef == null)
                {
                    return false;
                }

                IsValid = IsUniformlyScaled(blockRef);

                if (!IsValid)
                {
                    Generic.WriteMessage("Ce programme n'est pas compatible avec les blocs à échelle non uniforme.");
                }
            } while (!IsValid);

            return true;
        }

        private static bool IsUniformlyScaled(BlockReference blockRef)
        {
            Scale3d s = blockRef.ScaleFactors;
            return s.X == s.Y && s.Y == s.Z;
        }

        private static void AddEntitiesToBlock(BlockReference BlockRef, ObjectId[] selectedIds)
        {
            BlockTableRecord BlockDef = BlockRef.GetBlocDefinition(OpenMode.ForWrite) as BlockTableRecord;
            Matrix3d blockTransform = BlockRef.BlockTransform;
            Matrix3d inverseTransform = blockTransform.Inverse();

            IdMapping acIdMap = new IdMapping();
            ObjectIdCollection SelectedIds = selectedIds.ToObjectIdCollection();
            SelectedIds.Remove(BlockRef.ObjectId);
            try
            {
                /*
                We can use DeepCloneObjects() here because we're within the same drawing:
                if we wanted to move the new block between databases, for some reason, 
                then we'd use WblockCloneObjects() followed by Insert(). 
                This latter approach ensures that any "hard" references are followed and the contents copied, too: 
                something we shouldn't need when working in the same database. 
                 */
                BlockDef.Database.DeepCloneObjects(SelectedIds, BlockDef.ObjectId, acIdMap, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            foreach (IdPair entId in acIdMap)
            {
                try
                {
                    if (!(entId.Value.GetDBObject(OpenMode.ForWrite) is Entity SelectedEnt)) { continue; }
                    SelectedEnt.TransformBy(inverseTransform);
                    entId.Key.EraseObject();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private static void AddEntitiesToXref(ObjectId BlockRefObjId, BlockReference XrefRef, ObjectId[] selectedIds)
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
            {
                //https://www.keanw.com/2015/01/modifying-the-contents-of-an-autocad-xref-using-net.html
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

                        xrefTr.Commit();
                        foreach (ObjectId entId in SelectedIds)
                        {
                            entId.EraseObject();
                        }
                    }
                }
            }
        }
    }
}