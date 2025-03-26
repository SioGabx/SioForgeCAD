using System.Collections.Generic;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DictionnaryExtensions
    {
        public static string TryGetValueString<T>(this Dictionary<T, string> dictionary, T key)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }
            if (dictionary.TryGetValue(key, out string value))
            {
                return value;
            }
            return string.Empty;
        }
    }
}
