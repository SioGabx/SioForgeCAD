using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    internal static class TOGGLELAYERCOMPARE
    {
        private static bool _state = false;
        private static List<string> _layers = new List<string>();
        public static void Execute()
        {
            var db = Generic.GetDatabase();
            var ed = Generic.GetEditor();

            _layers = GetAffectedLayers(_layers);

            if (_layers == null || _layers.Count == 0)
            {
                return;
            }

            while (true)
            {
                var ppo = new PromptPointOptions("\nEntrée = Toggle des calques, Liste = sélectionner les calques, Echap = quitter");

                ppo.AllowNone = true;
                ppo.Keywords.Add("Liste");

                var result = ed.GetPoint(ppo);

                if (result.Status == PromptStatus.Cancel)
                {
                    break;
                }

                if (result.Status == PromptStatus.Keyword)
                {
                    if (result.StringResult == "Liste")
                    {
                        _layers = GetAffectedLayers(_layers);

                        if (_layers == null || _layers.Count == 0)
                        {
                            break;
                        }
                    }

                    continue;
                }

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    foreach (var layerName in _layers)
                    {
                        if (!lt.Has(layerName))
                        {
                            continue;
                        }

                        var layer = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);

                        layer.IsOff = _state;
                    }

                    tr.Commit();
                }

                _state = !_state;

                Generic.WriteMessage($"Calques {(_state ? "masqués" : "affichés")}.");
            }
        }



        public static List<string> GetAffectedLayers(List<string> SelectedItems)
        {
            var layoutNames = Layers.GetAllLayersInDrawings();
            List<string> layers = new List<string>();
            if (layoutNames.Count == 0)
            {
                return layers;
            }

            // Dialogue de sélection
            var dlg = new ComboboxDialog(layoutNames, "Layers", false);
            dlg.SetSelectedItems(SelectedItems);


            if (Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(dlg) == System.Windows.Forms.DialogResult.OK)
            {
                layers.AddRange(dlg.GetSelectedItems());
            }
            return layers;
        }
    }
}
