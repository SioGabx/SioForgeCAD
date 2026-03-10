using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class BLKSETDEFINITIONTOEXPLODABLE
    {
        public static void SetExplodable()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            var psr = ed.GetBlocks(out ObjectId[] ObjIds, null, false, true);

            if (!psr) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var modifiedDefinitions = new HashSet<ObjectId>();

                foreach (var ObjId in ObjIds)
                {
                    if (tr.GetObject(ObjId, OpenMode.ForRead) is BlockReference blkRef &&
                        !modifiedDefinitions.Contains(blkRef.BlockTableRecord) &&
                        blkRef.GetBlocDefinition(OpenMode.ForWrite) is BlockTableRecord btr)
                    {
                        btr.Explodable = true;
                        modifiedDefinitions.Add(btr.ObjectId);
                    }
                }
                tr.Commit();
            }
        }
    }
}
