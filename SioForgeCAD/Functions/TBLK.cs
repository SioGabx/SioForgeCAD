using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class TBLK
    {
        private class BlkInstance
        {
            public int Count;
            public string BlockName;

            public BlkInstance(string blockname)
            {
                Count = 0;
                BlockName = blockname;
            }
        }
        public static void Compute()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            SelectionFilter BlkSelectionFilter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            PromptSelectionResult SelectionBlkPSR = ed.GetSelectionRedraw(null, false, false, BlkSelectionFilter);


            if (SelectionBlkPSR.Status != PromptStatus.OK) { return; }
            Generic.WriteMessage("Extraction en cours... Veuillez patienter...");
            System.Windows.Forms.Application.DoEvents();

            List<BlkInstance> BlkInstanceList = new List<BlkInstance>();
            HashSet<ObjectId> ExtractedBlocObjIds = new HashSet<ObjectId>();

            ObjectId[] SelectedBlocObjIds = SelectionBlkPSR.Value.GetObjectIds();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject allBloc in SelectionBlkPSR.Value)
                {
                    if (allBloc?.ObjectId == null || !allBloc.ObjectId.IsDerivedFrom(typeof(BlockReference)))
                    {
                        continue;
                    }

                    BlockReference allBlocBlkRef = (BlockReference)tr.GetObject(allBloc.ObjectId, OpenMode.ForRead);
                    if (allBlocBlkRef?.IsXref() == true) { continue; }
                    string blockName = allBlocBlkRef.GetBlockReferenceName();

                    var instance = BlkInstanceList.FirstOrDefault(inst => inst.BlockName == blockName);
                    if (instance == null)
                    {
                        instance = new BlkInstance(blockName);
                        BlkInstanceList.Add(instance);
                    }

                    if (SelectedBlocObjIds.Contains(allBloc.ObjectId))
                    {
                        ExtractedBlocObjIds.Add(allBloc.ObjectId);
                        instance.Count++;
                    }
                }

                var clipboardText = string.Join("\n", BlkInstanceList
                    .OrderBy(v => v.BlockName)
                    .Select(v => $"\"{v.BlockName}\"\t{v.Count}")
                );

                Generic.WriteMessage($"Les métrés des blocs sélectionnés ont été copiés dans le presse-papiers.\nNombre de blocks : {BlkInstanceList.Count(inst => inst.Count > 0)}");
                Clipboard.SetText(clipboardText);
                ed.SetImpliedSelection(ExtractedBlocObjIds.ToArray());
                tr.Commit();
            }
        }
    }
}
