using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.JSONParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace SioForgeCAD.Functions
{
    public static class MANAGEDRAWINGCUSTOMPROPERTIES
    {
        public static void Menu()
        {
            var ed = Generic.GetEditor();
            var res = ed.GetOptions("Quelle option effectuer ?", false, "COPIER", "COLLER");
            if (res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                if (res.StringResult == "COPIER")
                {
                    CopyDrawingCustomProperties();
                }
                else if (res.StringResult == "COLLER")
                {
                    PasteDrawingCustomProperties();
                }
            }
        }

        public static void CopyDrawingCustomProperties()
        {
            Database db = Generic.GetDatabase();

            var propsDict = new Dictionary<string, string>();

            var summaryInfo = db.SummaryInfo;
            {
                var props = summaryInfo.CustomProperties;
                if (props != null)
                {
                    var enumerator = db.SummaryInfo.CustomProperties;
                    while (enumerator.MoveNext())
                    {
                        var entry = (KeyValuePair<string, string>)enumerator.Current;
                        string key = entry.Key;
                        string value = entry.Value;
                        propsDict.Add(key, value);
                    }
                }
            }

            if (propsDict.Count > 0)
            {
                string json = propsDict.ToJson();
                Clipboard.SetText(json);
                Generic.WriteMessage("Propriétés personnalisées copiées dans le presse-papiers (format JSON).");
            }
            else
            {
                Generic.WriteMessage("Aucune propriété personnalisée trouvée à copier.");
            }
        }


        public static void PasteDrawingCustomProperties()
        {
            Database db = Generic.GetDatabase();

            if (!Clipboard.ContainsText())
            {
                Generic.WriteMessage("Le presse-papiers ne contient pas de texte.");
                return;
            }

            string json = Clipboard.GetText();
            Dictionary<string, string> props = json.FromJson<Dictionary<string, string>>();

            if (props == null)
            {
                Generic.WriteMessage("Le contenu du presse-papiers n'est pas un JSON valide.");
                return;
            }

            if (props.Count == 0)
            {
                Generic.WriteMessage("Aucune propriété à coller depuis le JSON.");
                return;
            }

            var summaryInfoBuilder = new DatabaseSummaryInfoBuilder(db.SummaryInfo);
            var table = summaryInfoBuilder.CustomPropertyTable;
            int count = 0;

            foreach (var kvp in props)
            {
                table[kvp.Key] = kvp.Value;
                count++;
            }

            db.SummaryInfo = summaryInfoBuilder.ToDatabaseSummaryInfo();
        }











    }
}
