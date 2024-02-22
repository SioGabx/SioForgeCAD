using Autodesk.AutoCAD.BoundaryRepresentation;
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
using System.Text;
using System.Threading.Tasks;
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
                var rawJSON = BlkRef.GetDescription();

                var Json = rawJSON.FromJson<Dictionary<string, string>>();

                VegblocEditDialog EditDialog = new VegblocEditDialog();
                if (Json != null)
                {
                    if (Json["BlocName"] == BlkRef.GetBlockReferenceName())
                    {
                        EditDialog.NameInput.Text = Json["CompleteName"];
                        EditDialog.HeightInput.Text = Json["Height"];
                        EditDialog.WidthInput.Text = Json["Width"];
                        EditDialog.TypeInput.Text = Json["Type"];
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
                    MessageBox.Show("Le bloc séléctionné est sur un calque vérrouillé");
                    return;
                }

                string OldBlockName = BlkRef.GetBlockReferenceName();
                string NewBlockName = VEGBLOC.CreateBlockFromData(Name, Height, Width, Type);
                if (string.IsNullOrWhiteSpace(NewBlockName))
                {
                    return;
                }

                Layers.SetLayerColor(NewBlockName, Layers.GetLayerColor(BlkRef.Layer));

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
