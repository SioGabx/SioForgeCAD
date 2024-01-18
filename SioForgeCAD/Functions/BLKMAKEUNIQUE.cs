using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public class BLKMAKEUNIQUE
    {
        private readonly Dictionary<string, string> RenamedBlockNames;
        private readonly bool RegroupBlockDefinitionIfSameName;

        public BLKMAKEUNIQUE(bool MakeUniqueEachBlocks)
        {
            this.RegroupBlockDefinitionIfSameName = MakeUniqueEachBlocks;
            if (MakeUniqueEachBlocks)
            {
                RenamedBlockNames = new Dictionary<string, string>();
            }
        }

        public void MakeUniqueBlockReferences()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            ObjectId[] selectedBlockIds;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ActualSelection = ed.SelectImplied().Value;
                if (ActualSelection == null || ActualSelection.Count == 0)
                {
                    PromptSelectionOptions peo = new PromptSelectionOptions()
                    {
                        MessageForAdding = "\nSelectionnez des blocs à rendre unique",
                    };

                    PromptSelectionResult per = ed.GetSelection(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nErreur, veuillez réesayer.");
                        return;
                    }
                    selectedBlockIds = per.Value.GetObjectIds();
                ed.SetImpliedSelection(new ObjectId[0]);
                }
                else
                {
                    selectedBlockIds = ActualSelection.GetObjectIds();
                }
                List<ObjectId> RenameblockNewObjectIds = new List<ObjectId>();
                foreach (ObjectId selectedBlockId in selectedBlockIds)
                {
                    if (!(tr.GetObject(selectedBlockId, OpenMode.ForWrite) is BlockReference blockRef))
                    {
                        continue;
                    }
                    using (blockRef)
                    {
                        if (blockRef != null)
                        {
                            string oldName = blockRef.GetBlockReferenceName();
                            string newName = GetUniqueBlockName(oldName);

                            if (string.IsNullOrEmpty(newName))
                            {
                                ed.WriteMessage($"\nInvalid or duplicate block name: {oldName}.");
                                return;
                            }

                            // Clone the old block definition and its contents
                            if (selectedBlockId.IsValid)
                            {
                                try
                                {
                                    ObjectId newBtrId = RenameBlockInAnotherDatabase(selectedBlockId, oldName, newName);
                                    if (!newBtrId.IsNull)
                                    {
                                        RenameblockNewObjectIds.Add(newBtrId);
                                    }
                                }
                                //catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                catch (Autodesk.AutoCAD.BoundaryRepresentation.Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                                
                            }
                        }
                    }
                }
                ed.SetImpliedSelection(RenameblockNewObjectIds.ToArray());
                tr.Commit();
            }
        }


        public static ObjectId RenameBlockInAnotherDatabase(ObjectId BlockReferenceObjectId, string OldName, string NewName)
        {  if (!BlockReferenceObjectId.IsValid)
            {
                return ObjectId.Null;
            }
            ObjectIdCollection acObjIdColl = new ObjectIdCollection { BlockReferenceObjectId };
          
            Document ActualDocument = Application.DocumentManager.MdiActiveDocument;
            Database ActualDatabase = ActualDocument.Database;

            Database MemoryDatabase = new Database(true, true);
            IdMapping acIdMap = new IdMapping();
            using (Transaction MemoryTransaction = MemoryDatabase.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTblNewDoc = MemoryTransaction.GetObject(MemoryDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRecNewDoc = MemoryTransaction.GetObject(acBlkTblNewDoc[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                MemoryDatabase.WblockCloneObjects(acObjIdColl, acBlkTblRecNewDoc.ObjectId, acIdMap, DuplicateRecordCloning.Replace, false);
                BlockTableRecord btr = (BlockTableRecord)MemoryTransaction.GetObject(acBlkTblNewDoc[OldName], OpenMode.ForWrite);
                btr.Name = NewName;
                MemoryTransaction.Commit();
            }

            ObjectId newBlocRefenceId = acIdMap[BlockReferenceObjectId].Value;
            if (!newBlocRefenceId.IsValid)
            {
                return ObjectId.Null;
            }
            ObjectIdCollection acObjIdColl2 = new ObjectIdCollection { newBlocRefenceId };
            IdMapping acIdMap2 = new IdMapping();
            using (Transaction ActualTransaction = ActualDatabase.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTblNewDoc2 = ActualTransaction.GetObject(ActualDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRecNewDoc2 = ActualTransaction.GetObject(acBlkTblNewDoc2[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                ActualDatabase.WblockCloneObjects(acObjIdColl2, acBlkTblRecNewDoc2.ObjectId, acIdMap2, DuplicateRecordCloning.Replace, false);
                ActualTransaction.Commit();
            }
            BlockReferenceObjectId.EraseObject();
            return acIdMap2[newBlocRefenceId].Value;
        }





        private string GetUniqueBlockName(string oldName)
        {
            if (RegroupBlockDefinitionIfSameName && RenamedBlockNames.ContainsKey(oldName))
            {
                return RenamedBlockNames[oldName];
            }
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                string newName = oldName;
                int index = 1;
                while (bt.Has(newName))
                {
                    newName = $"{oldName}_{index}";
                    index++;
                }
                if (RegroupBlockDefinitionIfSameName)
                {
                    RenamedBlockNames.Add(oldName, newName);
                }
                return newName;
            }


        }
    }
}
