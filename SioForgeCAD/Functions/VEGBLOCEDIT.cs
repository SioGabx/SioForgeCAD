using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Linq;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCEDIT
    {
        // Classe utilitaire pour transporter les données du formulaire proprement
        private class VegblocEditData
        {
            public string Name { get; set; }
            public string Width { get; set; }
            public string Height { get; set; }
            public string Type { get; set; }
            public Autodesk.AutoCAD.Colors.Color SelectedColor { get; set; } // Ajustez le namespace de Color si besoin
        }

        public static bool SelectBloc(out PromptSelectionResult promptResult)
        {
            Editor editor = Generic.GetEditor();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Sélectionnez un végétal (VEGBLOC compactible)",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            while (true)
            {
                promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return false;
                }

                if (promptResult.Status == PromptStatus.OK && promptResult.Value.Count == 1)
                {
                    return true;
                }
            }
        }

        public static void Edit()
        {
            if (!SelectBloc(out PromptSelectionResult promptResult))
            {
                return;
            }

            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var editObjectId = promptResult.Value.GetObjectIds().First();
                BlockReference blkRef = editObjectId.GetDBObject() as BlockReference;

                if (blkRef.IsEntityOnLockedLayer())
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog("Le bloc sélectionné est sur un calque verrouillé");
                    return;
                }

                VegblocEditData userInput = ShowDialogAndGetData(blkRef);
                if (userInput == null)
                {
                    // L'utilisateur a annulé ou fermé la fenêtre
                    return;
                }

                // Correction du calque si nécessaire
                if (!EnsureLayerConsistency(blkRef, userInput.SelectedColor, tr, db))
                {
                    return; // L'utilisateur a refusé de continuer après l'avertissement
                }

                // Mise à jour ou recréation du bloc
                UpdateBlockAndReferences(blkRef, userInput, tr, db);

                tr.Commit();
            }
        }

        private static VegblocEditData ShowDialogAndGetData(BlockReference blkRef)
        {
            VegblocEditDialog editDialog = new VegblocEditDialog();
            editDialog.SetColor(Layers.GetLayerColor(blkRef.Layer));

            var blocData = VEGBLOC.GetDataStore(blkRef);
            if (blocData != null)
            {
                if (blocData.TryGetValueString(VEGBLOC.DataStore.BlocName) != blkRef.GetBlockReferenceName())
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog("Des données ont été trouvées mais sont incohérentes. Le nom du bloc a peut-être été modifié en dehors de VEGBLOCEDIT");
                }
                editDialog.NameInput.Text = blocData.TryGetValueString(VEGBLOC.DataStore.CompleteName);
                editDialog.HeightInput.Text = blocData.TryGetValueString(VEGBLOC.DataStore.Height);
                editDialog.WidthInput.Text = blocData.TryGetValueString(VEGBLOC.DataStore.Width);
                editDialog.TypeInput.Text = VEGBLOC.GetVegblocType(blocData.TryGetValueString(VEGBLOC.DataStore.Type));
            }

            var dialogResult = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(null, editDialog, true);

            if (dialogResult != System.Windows.Forms.DialogResult.OK)
            {
                return null;
            }

            return new VegblocEditData
            {
                Name = editDialog.NameInput.Text,
                Width = editDialog.WidthInput.Text,
                Height = editDialog.HeightInput.Text,
                Type = editDialog.TypeInput.Text,
                SelectedColor = editDialog.SelectedColor
            };
        }

        private static bool EnsureLayerConsistency(BlockReference blkRef, Color selectedColor, Transaction tr, Database db)
        {
            string oldBlockName = blkRef.GetBlockReferenceName();

            if (oldBlockName == blkRef.Layer)
            {
                return true; // Tout est normal
            }

            var msg = $"Le bloc n'est peut-être pas sur le bon calque, voulez-vous continuer l'opération ?\nEn continuant, le calque sera renommé sous le nouveau nom.\n\nBloc : {oldBlockName}\nCalque : {blkRef.Layer}";
            var result = MessageBox.Show(msg, Generic.GetExtensionDLLName(), MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
            {
                Generic.WriteMessage("Opération annulée");
                return false;
            }

            var oldLayer = Layers.GetLayerTableRecordByName(blkRef.Layer);
            var layerId = Layers.CreateLayer(oldBlockName, selectedColor, oldLayer.LineWeight, oldLayer.Transparency, oldLayer.IsPlottable);

            foreach (ObjectId item in BlockReferences.GetAllBlockReferenceInstances(oldBlockName, tr, db))
            {
                if (item.GetDBObject(OpenMode.ForWrite) is BlockReference blkRefInstance)
                {
                    blkRefInstance.LayerId = layerId;
                }
            }

            return true;
        }

        private static void UpdateBlockAndReferences(BlockReference blkRef, VegblocEditData data, Transaction tr, Database db)
        {
            string oldBlockName = blkRef.GetBlockReferenceName();

            // Appliquer la couleur avant de recréer le bloc
            Layers.SetLayerColor(oldBlockName, data.SelectedColor);

            string newBlockName = VEGBLOC.CreateBlockFromData(data.Name, data.Height, data.Width, data.Type, out string blockData, out bool wasCreated);

            if (string.IsNullOrWhiteSpace(newBlockName))
            {
                return;
            }

            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Remplacement si le bloc existe déjà sous un autre nom
            if (!string.Equals(oldBlockName, newBlockName, StringComparison.CurrentCultureIgnoreCase) && !wasCreated && BlockReferences.IsBlockExist(newBlockName))
            {
                BlockReferences.ReplaceAllBlockReference(oldBlockName, newBlockName);
                oldBlockName = newBlockName;
            }

            string oldBlockNewRenameName = oldBlockName;

            // Logique si l'utilisateur modifie uniquement la taille (même nom)
            if (string.Equals(oldBlockName, newBlockName, StringComparison.CurrentCultureIgnoreCase))
            {
                BlockTableRecord renBtr = (BlockTableRecord)tr.GetObject(bt[oldBlockName], OpenMode.ForWrite);
                oldBlockNewRenameName = SymbolUtilityServices.RepairSymbolName(oldBlockName + "_" + DateTime.Now.Ticks.ToString(), false);
                renBtr.Name = oldBlockNewRenameName;

                // Recréer le bloc avec la nouvelle taille
                newBlockName = VEGBLOC.CreateBlockFromData(data.Name, data.Height, data.Width, data.Type, out blockData, out _);
            }
            else
            {
                Layers.SetTransparency(newBlockName, Layers.GetTransparency(blkRef.Layer));
            }

            // Mise à jour de la définition du bloc
            var blkDef = db.GetBlocDefinition(newBlockName);
            blkDef.UpgradeOpen();
            blkDef.Comments = blockData;

            // Remplacement final et fusion des calques
            BlockReferences.ReplaceAllBlockReference(oldBlockNewRenameName, newBlockName, true, true);
            Layers.Merge(oldBlockName, newBlockName);
            Layers.SetLayerColor(newBlockName, data.SelectedColor);
        }

    }
}