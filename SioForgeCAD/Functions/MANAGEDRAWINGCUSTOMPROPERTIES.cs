using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.JSONParser;
using System.Collections.Generic;
using System.Windows;

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
            var propsDict = db.GetCustomProperties();

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

            db.SetCustomProperties(props);
        }











    }
}
