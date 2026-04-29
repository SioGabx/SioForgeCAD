using Microsoft.Win32;
using System.Diagnostics;

namespace SioForgeCAD.Commun.Mist.Helpers
{
    public static class Registries
    {
        public static string GetValue(string Path, string Name)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Path))
                {
                    if (key != null)
                    {
                        object initialDirectory = key.GetValue(Name);
                        if (initialDirectory != null)
                        {
                            return initialDirectory.ToString();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Impossible de lire le registre : {ex.Message}");
            }
            return null;
        }
    }
}
