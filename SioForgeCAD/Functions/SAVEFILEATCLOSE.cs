using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class SAVEFILEATCLOSE
    {
        public static class Event
        {
            public static bool IsActive { get; private set; }

            public static void Attach()
            {
                if (!IsActive)
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.DocumentToBeDestroyed += Execute;
                    IsActive = true;
                }
            }

            public static void Detach()
            {
                if (IsActive)
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.DocumentToBeDestroyed -= Execute;
                    IsActive = false;
                }
            }
        }

        public static void Toggle()
        {
            if (Event.IsActive)
            {
                Generic.WriteMessage("SAVEFILEATCLOSE désactivé.");
                Event.Detach();
            }
            else
            {
                Generic.WriteMessage("SAVEFILEATCLOSE activé.");
                Event.Attach();
            }
        }

        public static void Execute(object senderObj, DocumentCollectionEventArgs docColDocActEvtArgs)
        {
            Document doc = docColDocActEvtArgs.Document;
            string baseName = Path.GetFileNameWithoutExtension(doc.Name);
            string projectName = baseName.SplitByListString("-", "_").FirstOrDefault();
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string targetDirectory = Settings.SaveFileAtCloseDirectory;
            string finalDwgName = $"{baseName}_AutoSave_at_{timeStamp}.dwg";
            string finalFilePath = Path.Combine(targetDirectory, finalDwgName);

            try
            {
                doc.Database.SaveAs(finalFilePath, false, DwgVersion.Current, null);
                Debug.WriteLine($"Sauvegarde effectuée : {finalDwgName}");

            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Erreur lors de la sauvegarde : " + ex.Message);
            }
        }
    }
}