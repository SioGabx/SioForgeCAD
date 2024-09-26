using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE
    {
        public static void Convert()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetBlocks(out var BlkObjId, true, false))
            {
                return;
            }

            var XrefToReload = new ObjectIdCollection();
            foreach (ObjectId blockRefId in BlkObjId)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blockRef = blockRefId.GetDBObject() as BlockReference;
                    if (blockRef == null)
                    {
                        ed.WriteMessage("\nFailed to get the block reference.");
                        return;
                    }

                    // Check if the block reference is an Xref
                    if (!blockRef.IsXref())
                    {
                        Generic.WriteMessage("L'entité selectionnée n'est pas une XREF");
                        return;
                    }

                    // Open the Xref's block table record
                    BlockTableRecord blockTableRecord = blockRef.BlockTableRecord.GetDBObject() as BlockTableRecord;

                    // Iterate through the layers of the Xref and override the color
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = layerId.GetDBObject(OpenMode.ForWrite) as LayerTableRecord;

                        if (ltr?.IsResolved == true)
                        {
                            ltr.Color = ltr.Color.ConvertColorToGray();
                            ltr.IsLocked = true;
                        }
                    }
                    XrefToReload.Add(blockRef.BlockTableRecord);
                    tr.Commit();
                }

                db.ReloadXrefs(XrefToReload);
            }


        }



    }
}
