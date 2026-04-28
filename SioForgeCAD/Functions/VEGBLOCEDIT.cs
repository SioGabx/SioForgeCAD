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
        public static bool SelectBloc(out PromptSelectionResult promptResult)
        {
            Editor editor = Generic.GetEditor();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
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
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count == 1)
                    {
                        return true;
                    }
                }
            }
        }

        public static void Edit()
        {
            Database db = Generic.GetDatabase();

            if (!SelectBloc(out PromptSelectionResult promptResult))
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var EditObject = promptResult.Value.GetObjectIds().First();
                BlockReference BlkRef = EditObject.GetDBObject() as BlockReference;

                VegblocEditDialog EditDialog = new VegblocEditDialog();

                // Initialise la couleur avec celle du calque actuel du bloc
                EditDialog.SetColor(Layers.GetLayerColor(BlkRef.Layer));

                var BlocData = VEGBLOC.GetDataStore(BlkRef);
                if (BlocData != null)
                {
                    if (BlocData.TryGetValueString(VEGBLOC.DataStore.BlocName) != BlkRef.GetBlockReferenceName())
                    {
                        Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog("Des données ont été trouvées mais sont incohérentes.. Le nom du bloc à peut-être été modifié en dehors de VEGBLOCEDIT");
                    }
                    EditDialog.NameInput.Text = BlocData.TryGetValueString(VEGBLOC.DataStore.CompleteName);
                    EditDialog.HeightInput.Text = BlocData.TryGetValueString(VEGBLOC.DataStore.Height);
                    EditDialog.WidthInput.Text = BlocData.TryGetValueString(VEGBLOC.DataStore.Width);
                    EditDialog.TypeInput.Text = VEGBLOC.GetVegblocType(BlocData.TryGetValueString(VEGBLOC.DataStore.Type));
                }

                var DialogResult = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(null, EditDialog, true);
                if (DialogResult != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                string Name = EditDialog.NameInput.Text;
                string Width = EditDialog.WidthInput.Text;
                string Height = EditDialog.HeightInput.Text;
                string Type = EditDialog.TypeInput.Text;
                var SelectedColor = EditDialog.SelectedColor; // Récupère la couleur choisie

                if (BlkRef.IsEntityOnLockedLayer())
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog("Le bloc sélectionné est sur un calque verrouillé");
                    return;
                }

                string OldBlockName = BlkRef.GetBlockReferenceName();
                if (OldBlockName != BlkRef.Layer)
                {
                    var ContinueWithNotGoodLayerName = MessageBox.Show($"Le bloc n'est peut-être pas sur le bon calque, voulez-vous continuer l'opération ?\nEn continuant, le calque sera renommé sous le nouveau nom.\n\nBloc       : {OldBlockName}\nCalque : {BlkRef.Layer}", Generic.GetExtensionDLLName(), MessageBoxButton.YesNo);
                    if (ContinueWithNotGoodLayerName != MessageBoxResult.Yes)
                    {
                        Generic.WriteMessage("Opération annulée");
                        return;
                    }
                    var OldLayer = Layers.GetLayerTableRecordByName(BlkRef.Layer);

                    // On utilise ici la nouvelle couleur sélectionnée lors de la création du nouveau calque
                    var LayerId = Layers.CreateLayer(OldBlockName, SelectedColor, OldLayer.LineWeight, OldLayer.Transparency, OldLayer.IsPlottable);
                    ObjectIdCollection objectIdCollection = BlockReferences.GetAllBlockReferenceInstances(OldBlockName, tr, db);
                    foreach (ObjectId item in objectIdCollection)
                    {
                        if (item.GetDBObject(OpenMode.ForWrite) is BlockReference BlkRefInstance)
                        {
                            BlkRefInstance.LayerId = LayerId;
                        }
                    }
                }
                // On s'assure d'appliquer la couleur sélectionnée au calque AVANT DE recrée le bloc
                Layers.SetLayerColor(OldBlockName, SelectedColor);

                string NewBlockName = VEGBLOC.CreateBlockFromData(Name, Height, Width, Type, out string BlockData, out bool WasCreated);
                if (string.IsNullOrWhiteSpace(NewBlockName))
                {
                    return;
                }

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // If block already exist and its not the actual name, we need to change to the existing one before trying to change maybe its size
                if (!string.Equals(OldBlockName, NewBlockName, StringComparison.CurrentCultureIgnoreCase) && !WasCreated && BlockReferences.IsBlockExist(NewBlockName))
                {
                    BlockReferences.ReplaceAllBlockReference(OldBlockName, NewBlockName);
                    OldBlockName = NewBlockName;
                }

                string OldBlockNewRenameName = OldBlockName;

                // If user is only changing size, -> the name dont change but we need to replace old references
                if (string.Equals(OldBlockName, NewBlockName, StringComparison.CurrentCultureIgnoreCase))
                {
                    BlockTableRecord Renbtr = (BlockTableRecord)tr.GetObject(bt[OldBlockName], OpenMode.ForWrite);
                    OldBlockNewRenameName = SymbolUtilityServices.RepairSymbolName(OldBlockName + "_" + DateTime.Now.Ticks.ToString(), false);
                    Renbtr.Name = OldBlockNewRenameName;
                    // Recreate the block;
                    NewBlockName = VEGBLOC.CreateBlockFromData(Name, Height, Width, Type, out BlockData, out _);
                }
                else
                {
                    // Not same layer, on garde l'ancienne transparence mais on applique la couleur choisie plus bas.
                    Layers.SetTransparency(NewBlockName, Layers.GetTransparency(BlkRef.Layer));
                }

                var BlkDef = db.GetBlocDefinition(NewBlockName);
                BlkDef.UpgradeOpen();
                BlkDef.Comments = BlockData;

                BlockReferences.ReplaceAllBlockReference(OldBlockNewRenameName, NewBlockName, true, true);
                Layers.Merge(OldBlockName, NewBlockName);

                // On s'assure d'appliquer la couleur sélectionnée au calque de destination à la toute fin
                Layers.SetLayerColor(NewBlockName, SelectedColor);

                tr.Commit();
            }
        }
    }
}