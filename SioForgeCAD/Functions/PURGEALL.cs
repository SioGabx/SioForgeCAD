using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;

namespace SioForgeCAD.Functions
{
    public static class PURGEALL
    {
        public static void Purge()
        {
            Editor ed = Generic.GetEditor();
            Generic.Command("_-PURGE", "_ALL", "*", "N");
            Generic.Command("_-PURGE", "_REGAPPS", "*", "N");
        }
    }
}
