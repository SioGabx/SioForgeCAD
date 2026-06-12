using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using RegistryKey = Autodesk.AutoCAD.Runtime.RegistryKey;

namespace SioForgeCAD.Commun
{
    static class PluginRegister
    {
        private static RegistryKey GetAutoCADApplicationsRegistryKey(bool Writable = true)
        {
            // Get the AutoCAD Applications key
            string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
            RegistryKey regAcadProdKey = Autodesk.AutoCAD.Runtime.Registry.CurrentUser.OpenSubKey(sProdKey);
            return regAcadProdKey.OpenSubKey("Applications", Writable);
        }

        public static bool IsAlreadyRegister()
        {
            try
            {
                using (RegistryKey regAcadAppKey = GetAutoCADApplicationsRegistryKey(false))
                {
                    string sAppName = Generic.GetExtensionDLLName();

                    // On tente d'ouvrir directement la sous-clé de notre application
                    using (RegistryKey appKey = regAcadAppKey.OpenSubKey(sAppName))
                    {
                        // Si la clé n'existe pas, l'application n'est pas enregistrée
                        if (appKey == null)
                        {
                            return false;
                        }

                        // La clé existe, on vérifie maintenant la valeur de LOADCTRLS
                        object loadCtrlsObj = appKey.GetValue("LOADCTRLS");

                        // Si LOADCTRLS n'existe pas, on considère qu'il faut ré-enregistrer
                        if (loadCtrlsObj == null)
                        {
                            return false;
                        }

                        // Si LOADCTRLS est défini sur LoadDisabled, on considère qu'il faut ré-enregistrer
                        int loadCtrlsValue = (int)loadCtrlsObj;
                        if (loadCtrlsValue == (int)ApplicationLoadReasons.LoadDisabled)
                        {
                            return false;
                        }

                        // Si la clé existe et que LOADCTRLS est présent et différent de LoadDisabled
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la verification de l'inscription de l'application : {ex.Message}");
            }
            return false;
        }

        public static void Register()
        {
            try
            {
                string sAppName = Generic.GetExtensionDLLName();

                if (IsAlreadyRegister())
                {
                    Generic.WriteMessage($"{sAppName} est déja enregistrée et active.");
                    return;
                }

                using (RegistryKey regAcadAppKey = GetAutoCADApplicationsRegistryKey())
                {
                    string sAssemblyPath = Generic.GetExtensionDLLLocation();

                    // CreateSubKey ouvrira la clé en écriture si elle existe déjà, 
                    // ou la créera si elle n'existe pas. C'est parfait pour notre cas.
                    RegistryKey regAppAddInKey = regAcadAppKey.CreateSubKey(sAppName);
                    regAppAddInKey.SetValue("DESCRIPTION", sAppName, RegistryValueKind.String);

                    regAppAddInKey.SetValue("LOADCTRLS", ApplicationLoadReasons.OnAutoCADStartup | ApplicationLoadReasons.OnCommandInvocation | ApplicationLoadReasons.OnLoadRequest, RegistryValueKind.DWord);
                    /*
                        0x01 : OnProxyDetection - Load the application upon detection of proxy object.
                        0x02 : OnAutoCADStartup - Load the .NET application when AutoCAD starts up
                        0x04 : OnCommandInvocation - Load the .NET application whenever an unknown command is executed for which it has a registry entry
                        0x08 : OnLoadRequest - Load the application upon request by the user or another application.
                        0x10 : LoadDisabled - Do not load the application.
                        0x20 : TransparentlyLoadable - Do not demand load the .NET application for any reason
                    */
                    regAppAddInKey.SetValue("LOADER", sAssemblyPath, RegistryValueKind.String);
                    regAppAddInKey.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                    Generic.WriteMessage($"{sAppName} à été enregistrée avec succès");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'inscription de l'application : {ex.Message}");
            }
        }

        public static void Unregister()
        {
            try
            {
                // On passe 'true' pour s'assurer d'avoir les droits d'écriture
                using (RegistryKey regAcadAppKey = GetAutoCADApplicationsRegistryKey(true))
                {
                    string sAppName = Generic.GetExtensionDLLName();

                    // On ouvre la clé de l'application en écriture au lieu d'utiliser une variable non déclarée
                    using (RegistryKey regAppAddInKey = regAcadAppKey.OpenSubKey(sAppName, true))
                    {
                        if (regAppAddInKey != null)
                        {
                            regAppAddInKey.SetValue("LOADCTRLS", ApplicationLoadReasons.LoadDisabled, RegistryValueKind.DWord);
                            Generic.WriteMessage($"{sAppName} ne se chargera désormais plus au démarage d'AutoCAD");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la désinscription de l'application : {ex.Message}");
            }
        }
    }
}