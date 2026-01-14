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
                //AddToNewDrawing(orderedIds.ToObjectIdCollection());
                var InsPoint = Points.GetFromPromptPointResult(ptResult);
                var BlkDefId = BlockReferences.CreateFromExistingEnts("*U", "", orderedIds, InsPoint, true, BlockScaling.Any, true);
                if (!BlkDefId.IsValid) { tr.Commit(); return; }
                var BlkRef = new BlockReference(InsPoint.SCG, BlkDefId);
                
                BlkRef.AddToDrawing();
                tr.Commit();
            }
        }

        public static void AddToNewDrawing(ObjectIdCollection SelectedIds)
        {

            //// Create a new in-memory drawing
            //Database db = new Database(true, false); // true = default template, true = in-memory

            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            //    // Get the BlockTable
            //    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            //    // Get ModelSpace BlockTableRecord
            //    BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            //    // Create DBText
            //    DBText text = new DBText
            //    {
            //        Position = new Point3d(2.0, 2.0, 0.0),
            //        Layer = "0",
            //        Height = 500.0,
            //        TextString = "Hello, World"
            //    };

            //    // Append text to ModelSpace (this associates it with the database)
            //    text.AddToDrawingCurrentTransaction();

            //    tr.Commit();
            //}
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Database MemoryDatabase = new Database(true, false);
                IdMapping acIdMap = new IdMapping();
                using (Transaction MemoryTransaction = MemoryDatabase.TransactionManager.StartTransaction())
                {
                    BlockTable acBlkTblNewDoc = MemoryTransaction.GetObject(MemoryDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRecNewDoc = MemoryTransaction.GetObject(acBlkTblNewDoc[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                    try
                    {
                        MemoryDatabase.WblockCloneObjects(SelectedIds, acBlkTblRecNewDoc.ObjectId, acIdMap, DuplicateRecordCloning.Replace, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        return;
                    }
                    MemoryTransaction.Commit();
                }


                db.Insert("TEST", MemoryDatabase, false);

                tr.Commit();
            }

        }


        //public static void CreateOld()
        //{
        //    Editor ed = Generic.GetEditor();
        //    Database db = Generic.GetDatabase();

        //    var selResult = ed.GetSelectionRedraw();
        //    if (selResult.Status != PromptStatus.OK) { return; }
        //    PromptPointOptions ptOptions = new PromptPointOptions("Selectionnez le point de base")
        //    {
        //        AllowNone = true
        //    };
        //    var ptResult = ed.GetPoint(ptOptions);

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        var BlockReferencesCollection = new DBObjectCollection();

        //        var modelSpace = SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForRead) as BlockTableRecord;
        //        var drawOrderTable = modelSpace.DrawOrderTableId.GetObject(OpenMode.ForRead) as DrawOrderTable;
        //        var selectedIds = new HashSet<ObjectId>(selResult.Value.GetObjectIds());
        //        var orderedIds = drawOrderTable.GetFullDrawOrder(0)
        //            .Cast<ObjectId>()
        //            .Where(id => selectedIds.Contains(id));

        //        foreach (ObjectId SelectedEntityObjId in orderedIds)
        //        {
        //            var ent = SelectedEntityObjId.GetDBObject(OpenMode.ForWrite);
        //            BlockReferencesCollection.Add(ent.Clone() as DBObject);
        //            ent.Erase();
        //        }

        //        var InsPoint = Points.GetFromPromptPointResult(ptResult);
        //        var BlkDefId = BlockReferences.Create("*U", "", BlockReferencesCollection, InsPoint, true, BlockScaling.Any);
        //        var BlkRef = new BlockReference(InsPoint.SCG, BlkDefId);
        //        BlkRef.AddToDrawing();
        //        tr.Commit();
        //    }
        //}
























    }
}