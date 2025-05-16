using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using System;
using System.Diagnostics;
using System.IO;

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
            string FileName = "AutoSave_" + Path.GetFileNameWithoutExtension(doc.Name) + "_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".dwg";
            string tempFileName = Path.Combine(Path.GetTempPath(), FileName);
            Debug.WriteLine("NumberOfSaves : " + doc.Database.NumberOfSaves);
            try
            {
                doc.Database.SaveAs(tempFileName, false, DwgVersion.Current, null);
                Generic.WriteMessage("Sauvegarde temporaire créée à : " + tempFileName);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Erreur lors de la sauvegarde temporaire : " + ex.Message);
            }
        }
    }
}