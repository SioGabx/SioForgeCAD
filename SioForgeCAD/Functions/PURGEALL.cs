using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class PURGEALL
    {
        public static void Purge()
        {
            Editor ed = Generic.GetEditor();
            ed.Command("_-PURGE", "_ALL", "*", "N");
            ed.Command("_-PURGE", "_REGAPPS", "*", "N");
        }
    }
}
