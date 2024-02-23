using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using SioForgeCAD.JSONParser;
using System;
using System.Collections.Generic;
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

        public static Dictionary<string, string> GetBlocData(BlockReference BlkRef)
        {
            string BlocDescription = BlkRef.GetDescription();
            if (string.IsNullOrWhiteSpace(BlocDescription))
            {
                return null;
            }
            Dictionary<string, string> BlocDataFromJson = BlocDescription.FromJson<Dictionary<string, string>>();
            if (BlocDataFromJson != null)
            {
                return BlocDataFromJson;
            }

            //LEGACY 
            var OldVeg = BlocDescription.Split('\n');
            if (OldVeg.Length == 3)
            {
                try
                {
                    Dictionary<string, string> BlocDataFromLegacy = new Dictionary<string, string>()
                {
                    {"BlocName", BlkRef.GetBlockReferenceName()},
                    {"CompleteName", OldVeg[0]},
                    {"Width",OldVeg[1].Split(':')[1]},
                    {"Height",OldVeg[2].Split(':')[1]},
                    {"Type", BlkRef.GetBlockReferenceName().Replace(Settings.VegblocLayerPrefix, "").Replace(OldVeg[0], "").Replace("_","") },
                };
                    return BlocDataFromLegacy;
                }
                catch (System.Exception) { }
            }

            return null;
        }





        public static void Edit()
        {
            Editor ed = Generic.GetEditor();
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
                var BlocData = GetBlocData(BlkRef);
                if (BlocData != null)
                {
                    if (BlocData.TryGetValueString("BlocName") == BlkRef.GetBlockReferenceName())
                    {
                        EditDialog.NameInput.Text = BlocData.TryGetValueString("CompleteName");
                        EditDialog.HeightInput.Text = BlocData.TryGetValueString("Height");
                        EditDialog.WidthInput.Text = BlocData.TryGetValueString("Width");
                        EditDialog.TypeInput.Text = BlocData.TryGetValueString("Type");
                    }
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


                if (BlkRef.IsEntityOnLockedLayer())
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Le bloc sélectionné est sur un calque verrouillé");
                    return;
                }

                string OldBlockName = BlkRef.GetBlockReferenceName();
                if (OldBlockName != BlkRef.Layer)
                {
                    var ContinueWithNotGoodLayerName = MessageBox.Show("Le bloc sélectionné n'est peut-être pas sur le bon calque, voulez-vous continuer l'opération ?", Generic.GetExtensionDLLName(), MessageBoxButton.YesNo);
                    if (ContinueWithNotGoodLayerName != MessageBoxResult.Yes)
                    {
                        Generic.WriteMessage("Opération annulée");
                        return;
                    }
                }



                string NewBlockName = VEGBLOC.CreateBlockFromData(Name, Height, Width, Type);
                if (string.IsNullOrWhiteSpace(NewBlockName))
                {
                    return;
                }

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                string OldBlockNewRenameName = OldBlockName;

                //If user is only changing size, -> the name dont change but we need to replace old references
                if (OldBlockName == NewBlockName)
                {
                    BlockTableRecord Renbtr = (BlockTableRecord)tr.GetObject(bt[OldBlockName], OpenMode.ForWrite);
                    OldBlockNewRenameName = SymbolUtilityServices.RepairSymbolName(OldBlockName + "_" + DateTime.Now.Ticks.ToString(), false);
                    Renbtr.Name = OldBlockNewRenameName;
                    //Recreate the block;
                    NewBlockName = VEGBLOC.CreateBlockFromData(Name, Height, Width, Type);
                }
                else
                {
                    //Not same layer, we should copy properties of the old one
                    Layers.SetLayerColor(NewBlockName, Layers.GetLayerColor(BlkRef.Layer));
                   //Layers.SetTransparency(NewBlockName, Layers.GetTransparency(BlkRef.Layer));
                }


                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                foreach (ObjectId objId in btr)
                {
                    Entity ent = objId.GetEntity(OpenMode.ForWrite);

                    if (ent is BlockReference br)
                    {
                        if (br.GetBlockReferenceName() == OldBlockNewRenameName) // If the BlockReference matches the one to replace
                        {
                            BlockReferences.InsertFromNameImportIfNotExist(NewBlockName, br.Position.ToPoints(), ed.GetUSCRotation(AngleUnit.Radians), null, NewBlockName);
                            if (!br.IsErased)
                            {
                                br.Erase(true);
                            }
                        }
                    }
                }
                BlockReferences.Purge(OldBlockNewRenameName);
                Layers.Merge(OldBlockName, NewBlockName);
                tr.Commit();

            }
        }



    }
}
