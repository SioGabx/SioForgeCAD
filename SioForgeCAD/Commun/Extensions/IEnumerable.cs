using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Convertit n'importe quel IEnumerable (non générique) en List de T.
        /// Utile pour les anciennes API ou ICollection.
        /// </summary>
        public static List<T> ToList<T>(this IEnumerable source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Cast<T> transforme l'IEnumerable (objet) en IEnumerable<T> (typé)
            // ce qui rend ensuite ToList() disponible.
            return source.Cast<T>().ToList();
        }

    }
}
