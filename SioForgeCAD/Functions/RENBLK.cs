using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
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

            var actualSelection = ed.SelectImplied().Value;
            ObjectId[] selectedBlockIds;


            if (actualSelection == null || actualSelection.Count == 0)
            {
                PromptEntityOptions options = new PromptEntityOptions("\nSélectionnez un bloc à renommer : ");
                options.SetRejectMessage("L'objet sélectionné n'est pas un bloc.");
                options.AddAllowedClass(typeof(BlockReference), exactMatch: false); // Force la sélection d'un bloc

                PromptEntityResult selectionResult = ed.GetEntity(options);
                if (selectionResult.Status != PromptStatus.OK)
                {
                    Generic.WriteMessage("Sélection de bloc annulée ou invalide.");
                    return;
                }
                selectedBlockIds = new[] { selectionResult.ObjectId };
            }
            else
            {
                selectedBlockIds = actualSelection.GetObjectIds();
            }


            var uniqueBlockNames = new HashSet<string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selectedBlockIds)
                {
                    if (id.GetDBObject(OpenMode.ForRead) is BlockReference blkRef)
                    {
                        string blockName = blkRef.GetBlockReferenceName();
                        uniqueBlockNames.Add(blockName);
                    }
                }
                tr.Commit();
            }

            if (uniqueBlockNames.Count == 0)
            {
                Generic.WriteMessage("Aucun bloc valide n'a été trouvé dans la sélection.");
                return;
            }


            if (uniqueBlockNames.Count > 1)
            {
                var askContinue = MessageBox.Show(
                    $"Vous avez sélectionné un total de {uniqueBlockNames.Count} blocs dont le nom est différent.\nÊtes-vous sûr de vouloir continuer ?",
                    Generic.GetExtensionDLLName(),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (askContinue != DialogResult.Yes) return;
            }

            foreach (string oldName in uniqueBlockNames)
            {
                string newName = null;
                bool isNameValid = false;

                while (!isNameValid)
                {
                    using (Forms.InputDialogBox dialogBox = new Forms.InputDialogBox())
                    {
                        dialogBox.SetUserInputPlaceholder(oldName);
                        dialogBox.SetPrompt($"Indiquez un nouveau nom pour le bloc \"{oldName}\"");
                        dialogBox.SetCursorAtEnd();

                        if (dialogBox.ShowDialog() != DialogResult.OK)
                        {
                            return;
                        }

                        newName = dialogBox.GetUserInput();

                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            Generic.WriteMessage("Impossible de définir le bloc avec un nom vide.");
                            continue;
                        }

                        newName = SymbolUtilityServices.RepairSymbolName(newName, false);
                        isNameValid = true;
                    }
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    if (bt.Has(oldName))
                    {
                        try
                        {
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[oldName], OpenMode.ForWrite);
                            btr.Name = newName;

                            BlockReferences.Purge(oldName);
                            tr.Commit();
                        }
                        catch (Exception ex)
                        {
                            Generic.WriteMessage($"Impossible de définir le nom spécifié pour '{oldName}' : {ex.Message}");
                            tr.Abort();
                        }
                    }
                    else
                    {
                        tr.Abort();
                    }
                }
            }

            ed.SetImpliedSelection(Array.Empty<ObjectId>());
            ed.SetImpliedSelection(selectedBlockIds);
        }
    }
}