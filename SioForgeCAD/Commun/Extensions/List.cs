using System;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ListExtensions
    {
        public static void DeepDispose<T>(this IList<T> list)
        {
            DeepDispose(list as IEnumerable<T>);
        }

        public static void DeepDispose<T>(this IEnumerable<T> list)
        {
            foreach (var item in list)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
