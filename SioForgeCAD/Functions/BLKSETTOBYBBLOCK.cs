using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class BLKSETTOBYBBLOCK
    {
        public static void ByBlock()
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

                    BlockTableRecord blockDef = blockRef.BlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                    foreach (ObjectId EntityInBlockDef in blockDef)
                    {
                        if (EntityInBlockDef.GetDBObject(OpenMode.ForWrite) is Entity ent)
                        {
                            ent.ColorIndex = 0; //ByBlock 
                            ent.Transparency = new Autodesk.AutoCAD.Colors.Transparency(Autodesk.AutoCAD.Colors.TransparencyMethod.ByBlock);
                            ent.Linetype = "BYBLOCK";
                            ent.LineWeight = LineWeight.ByBlock;
                            ent.Layer = "0";
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
