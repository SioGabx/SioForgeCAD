using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    internal class BLKCLIPFRAME
    {
        internal static void Toggle()
        {
            var ed = Generic.GetEditor();
            var Current = Generic.GetSystemVariable("XCLIPFRAME");

            string[] keywords;

            if ((short)Current == 0)
            {
                keywords = new string[] { "Activer", "Desactiver" };
            }
            else
            {
                keywords = new string[] { "Desactiver", "Activer" };
            }

            var options = ed.GetOptions("Voulez-vous afficher/masquer les bordures du XCLIP ?", false, keywords);

            if (options.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) { return; }

            if (options.StringResult == "Activer")
            {
                Generic.SetSystemVariable("XCLIPFRAME", 2);
                Generic.WriteMessage("XCLIPFRAME activé");
            }
            else if (options.StringResult == "Desactiver")
            {
                Generic.SetSystemVariable("XCLIPFRAME", 0);
                Generic.WriteMessage("XCLIPFRAME désactivé");
            }
        }
    }
}