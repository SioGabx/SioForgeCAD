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

                var modelSpace = SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForRead) as BlockTableRecord;
                var drawOrderTable = modelSpace.DrawOrderTableId.GetObject(OpenMode.ForRead) as DrawOrderTable;
                var selectedIds = new HashSet<ObjectId>(selResult.Value.GetObjectIds());
                var orderedIds = drawOrderTable.GetFullDrawOrder(0)
                    .Cast<ObjectId>()
                    .Where(id => selectedIds.Contains(id)).ToObjectIdCollection();
               
                var InsPoint = Points.GetFromPromptPointResult(ptResult);
                var BlkDefId = BlockReferences.CreateFromExistingEnts("*U", "", orderedIds, InsPoint, true, BlockScaling.Any, true);
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