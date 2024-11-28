using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;

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
                BlockReference BlockRef = ObjectIds.First().GetDBObject(OpenMode.ForWrite) as BlockReference;
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
                        Generic.WriteMessage("\nUnable to modify the external reference. It may be open in the editor or read-only.");
                    }
                    else
                    {
                        //https://www.keanw.com/2015/01/modifying-the-contents-of-an-autocad-xref-using-net.html
                        //RestoreOriginalXrefSymbols(Xref);
                        var SelectedIds = Selection.Value.GetObjectIds().ToObjectIdCollection();
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
                                
                                foreach (ObjectId entId in SelectedIds)
                                {
                                    entId.EraseObject();
                                }
                                
                                xrefTr.Commit();
                                tr.Commit();
                            }
                        }
                    }
                }
                else
                {
                    // Modifier le bloc dans le dessin courant
                    BlockTableRecord blockDef = BlockRef.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
                    AddEntitiesToBlock(blockDef, BlockRef.BlockTransform, Selection.Value.GetObjectIds(), tr);
                    tr.Commit();
                }
               
                BlockRef.RegenAllBlkDefinition();
            }
        }

        private static void AddEntitiesToBlock(BlockTableRecord BlockDef, Matrix3d blockTransform, ObjectId[] selectedIds, Transaction tr)
        {
            Matrix3d inverseTransform = blockTransform.Inverse();

            foreach (ObjectId entId in selectedIds)
            {
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
