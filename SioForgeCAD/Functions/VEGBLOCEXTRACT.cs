using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCEXTRACT
    {
        private class VegInstance
        {
            public double Quantity;
            public string CompleteName;
            public string Type;
            public string Unit; // "U" ou "M2"

            public VegInstance(string completeName, string type, string unit)
            {
                Quantity = 0;
                CompleteName = completeName;
                Type = type;
                Unit = unit;
            }
        }

        public static void Extract()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            // Prise en compte des blocs (INSERT) et des hachures (HATCH)
            SelectionFilter vegSelectionFilter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.Start, "HATCH"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            });

            var SelectionVegPSR = ed.GetSelectionRedraw(null, false, false, vegSelectionFilter);
            PromptSelectionResult AllVegBSR = ed.SelectAll(vegSelectionFilter);

            if (SelectionVegPSR.Status != PromptStatus.OK || AllVegBSR.Status != PromptStatus.OK) { return; }

            Generic.WriteMessage("Extraction en cours... Veuillez patienter...");
            System.Windows.Forms.Application.DoEvents();

            List<VegInstance> VegInstanceList = new List<VegInstance>();
            HashSet<ObjectId> ExtractedObjIds = new HashSet<ObjectId>();

            ObjectId[] SelectedObjIds = SelectionVegPSR.Value.GetObjectIds();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject allObj in AllVegBSR.Value)
                {
                    if (allObj?.ObjectId == null) continue;

                    Entity allEntity = tr.GetObject(allObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (allEntity == null) continue;

                    string unit = string.Empty;
                    double qtyToAdd = 0;
                    Dictionary<VEGBLOC.DataStore, string> infos = null;

                    if (allEntity is BlockReference allBlocBlkRef)
                    {
                        if (allBlocBlkRef.IsXref())
                        {
                            continue;
                        }

                        unit = " u";
                        qtyToAdd = 1;
                        infos = VEGBLOC.GetDataStore(allBlocBlkRef);
                    }
                    else if (allEntity is Hatch hatch)
                    {
                        // Vérification du préfixe du calque
                        if (string.IsNullOrEmpty(hatch.Layer) || !hatch.Layer.StartsWith(Settings.PrefixVegblocLayer, System.StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        unit = " m²";
                        qtyToAdd = hatch.TryGetArea();
                        infos = VEGBLOC.GetDataStoreFromLayerName(hatch.Layer);
                    }
                    else
                    {
                        continue;
                    }

                    if (infos == null) continue;

                    string name = infos[VEGBLOC.DataStore.CompleteName];
                    string type = infos[VEGBLOC.DataStore.Type]?.ToUpper() ?? "UNKNOWN";

                    var instance = VegInstanceList.FirstOrDefault(inst => inst.CompleteName == name && inst.Type == type && inst.Unit == unit);
                    if (instance == null)
                    {
                        instance = new VegInstance(name, type, unit);
                        VegInstanceList.Add(instance);
                    }

                    if (SelectedObjIds.Contains(allObj.ObjectId))
                    {
                        ExtractedObjIds.Add(allObj.ObjectId);
                        instance.Quantity += qtyToAdd;
                    }
                }

               var clipboardText = string.Join("\n", VegInstanceList
                    .OrderBy(c =>
                    {
                        if (System.Enum.TryParse(VEGBLOC.GetVegblocType(c.Type), true, out VEGBLOC.VegblocTypes vegEnum))
                        {
                            return (int)vegEnum;
                        }
                        return int.MaxValue;
                    })
                    .ThenBy(c => c.CompleteName)
                    .Select(v => $"\"{v.Type}\"\t\"{v.CompleteName}\"\t{v.Quantity:0.##}\t\"{v.Unit}\"")
                );

                Generic.WriteMessage($"Les métrés sélectionnés ont été copiés dans le presse-papiers.\nNombre d'espèces : {VegInstanceList.Count(inst => inst.Quantity > 0)} / {VegInstanceList.Count}");
                Clipboard.SetText(clipboardText);
                ed.SetImpliedSelection(ExtractedObjIds.ToArray());
                tr.Commit();
            }
        }
    }
}