using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
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

            var Selection = ed.GetSelectionRedraw("Selectionnez des entités à inclure au block", true, false);
            if (Selection.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference BlockRef = ObjectIds.First().GetDBObject(OpenMode.ForRead) as BlockReference;
                BlockTableRecord BlockDef = BlockRef.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;

                Matrix3d blockTransform = BlockRef.BlockTransform;
                foreach (var SelectedEntObjId in Selection.Value.GetObjectIds())
                {
                    Entity SelectedEnt = SelectedEntObjId.GetDBObject(OpenMode.ForWrite) as Entity;
                    Entity SelectedEntClone = SelectedEnt.Clone() as Entity;
                    Matrix3d BlockTransform = BlockRef.BlockTransform.Inverse();

                    SelectedEntClone.TransformBy(BlockTransform);
                    BlockDef.AppendEntity(SelectedEntClone);
                    tr.AddNewlyCreatedDBObject(SelectedEntClone, true);
                    SelectedEnt.Erase();
                }

                tr.Commit();
                BlockRef.RegenAllBlkDefinition();
            }
        }
    }
}
