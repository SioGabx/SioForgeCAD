using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class RENAMELAYERS
    {
        public static void Rename()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            List<string> layerNames = new List<string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    //Ignorer les calques système non renommables
                    if (ltr.Name == "0" || ltr.Name.Equals("Defpoints", StringComparison.OrdinalIgnoreCase))
                        continue;
                    layerNames.Add(ltr.Name);
                }
                tr.Commit();
            }

            using (var renameForm = new RenameDialog(layerNames, (original, transformed) =>
            {
                if (!string.Equals(original, transformed, StringComparison.Ordinal))
                {
                    try
                    {
                        return SymbolUtilityServices.RepairSymbolName(transformed, false);
                    }
                    catch
                    {
                        return transformed;
                    }
                }
                return original; // Pas de changement, retourner le nom original
            }))
            {
                //https://help.autodesk.com/view/ACD/2025/FRA/?guid=GUID-28E52FA3-248E-4F65-94DD-7C47BE761D58
                renameForm.UpdateMessage($"Les noms des calques peuvent contenir jusqu'à 255 caractères et inclure des lettres, des chiffres, des espaces et plusieurs caractères spéciaux.\nIls ne peuvent pas contenir les caractères suivants : {"< > / \\ “ : ; ? * | = ‘".Replace(' ', '\u00A0')}");
                if (renameForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var resultats = renameForm.GetRenamingResults();

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                        foreach (var item in resultats)
                        {
                            if (string.Equals(item.Original, item.Renamed, StringComparison.Ordinal))
                            {
                                continue;
                            }

                            if (Layers.Rename(item.Original, item.Renamed))
                            {
                                ed.WriteMessage($"Renommé : {item.Original} -> {item.Renamed}");
                            }
                            else
                            {
                                ed.WriteMessage($"Erreur sur {item.Original}");
                            }

                        }
                        tr.Commit();
                    }
                }
            }
        }
    }
}
