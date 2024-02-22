using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class IEnumerableExtensions
    {    /// <summary>
         /// For each loop.
         /// </summary>
         /// <typeparam name="T">The element type of source.</typeparam>
         /// <param name="source">The source collection.</param>
         /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }
    }
}
