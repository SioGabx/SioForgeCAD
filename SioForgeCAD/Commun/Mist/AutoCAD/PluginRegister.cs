using Autodesk.AutoCAD.DatabaseServices;
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
                    // Check to see if the "MyApp" key exists
                    foreach (string subKey in regAcadAppKey.GetSubKeyNames())
                    {
                        // If the application is already registered, exit
                        if (subKey.Equals(sAppName))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
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
                    Generic.WriteMessage($"{sAppName} est déja enregistrée");
                    return;
                }

                using (RegistryKey regAcadAppKey = GetAutoCADApplicationsRegistryKey())
                {
                    string sAssemblyPath = Generic.GetExtensionDLLLocation();
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'inscription de l'application : {ex.Message}");
            }
        }

        public static void Unregister()
        {
            try
            {
                using (RegistryKey regAcadAppKey = GetAutoCADApplicationsRegistryKey())
                {
                    string sAppName = Generic.GetExtensionDLLName();
                    regAcadAppKey.DeleteSubKeyTree(sAppName);
                    Generic.WriteMessage($"{sAppName} ne se chargera désormais plus au démarage d'AutoCAD");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la désinscription de l'application : {ex.Message}");
            }
        }
    }
}
