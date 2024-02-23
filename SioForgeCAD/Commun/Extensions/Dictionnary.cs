using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DictionnaryExtensions
    {
        public static string TryGetValueString(this Dictionary<string, string> dictionary, string key)
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
