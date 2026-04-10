using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ViewModel.HWTuner;
using Microsoft.Win32;
using SioForgeCAD.Commun;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public static class ALLANAVDIALOGSDEFINECURRENTDRAWING
    {
        public static class Event
        {
            public static void Attach()
            {
                Detach();

                DocumentCollection docs = Application.DocumentManager;

                // 1. S'abonner aux événements de création/destruction futurs
                docs.DocumentCreated += Event_DocumentCreated;
                docs.DocumentToBeDestroyed += Event_DocumentToBeDestroyed;

                // 2. S'abonner manuellement aux documents DÉJÀ ouverts au moment du chargement
                foreach (Document doc in docs)
                {
                    doc.CommandWillStart += Event_CommandWillStart;
                }
            }

            public static void Detach()
            {
                DocumentCollection docs = Application.DocumentManager;

                docs.DocumentCreated -= Event_DocumentCreated;
                docs.DocumentToBeDestroyed -= Event_DocumentToBeDestroyed;

                foreach (Document doc in docs)
                {
                    doc.CommandWillStart -= Event_CommandWillStart;
                }
            }

            private static void Event_DocumentCreated(object sender, DocumentCollectionEventArgs e)
            {
                var doc = e.Document;
                if (doc != null)
                {
                    doc.CommandWillStart += Event_CommandWillStart;
                }
            }

            private static void Event_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                var doc = e.Document;
                if (doc != null)
                {
                    doc.CommandWillStart -= Event_CommandWillStart;
                }
            }

            private static void Event_CommandWillStart(object sender, CommandEventArgs e)
            {
                Debug.WriteLine("CommandWillStart");

                string cmd = e.GlobalCommandName.ToUpper();
                Debug.WriteLine(cmd);
                if (cmd == "OPEN" ||
                    cmd == "SAVEAS" ||
                    cmd == "LAYOUT_CONTROL" ||
                    cmd == "PLOT" ||
                    cmd == "QSAVE" ||
                    cmd == "ETRANSMIT" ||
                    cmd == "PUBLISH" ||
                    cmd == "IMPORT" ||
                    cmd == "EXPORT" ||
                    cmd == "NETLOAD" ||
                    cmd == "APPLOAD" ||
                    cmd == "RECOVER" ||
                    cmd == "WMFIN" ||
                    cmd == "DXBIN" ||
                    cmd == "ACISIN" ||
                    cmd == "ATTACH" ||//from ruban
                    cmd == "XATTACH" ||//in XREF menu
                    cmd == "IMAGEATTACH" ||//in XREF menu
                    cmd == "DWFATTACH" ||//in XREF menu
                    cmd == "DGNATTACH" ||//in XREF menu
                    cmd == "PDFATTACH" ||//in XREF menu
                    cmd == "PDFIMPORT" ||//from ruban
                    cmd == "GEOGRAPHICLOCATION" || //GEOGRAPHICLOCATION from file
                    cmd == "POINTCLOUDATTACH" || //in XREF menu
                    cmd == "COORDINATIONMODELATTACH" || //in XREF menu
                    cmd == "EXPORTDWF" || //big A -> Export
                    cmd == "EXPORTDWFX" || //big A -> Export
                    cmd == "3DDWF" || //big A -> Export
                    cmd == "EXPORTPDF" || //big A -> Export
                    cmd == "DGNEXPORT" || //big A -> Export
                    cmd == "ARCHIVE" ||
                    cmd == "NEW" ||
                    cmd == "QNEW" ||
                    cmd == "EXPORT" ||
                    cmd == "XREF")
                {
                    Debug.WriteLine(cmd);
                    UpdateRegistry();
                }
            }

        }

        public static string ExtractPath()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return string.Empty;

            if (doc.IsNamedDrawing)
            {
                return Path.GetDirectoryName(doc.Name);
            }

            try
            {
                //Fallback find [HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\RXX.X\ACAD-XXXX:XXX\Profiles\XXXXX\Dialogs\BrowseFolder]
                string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
                string currentProfile = Application.GetSystemVariable("CPROFILE") as string;

                if (!string.IsNullOrEmpty(currentProfile))
                {
                    string regPath = $@"{sProdKey}\Profiles\{currentProfile}\Dialogs\BrowseFolder";

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                    {
                        if (key != null)
                        {
                            object initialDirectory = key.GetValue("InitialDirectory");
                            if (initialDirectory != null)
                            {
                                return initialDirectory.ToString();
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[Erreur ExtractPath] Impossible de lire le registre : {ex.Message}");
            }

            // Fallback si rien n'est trouvé (ni DWG enregistré, ni registre valide)
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }


        public static void UpdateRegistry()
        {
            try
            {
                string Path = ExtractPath();
                Debug.WriteLine($"ExtractPath : {Path}");
                // Récupération de la racine courante (ex: Software\Autodesk\AutoCAD\R24.0\ACAD-4101:409)
                string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
                string profilesPath = $@"{sProdKey}\Profiles";

                using (RegistryKey profilesKey = Registry.CurrentUser.OpenSubKey(profilesPath, true))
                {
                    if (profilesKey == null) return;

                    foreach (string profileName in profilesKey.GetSubKeyNames())
                    {
                        string AllAnavDialogs = $@"{profileName}\Dialogs\AllAnavDialogs";
                        using (RegistryKey AllAnavDialogsKey = profilesKey.OpenSubKey(AllAnavDialogs, true))
                        {
                            if (AllAnavDialogsKey != null)
                            {
                                ApplyAllAnavDialogsFolderLogic(AllAnavDialogsKey, Path);
                            }
                        }

                        string AcPublishDlg = $@"{profileName}\Dialogs\AcPublishDlg";
                        using (RegistryKey AcPublishDlgKey = profilesKey.OpenSubKey(AcPublishDlg, true))
                        {
                            AcPublishDlgKey?.SetValue("Location", Path, RegistryValueKind.String);
                        }

                        string BrowseropenDialog = $@"{profileName}\Dialogs\BrowseropenDialog";
                        using (RegistryKey BrowseropenDialogKey = profilesKey.OpenSubKey(BrowseropenDialog, true))
                        {
                            BrowseropenDialogKey?.SetValue("InitialDirectory", Path, RegistryValueKind.String);
                        }
                    }
                }

                string ETransmitSetupsPath = $@"{sProdKey}\ETransmit\setups";
                using (RegistryKey ETransmitSetups = Registry.CurrentUser.OpenSubKey(ETransmitSetupsPath, true))
                {
                    if (ETransmitSetups == null) return;
                    foreach (string setup in ETransmitSetups.GetSubKeyNames())
                    {
                        using (RegistryKey AcPublishDlgKey = ETransmitSetups.OpenSubKey(setup, true))
                        {
                            AcPublishDlgKey?.SetValue("DestFolder", Path, RegistryValueKind.String);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur dans ALLANAVDIALOGSDEFINECURRENTDRAWING : {ex.Message}");
            }
        }

        private static void ApplyAllAnavDialogsFolderLogic(RegistryKey key, string Path)
        {
            int foundIndex = -1;
            int firstEmptyIndex = -1;

            for (int i = 0; true; i++)
            {
                object rawPathVal = key.GetValue($"PlacesOrder{i}");

                if (rawPathVal == null)
                {
                    // La valeur n'existe pas : on est à la fin de la liste d'AutoCAD.
                    if (firstEmptyIndex == -1)
                    {
                        firstEmptyIndex = i;
                    }
                    break;
                }

                string pathVal = rawPathVal as string;
                string displayVal = key.GetValue($"PlacesOrder{i}Display") as string;

                // Est-ce notre "Drawing Folder" ?
                if (displayVal?.Equals("Drawing Folder", StringComparison.OrdinalIgnoreCase) == true)
                {
                    foundIndex = i;
                    break;
                }

                // Est-ce un emplacement existant mais vide ?
                if (firstEmptyIndex == -1 && string.IsNullOrEmpty(pathVal))
                {
                    firstEmptyIndex = i;
                }
            }

            if (foundIndex != -1)
            {
                key.SetValue($"PlacesOrder{foundIndex}", Path, RegistryValueKind.String);
            }
            else if (firstEmptyIndex != -1)
            {
                key.SetValue($"PlacesOrder{firstEmptyIndex}", Path, RegistryValueKind.String);
                key.SetValue($"PlacesOrder{firstEmptyIndex}Display", "Drawing Folder", RegistryValueKind.String);
                key.SetValue($"PlacesOrder{firstEmptyIndex}Ext", "", RegistryValueKind.String);
            }
        }
    }
}