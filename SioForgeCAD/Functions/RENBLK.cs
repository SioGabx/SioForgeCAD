﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class RENBLK
    {
        public static void RenameBloc()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId[] SelectedBlocObjectIdArray;
            var ActualSelection = ed.SelectImplied().Value;

            if (ActualSelection?.Count > 1)
            {
                var ListOfUniqueBlockName = new List<string>();
                foreach (ObjectId SelectedBlocObjectId in ActualSelection.GetObjectIds())
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        if (!(SelectedBlocObjectId.GetEntity() is BlockReference OriginalBlk))
                        {
                            continue;
                        }
                        string blockname = OriginalBlk.GetBlockReferenceName();
                        if (!ListOfUniqueBlockName.Contains(blockname))
                        {
                            ListOfUniqueBlockName.Add(blockname);
                        }
                    }
                }
                if (ListOfUniqueBlockName.Count > 1)
                {
                    var AskContinue = MessageBox.Show($"Vous avez sélectionné un total de {ListOfUniqueBlockName.Count} blocs dont le nom est différent.\nÊtes-vous sûr de vouloir continuer ?", Generic.GetExtensionDLLName(), MessageBoxButtons.YesNo);
                    if (AskContinue != DialogResult.Yes)
                    {
                        return;
                    }
                }
            }
            if (ActualSelection == null || ActualSelection.Count <= 0)
            {
                PromptEntityOptions options = new PromptEntityOptions("Sélectionnez un bloc à renommer : ");
                PromptEntityResult selectionResult = ed.GetEntity(options);
                if (selectionResult.Status != PromptStatus.OK)
                {
                    Generic.WriteMessage("Sélection de bloc annulée ou invalide.");
                    return;
                }
                SelectedBlocObjectIdArray = new ObjectId[] { selectionResult.ObjectId };
            }
            else
            {
                SelectedBlocObjectIdArray = ActualSelection.GetObjectIds();
            }
            var ArealdyRenamedBlock = new List<string>();
            foreach (ObjectId SelectedBlocObjectId in SelectedBlocObjectIdArray)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    if (!(SelectedBlocObjectId.GetEntity() is BlockReference OriginalBlk))
                    {
                        continue;
                    }
                    string OldName = OriginalBlk.GetBlockReferenceName();
                    if (ArealdyRenamedBlock.Contains(OldName))
                    {
                        continue;
                    }
                    // ouvrir la table des blocs en lecture
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    // vérifier si la table contient bien le bloc à renommer
                    if (bt.Has(OldName))
                    {
                        while (true)
                        {
                            Forms.InputDialogBox dialogBox = new Forms.InputDialogBox();
                            dialogBox.SetUserInputPlaceholder(OldName);
                            dialogBox.SetPrompt($"Indiquez un nouveau nom pour le bloc \"{OldName}\"");
                            dialogBox.SetCursorAtEnd();
                            DialogResult dialogResult = dialogBox.ShowDialog();
                            if (dialogResult != DialogResult.OK)
                            {
                                return;
                            }
                            string NewName = dialogBox.GetUserInput();
                            if (string.IsNullOrWhiteSpace(NewName))
                            {
                                Generic.WriteMessage("Impossible de definir le block avec un nom vide.\n");
                                continue;
                            }
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[OldName], OpenMode.ForWrite);

                            NewName = SymbolUtilityServices.RepairSymbolName(NewName, false);
                            try
                            {
                                btr.Name = NewName;
                                ArealdyRenamedBlock.Add(NewName);
                                BlockReferences.Purge(OldName);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Generic.WriteMessage($"Impossible de definir le nom spécifié : {ex.Message}\n");
                            }
                        }
                    }

                    ed.SetImpliedSelection(Array.Empty<ObjectId>());
                    ed.SetImpliedSelection(SelectedBlocObjectIdArray.ToArray());
                    tr.Commit();
                }
            }
        }
    }
}
