﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

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
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            ObjectId[] selectedBlockIds;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (Generic.GetLock())
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
                        Generic.WriteMessage("\nErreur, veuillez réesayer.");
                        return;
                    }
                    selectedBlockIds = per.Value.GetObjectIds();
                    ed.SetImpliedSelection(System.Array.Empty<ObjectId>());
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
                                Generic.WriteMessage($"\nInvalid or duplicate block name: {oldName}.");
                                return;
                            }

                            // Clone the old block definition and its contents
                            if (selectedBlockId.IsValid)
                            {
                                try
                                {
                                    int index = 0;
                                    ObjectId newBtrId = ObjectId.Null;
                                    do
                                    {
                                        index++;
                                        newBtrId = BlockReferences.RenameBlockAndInsert(selectedBlockId, oldName, newName);
                                        selectedBlockId.EraseObject();
                                        if (!newBtrId.IsNull)
                                        {
                                            RenameblockNewObjectIds.Add(newBtrId);
                                        }
                                    } while (newBtrId.IsNull && index < 5);
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
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

        private string GetUniqueBlockName(string oldName)
        {
            if (RegroupBlockDefinitionIfSameName && RenamedBlockNames.TryGetValue(oldName, out string value))
            {
                return value;
            }
            string newName = BlockReferences.GetUniqueBlockName(oldName);
            if (RegroupBlockDefinitionIfSameName)
            {
                RenamedBlockNames.Add(oldName, newName);
            }
            return newName;
        }
    }
}
