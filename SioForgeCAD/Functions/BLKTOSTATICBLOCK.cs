using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class BLKTOSTATICBLOCK
    {
        public static void Convert()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (!ed.GetBlocks(out ObjectId[] ObjectIds, true, true))
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId blockRefId in ObjectIds)
                {
                    if (!(blockRefId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRef))
                    {
                        return;
                    }

                    string UniqueName;
                    if (blockRef.IsDynamicBlock || (blockRef.BlockTableRecord.GetDBObject() as BlockTableRecord).IsDynamicBlock)
                    {
                        UniqueName = BlockReferences.GetUniqueBlockName(blockRef.GetBlockReferenceName() + "_static");
                        blockRef.ConvertToStaticBlock(UniqueName);
                    }
                    else
                    {
                        UniqueName = blockRef.GetBlockReferenceName();
                    }

                    BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                    if (!bt.Has(UniqueName))
                    {
                        return;
                    }
                    BlockTableRecord blockDef = bt[UniqueName].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                    foreach (ObjectId EntityInBlockDef in blockDef)
                    {
                        if (EntityInBlockDef.GetDBObject() is Entity ent)
                        {
                            if (!ent.Visible)
                            {
                                ent.EraseObject();
                            }
                        }
                    }
                    foreach (ObjectId BlkRefInDrawing in blockDef.GetBlockReferenceIds(true, false))
                    {
                        (BlkRefInDrawing.GetDBObject(OpenMode.ForWrite) as BlockReference).RecordGraphicsModified(true);
                    }
                }
                tr.Commit();
            }
        }
    }
}
