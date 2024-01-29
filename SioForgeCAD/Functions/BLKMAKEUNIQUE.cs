using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
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
                                    ObjectId newBtrId = BlockReferences.RenameBlockAndInsert(selectedBlockId, oldName, newName);
                                    selectedBlockId.EraseObject();
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

        private string GetUniqueBlockName(string oldName)
        {
            if (RegroupBlockDefinitionIfSameName && RenamedBlockNames.ContainsKey(oldName))
            {
                return RenamedBlockNames[oldName];
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
