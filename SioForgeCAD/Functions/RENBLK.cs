using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Linq;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Windows.Forms;
using System.Diagnostics;
using System;

namespace SioForgeCAD.Functions
{
    public static class RENBLK
    {
        public static void RenameBloc()
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            // Définir un filtre pour ne sélectionner que des blocs
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT")
            };

            SelectionFilter filter = new SelectionFilter(filterList);

            // Options pour la sélection d'une seule entité avec le filtre
            PromptSelectionOptions options = new PromptSelectionOptions();
            options.MessageForAdding = "\nSélectionnez un bloc à renommer : ";
            options.SingleOnly = true;

            // Demandez à l'utilisateur de sélectionner un bloc
            PromptSelectionResult selectionResult = ed.GetSelection(options, filter);

            if (selectionResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Sélection de bloc annulée ou invalide.");
                return;
            }

            // Obtenez l'ObjectId de l'entité sélectionnée (le bloc)
            ObjectId SelectedBlocObjectId = selectionResult.Value.GetObjectIds().FirstOrDefault();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference OriginalBlk = SelectedBlocObjectId.GetEntity() as BlockReference;
                string blockname = OriginalBlk.GetBlockReferenceName();

                // ouvrir la table des blocs en lecture
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                // vérifier si la table contient bien le bloc à renommer
                if (bt.Has(blockname))
                {
                    while (true)
                    {
                        Forms.InputDialogBox dialogBox = new Forms.InputDialogBox();
                        dialogBox.SetUserInputPlaceholder(blockname);
                        dialogBox.SetPrompt("Indiquez un nouveau nom de bloc");
                        DialogResult dialogResult = dialogBox.ShowDialog();
                        if (dialogResult != DialogResult.OK)
                        {
                            break;
                        }
                        string NewName = dialogBox.GetUserInput();
                        if (string.IsNullOrWhiteSpace(NewName))
                        {
                            ed.WriteMessage("Impossible de definir le block avec un nom vide.\n");
                            continue;
                        }
                        // ouvrir la défintion du bloc en écriture
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockname], OpenMode.ForWrite);
                        // changer le nom du bloc
                        try
                        {
                            btr.Name = NewName;
                            break;
                        }
                        catch (Exception ex)
                        {
                            ed.WriteMessage($"Impossible de definir le nom spécifié : {ex.Message}\n");
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}
