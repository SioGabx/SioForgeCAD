using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public class BLKMAKEUNIQUEEACH
    {
        private Dictionary<string, ObjectId> renamedBlockNames;
        private bool regroupBlockDefinitionIfSameName;

        public BLKMAKEUNIQUEEACH(bool MakeUniqueEachBlocks)
        {
            this.regroupBlockDefinitionIfSameName = MakeUniqueEachBlocks;
            if (MakeUniqueEachBlocks)
            {
                renamedBlockNames = new Dictionary<string, ObjectId>();
            }
        }

        public void MakeUniqueBlockReferences()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            var ActualSelection = ed.SelectImplied().Value;
            ObjectId[] selectedBlockIds;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (ActualSelection.Count == 0)
                {
                    PromptSelectionOptions peo = new PromptSelectionOptions()
                    {
                        MessageForAdding = "Select block references to rename",
                    };

                    PromptSelectionResult per = ed.GetSelection(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nMissed, try again.");
                        return;
                    }
                    selectedBlockIds = per.Value.GetObjectIds();
                }
                else
                {
                    selectedBlockIds = ActualSelection.GetObjectIds();
                }

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
                            ObjectId newBtrId = CloneBlockDefinition(tr, blockRef.BlockTableRecord, oldName, newName);

                            // Update the block reference to use the new block definition
                            blockRef.UpgradeOpen();
                            blockRef.BlockTableRecord = newBtrId;
                        }
                    }
                }

                tr.Commit();
            }
        }

        private ObjectId CloneBlockDefinition(Transaction tr, ObjectId oldBtrId, string oldName, string newName)
        {
            if (regroupBlockDefinitionIfSameName && renamedBlockNames.ContainsKey(oldName))
            {
                return renamedBlockNames[oldName];
            }
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            BlockTableRecord oldBtr = tr.GetObject(oldBtrId, OpenMode.ForRead) as BlockTableRecord;
            BlockTableRecord newBtr = new BlockTableRecord();
            newBtr.Name = newName;
            ObjectId newBtrId;
            // Append the new block table record to the block table
            using (BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable)
            {
                bt.UpgradeOpen();
                newBtrId = bt.Add(newBtr);
                tr.AddNewlyCreatedDBObject(newBtr, true);
                foreach (ObjectId id in oldBtr)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent != null)
                    {
                        Entity entCopy = ent.Clone() as Entity;
                        newBtr.AppendEntity(entCopy);
                        tr.AddNewlyCreatedDBObject(entCopy, true);
                    }
                }
                // Set the insertion point of the new block definition
                newBtr.Origin = oldBtr.Origin;
                if (regroupBlockDefinitionIfSameName)
                {
                    renamedBlockNames.Add(oldName, newBtr.ObjectId);
                }
            }
            return newBtrId;
        }

        private string GetUniqueBlockName(string baseName)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                string newName = baseName;
                int index = 1;
                while (bt.Has(newName))
                {
                    newName = $"{baseName}_{index}";
                    index++;
                }

                return newName;
            }
        }
    }
}
