using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class BLKRECORDMODIFIEDGRAPHICS
    {
        public static void Execute()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (!ed.GetBlocks(out ObjectId[] ObjectIds, "Selectionnez un bloc", true, true))
            {
                return;
            }

            List<string> AlreadyProcessedBlkNames = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId blockRefId in ObjectIds)
                {
                    var SelEnt = blockRefId.GetDBObject(OpenMode.ForWrite) as Entity;
                    if (SelEnt is BlockReference blockRef)
                    {
                        var BlkName = blockRef.GetBlockReferenceName();
                        if (!AlreadyProcessedBlkNames.Contains(BlkName))
                        {
                            AlreadyProcessedBlkNames.Add(BlkName);
                            blockRef.RegenAllBlkDefinition();
                        }

                    }
                }
                tr.Commit();
                ed.SetImpliedSelection(ObjectIds);
                Generic.RegenALLCommand();
            }
        }
    }
}
