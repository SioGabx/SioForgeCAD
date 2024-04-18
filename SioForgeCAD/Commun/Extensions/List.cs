using System;
using System.Collections.Generic;
using System.Linq;

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

        public static List<T> RemoveCommun<T>(this IEnumerable<T> list, IEnumerable<T> ItemsToRemove)
        {
            List<T> NewList = list.ToList();
            foreach (var item in list)
            {
                if (ItemsToRemove.Contains(item))
                {
                    NewList.Remove(item);
                }
            }
            return NewList;
        }

        public static List<T> AddRangeUnique<T>(this IEnumerable<T> list, IEnumerable<T> ItemsToAddIfNotAlreadyInside)
        {
            List<T> NewList = list.ToList();
            foreach (var item in ItemsToAddIfNotAlreadyInside)
            {
                if (!NewList.Contains(item))
                {
                    NewList.Add(item);
                }
            }
            return NewList;
        }
    }
}
