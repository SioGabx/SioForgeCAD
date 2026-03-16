using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class RENAMELAYOUT
    {
        public static void Rename()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            List<string> layoutNames = new List<string>();

            // 1. Récupération des noms de Layouts (hors Model)
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);

                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    // On ignore l'onglet "Model"
                    if (entry.Key.Equals("Model", StringComparison.OrdinalIgnoreCase))
                        continue;

                    layoutNames.Add(entry.Key);
                }
                tr.Commit();
            }

            // Note : Les layouts acceptent presque tout, mais RepairSymbolName assure la compatibilité
            using (var renameForm = new RenameDialog(layoutNames, (_, transformed) =>
            {
                try
                {
                    return SymbolUtilityServices.RepairSymbolName(transformed, false);
                }
                catch
                {
                    return transformed;
                }
            }))
            {
                //https://help.autodesk.com/view/ACD/2025/FRA/?guid=GUID-28E52FA3-248E-4F65-94DD-7C47BE761D58
                renameForm.UpdateMessage($"Les noms des présentations peuvent contenir jusqu'à 255 caractères et inclure des lettres, des chiffres, des espaces et plusieurs caractères spéciaux.\nIls ne peuvent pas contenir les caractères suivants : { "< > / \\ “ : ; ? * | = ‘".Replace(' ', '\u00A0')}");
                if (renameForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var resultats = renameForm.GetRenamingResults();

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        LayoutManager lm = LayoutManager.Current;

                        foreach (var item in resultats)
                        {
                            if (string.Equals(item.Original, item.Renamed, StringComparison.Ordinal))
                                continue;

                            try
                            {
                                lm.RenameLayout(item.Original, item.Renamed);
                                ed.WriteMessage($"Renommé : {item.Original} -> {item.Renamed}");
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage($"Erreur lors du renommage de {item.Original} : {ex.Message}");
                            }
                        }
                        tr.Commit();
                    }
                }
            }
        }
    }
}