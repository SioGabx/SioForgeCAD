using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Microsoft.Win32;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.Helpers;
using System;
using System.Diagnostics;
using System.IO;
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
                Autodesk.AutoCAD.ApplicationServices.Core.Application.EnterModal += Application_EnterModal;
            }

            private static void Application_EnterModal(object sender, EventArgs e)
            {
                Debug.WriteLine("ALLANAVDIALOGSDEFINECURRENTDRAWING : Enter Model");
                UpdateRegistry();
            }

            public static void Detach()
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.EnterModal -= Application_EnterModal;
            }
        }

        public static string ExtractPath()
        {
            var doc = Generic.GetDocument();
            return doc?.GetPath() ?? string.Empty;
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