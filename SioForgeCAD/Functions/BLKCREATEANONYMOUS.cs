using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class BLKCREATEANONYMOUS
    {
        public static void Create()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            var selResult = ed.GetSelectionRedraw();
            if (selResult.Status != PromptStatus.OK) { return; }
            PromptPointOptions ptOptions = new PromptPointOptions("Selectionnez le point de base")
            {
                AllowNone = true
            };
            var ptResult = ed.GetPoint(ptOptions);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var BlockReferencesCollection = new DBObjectCollection();
                foreach (ObjectId SelectedEntityObjId in selResult.Value.GetObjectIds())
                {
                    var ent = SelectedEntityObjId.GetDBObject(OpenMode.ForWrite);
                    BlockReferencesCollection.Add(ent.Clone() as DBObject);
                    ent.Erase();
                }

                var BlkDefId = BlockReferences.Create("*U", "", BlockReferencesCollection, ptResult.Value.ToPoints(), true, BlockScaling.Any);
                var BlkRef = new BlockReference(ptResult.Value, BlkDefId);
                BlkRef.AddToDrawing();
                tr.Commit();
            }
        }
    }
}
