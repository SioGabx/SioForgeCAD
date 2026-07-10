using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Mist.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DocumentExtensions
    {
        public static string GetPath(this Document doc)
        {
            if (doc == null)
            {
                return string.Empty;
            }

            if (doc.IsNamedDrawing)
            {
                return Path.GetDirectoryName(doc.Name);
            }


            //Fallback find [HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\RXX.X\ACAD-XXXX:XXX\Profiles\XXXXX\Dialogs\BrowseFolder]
            string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
            string currentProfile = Application.GetSystemVariable("CPROFILE") as string;

            if (!string.IsNullOrEmpty(currentProfile))
            {
                object initialDirectory = Registries.GetValue<string>($@"{sProdKey}\Profiles\{currentProfile}\Dialogs\BrowseFolder", "InitialDirectory");
                if (initialDirectory != null)
                {
                    return initialDirectory.ToString();
                }
            }
            // Fallback si rien n'est trouvé (ni DWG enregistré, ni registre valide)
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}
