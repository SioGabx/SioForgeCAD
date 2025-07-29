using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class BLKREPLACE
    {
        public static void All()
        {
            var db = Generic.GetDatabase();
            var ed = Generic.GetEditor();

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!ed.GetBlocks(out ObjectId[] BlkToReplaceObjIds, "Sélectionnez un ou des types de blocs À REMPLACER :", false, true)) return;
                if (!ed.GetBlock(out ObjectId BlkToReplacementObjId, "Sélectionnez le bloc DE REMPLACEMENT :", true)) return;

                var BlkToReplacement = (BlkToReplacementObjId.GetDBObject() as BlockReference);
                string NewBlockName = BlkToReplacement.GetBlockReferenceName();
                List<string> OldBlockNewRenameNames = new List<string>();
                foreach (var item in BlkToReplaceObjIds)
                {
                    var OldBlockNewRenameName = (item.GetDBObject() as BlockReference).GetBlockReferenceName();
                    if (OldBlockNewRenameName != NewBlockName)
                    {
                        OldBlockNewRenameNames.Add(OldBlockNewRenameName);
                    }
                }

                if (OldBlockNewRenameNames.Count == 0)
                {

                    tr.Commit();
                    return;
                }

                GetUserOptions(ed, out bool keepRotation, out bool keepScale);
                var BackupLayer = Layers.GetCurrentLayerName();
                Layers.SetCurrentLayerName(BlkToReplacement.Layer);
                foreach (var OldBlockNewRenameName in OldBlockNewRenameNames)
                {

                    BlockReferences.ReplaceAllBlockReference(OldBlockNewRenameName, NewBlockName, keepScale, keepRotation);
                }
                Layers.SetCurrentLayerName(BackupLayer);
                tr.Commit();
            }
        }
        public static void Selected()
        {
            var db = Generic.GetDatabase();
            var ed = Generic.GetEditor();

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!ed.GetBlocks(out ObjectId[] BlkToReplaceObjIds, "Sélectionnez un ou plusieurs instances de blocs À REMPLACER :", false, true)) return;
                if (!ed.GetBlock(out ObjectId BlkToReplacementObjId, "Sélectionnez le bloc DE REMPLACEMENT :", true)) return;

                var BlkToReplacement = (BlkToReplacementObjId.GetDBObject() as BlockReference);
                string NewBlockName = BlkToReplacement.GetBlockReferenceName();
                List<string> OldBlockNewRenameNames = new List<string>();

                GetUserOptions(ed, out bool keepRotation, out bool keepScale);

                var BackupLayer = Layers.GetCurrentLayerName();
                Layers.SetCurrentLayerName(BlkToReplacement.Layer);

                BlockReferences.ReplaceAllBlockReference(BlkToReplaceObjIds.ToObjectIdCollection(), NewBlockName, keepScale, keepRotation);
                Layers.SetCurrentLayerName(BackupLayer);

                tr.Commit();
            }
        }

        private static void GetUserOptions(Editor ed, out bool keepRotation, out bool keepScale)
        {
            var keepRotationPrompt = ed.GetOptions("Conserver la rotation du bloc À REMPLACER ?", "Oui", "Non");
            keepRotation = keepRotationPrompt.StringResult != "Non";
            var keepScalePrompt = ed.GetOptions("Conserver l’échelle du bloc À REMPLACER ?", "Oui", "Non");
            keepScale = keepRotationPrompt.StringResult != "Non";
        }
    }
}
