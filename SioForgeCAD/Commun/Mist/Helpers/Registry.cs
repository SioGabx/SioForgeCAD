using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SioForgeCAD.Commun.Mist.Helpers
{
    public static class Registries
    {
        public static void OpenRegEditAtKey(string keyPath)
        {
            SetValue(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit", "LastKey",Path.Combine(@"Computer\HKEY_CURRENT_USER", keyPath));
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { }
        }


        /// <summary>
        /// Lit une valeur dans le registre. Retourne la valeur par défaut si elle n'existe pas.
        /// </summary>
        public static bool KeyExist(string RegistryPath, string name)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key?.GetValue(name) != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur de lecture du registre pour {name} : {ex.Message}");
            }
            return false;
        }

        public static T GetValue<T>(string RegistryPath, string name, T defaultValue = default)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(name);
                        if (val != null)
                        {
                            // ChangeType gère la conversion (ex: le bool "True" du registre redevient un booléen C#)
                            return (T)Convert.ChangeType(val, typeof(T));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur de lecture du registre pour {name} : {ex.Message}");
            }
            return defaultValue;
        }



        /// <summary>
        /// Écrit ou met à jour une valeur dans le registre. Crée l'arborescence si nécessaire.
        /// </summary>
        public static void SetValue<T>(string RegistryPath, string name, T value)
        {
            try
            {
                // CreateSubKey ouvre la clé si elle existe, ou la crée si elle est manquante
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    key?.SetValue(name, value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur d'écriture du registre pour {name} : {ex.Message}");
            }
        }

        public static void DeleteValue(string RegistryPath, string name)
        {
            try
            {
                // CreateSubKey ouvre la clé si elle existe, ou la crée si elle est manquante
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    key?.DeleteValue(name, false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur d'écriture du registre pour {name} : {ex.Message}");
            }
        }
    }
}
