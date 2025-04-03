using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCEXTRACT
    {
        private class VegInstance
        {
            public int Count;
            public string CompleteName;
            public string Type;

            public VegInstance(string completeName, string type)
            {
                Count = 0;
                CompleteName = completeName;
                Type = type;
            }
        }
        public static void Extract()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            SelectionFilter BlkSelectionFilter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            PromptSelectionResult SelectionBlkPSR = ed.GetSelectionRedraw(null, false, false, BlkSelectionFilter);
            PromptSelectionResult AllBlkBSR = ed.SelectAll(BlkSelectionFilter);

            if (SelectionBlkPSR.Status != PromptStatus.OK || AllBlkBSR.Status != PromptStatus.OK) { return; }
            Generic.WriteMessage("Extraction en cours... Veuillez patienter...");

            List <VegInstance> VegInstanceList = new List<VegInstance>();
            HashSet<ObjectId> ExtractedBlocObjIds = new HashSet<ObjectId>();

            ObjectId[] SelectedBlocObjIds = SelectionBlkPSR.Value.GetObjectIds();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject allBloc in AllBlkBSR.Value)
                {
                    if (allBloc?.ObjectId == null || !allBloc.ObjectId.IsDerivedFrom(typeof(BlockReference)))
                    {
                        continue;
                    }

                    BlockReference allBlocBlkRef = (BlockReference)tr.GetObject(allBloc.ObjectId, OpenMode.ForRead);
                    if (allBlocBlkRef?.IsXref() == true) { continue; }
                    string blockName = allBlocBlkRef.GetBlockReferenceName();

                    var infos = VEGBLOC.GetDataStore(allBlocBlkRef);
                    if (infos == null) { continue; }

                    string name = infos[VEGBLOC.DataStore.CompleteName];
                    string type = infos[VEGBLOC.DataStore.Type]?.ToUpper() ?? "UNKNOWN";

                    var instance = VegInstanceList.FirstOrDefault(inst => inst.CompleteName == name && inst.Type == type);
                    if (instance == null)
                    {
                        instance = new VegInstance(name, type);
                        VegInstanceList.Add(instance);
                    }

                    if (SelectedBlocObjIds.Contains(allBloc.ObjectId))
                    {
                        ExtractedBlocObjIds.Add(allBloc.ObjectId);
                        instance.Count++;
                    }
                }

                var clipboardText = string.Join("\n", VegInstanceList
                    .OrderBy(v => v.Type)
                    .ThenBy(v => v.CompleteName)
                    .Select(v => $"\"{v.Type}\"\t\"{v.CompleteName}\"\t{v.Count}")
                );

                Generic.WriteMessage($"Les métrés des blocs sélectionnés ont été copiés dans le presse-papiers.\nNombre d'espèces : {VegInstanceList.Count(inst => inst.Count > 0)} / {VegInstanceList.Count}");
                Clipboard.SetText(clipboardText);
                ed.SetImpliedSelection(ExtractedBlocObjIds.ToArray());
                tr.Commit();
            }
        }
    }
}
