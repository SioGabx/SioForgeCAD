using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class BLKSETDEFINITIONTOSCALEUNIFORM
    {
        public static void SetBlockScaleToUniform()
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
                        btr.BlockScaling = BlockScaling.Uniform;
                        modifiedDefinitions.Add(btr.ObjectId);
                    }
                }
                tr.Commit();
            }
        }
    }
}
