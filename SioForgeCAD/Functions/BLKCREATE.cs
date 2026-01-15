using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class BLKCREATE
    {
        public static void Create()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            var selResult = ed.GetSelectionRedraw();
            if (selResult.Status != PromptStatus.OK) { return; }


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptPointOptions ptOptions = new PromptPointOptions("Selectionnez le point de base")
                {
                    AllowNone = true
                };
                var ptResult = ed.GetPoint(ptOptions);

                string BlkName = string.Empty;
                do
                {
                    Forms.InputDialogBox dialogBox = new Forms.InputDialogBox();
                    dialogBox.SetUserInputPlaceholder(BlkName);
                    dialogBox.SetPrompt($"Indiquez un nom pour le bloc");
                    dialogBox.SetCursorAtEnd();
                    DialogResult dialogResult = dialogBox.ShowDialog();
                    if (dialogResult != DialogResult.OK)
                    {
                        return;
                    }
                    BlkName = dialogBox.GetUserInput();
                    if (string.IsNullOrWhiteSpace(BlkName))
                    {
                        BlkName = "BLK_" + DateTime.Now.Ticks.ToString();
                        continue;
                    }

                    BlkName = SymbolUtilityServices.RepairSymbolName(BlkName, false);
                }
                while (BlockReferences.IsBlockExist(BlkName));

                var BlockReferencesCollection = new DBObjectCollection();

                var modelSpace = SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForRead) as BlockTableRecord;
                var drawOrderTable = modelSpace.DrawOrderTableId.GetObject(OpenMode.ForRead) as DrawOrderTable;
                var selectedIds = new HashSet<ObjectId>(selResult.Value.GetObjectIds());
                var orderedIds = drawOrderTable.GetFullDrawOrder(0)
                    .Cast<ObjectId>()
                    .Where(id => selectedIds.Contains(id)).ToObjectIdCollection();
             
                var InsPoint = Points.GetFromPromptPointResult(ptResult);
                var BlkDefId = BlockReferences.CreateFromExistingEnts(BlkName, "", orderedIds, InsPoint, true, BlockScaling.Any, true);
                if (!BlkDefId.IsValid) { tr.Commit(); return; }
                var BlkRef = new BlockReference(InsPoint.SCG, BlkDefId)
                {
                    Rotation = Vector3d.XAxis.GetAngleTo(ed.CurrentUserCoordinateSystem.CoordinateSystem3d.Xaxis, Vector3d.ZAxis)
                };
                BlkRef.AddToDrawing();
                tr.Commit();
            }
        }

    }
}