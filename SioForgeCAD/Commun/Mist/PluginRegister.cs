using Autodesk.AutoCAD.DatabaseServices;
using Microsoft.Win32;
using System.Reflection;
using RegistryKey = Autodesk.AutoCAD.Runtime.RegistryKey;

namespace SioForgeCAD.Commun
{
    static class PluginRegister
    {
        public static void Register()
        {
            // Get the AutoCAD Applications key
            string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
            string sAppName = Generic.GetExtensionDLLName();
            RegistryKey regAcadProdKey = Autodesk.AutoCAD.Runtime.Registry.CurrentUser.OpenSubKey(sProdKey);
            RegistryKey regAcadAppKey = regAcadProdKey.OpenSubKey("Applications", true);

            // Check to see if the "MyApp" key exists
            string[] subKeys = regAcadAppKey.GetSubKeyNames();
            foreach (string subKey in subKeys)
            {
                // If the application is already registered, exit
                if (subKey.Equals(sAppName))
                {
                    Generic.WriteMessage($"{sAppName} est déja enregistrée");
                    regAcadAppKey.Close();
                    return;
                }
            }

            // Get the location of this module
            string sAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // Register the application
            RegistryKey regAppAddInKey = regAcadAppKey.CreateSubKey(sAppName);
            regAppAddInKey.SetValue("DESCRIPTION", sAppName, RegistryValueKind.String);
            regAppAddInKey.SetValue("LOADCTRLS", 14, RegistryValueKind.DWord);
            regAppAddInKey.SetValue("LOADER", sAssemblyPath, RegistryValueKind.String);
            regAppAddInKey.SetValue("MANAGED", 1, RegistryValueKind.DWord);
            regAcadAppKey.Close();
            Generic.WriteMessage($"{sAppName} à été enregistrée avec succès");
        }

        public static void Unregister()
        {
            // Get the AutoCAD Applications key
            string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
            string sAppName = Generic.GetExtensionDLLName();

            RegistryKey regAcadProdKey = Autodesk.AutoCAD.Runtime.Registry.CurrentUser.OpenSubKey(sProdKey);
            RegistryKey regAcadAppKey = regAcadProdKey.OpenSubKey("Applications", true);

            // Delete the key for the application
            regAcadAppKey.DeleteSubKeyTree(sAppName);
            regAcadAppKey.Close();
            Generic.WriteMessage($"{sAppName} à été supprimée avec succès");
        }
    }
}
