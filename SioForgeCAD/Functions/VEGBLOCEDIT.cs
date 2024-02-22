using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
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

            string Name = "SioGabx sioforgecad 'Test'";
            string Width = "2.5";
            string Height = "3.1";
            string Type = "ARBUSTE";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var EditObject = promptResult.Value.GetObjectIds().First();



                BlockReference BlkRef = EditObject.GetDBObject() as BlockReference;
                if (BlkRef.IsEntityOnLockedLayer())
                {
                    MessageBox.Show("Le bloc séléctionné est sur un calque vérrouillé");
                    return;
                }

                string OldBlockName = BlkRef.GetBlockReferenceName();
                string NewBlockName = VEGBLOC.CreateBlockFromData(Name, Height, Width, Type);
                if (NewBlockName == OldBlockName) {
                    return;
                }
                Layers.SetLayerColor(BlkRef.Layer, Layers.GetLayerColor(BlkRef.Layer));


                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                foreach (ObjectId objId in btr)
                {
                    Entity ent = objId.GetEntity(OpenMode.ForWrite);

                    if (ent is BlockReference br)
                    {
                        if (br.GetBlockReferenceName() == OldBlockName) // If the BlockReference matches the one to replace
                        {
                            BlockReferences.InsertFromNameImportIfNotExist(NewBlockName, br.Position.ToPoints(), ed.GetUSCRotation(AngleUnit.Radians), null, NewBlockName);
                            if (!br.IsErased)
                            {
                                br.Erase(true);
                            }
                        }
                    }
                }
                Layers.Merge(OldBlockName, NewBlockName);
                tr.Commit();
            }
        }



    }
}
