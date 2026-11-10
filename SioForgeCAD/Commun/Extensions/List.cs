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

        public static void AddRangeUnique<T>(this List<T> list, IEnumerable<T> itemsToAddIfNotAlreadyInside)
        {
            foreach (var item in itemsToAddIfNotAlreadyInside)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }

        public static List<T> UnionUnique<T>(this IEnumerable<T> list, IEnumerable<T> ItemsToAddIfNotAlreadyInside)
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

        public static double SumNumeric(this List<object> values)
        {
            var doubles = values.ConvertAll(v =>
            {
                if (v is double d)
                {
                    return d;
                }

                if (double.TryParse(v.ToString(), out double res))
                {
                    return res;
                }

                return 0.0;
            });

            return doubles.Count != 0 ? doubles.Sum() : 0d;
        }

        public static bool HasTypeOf<T>(this List<T> list, Type type)
        {
            return list.Any(item => item != null && type.IsAssignableFrom(item.GetType()));
        }

        public static List<string> LevenshteinDistance(this List<string> list, string value, int distance = 3, int NumberToTake = -1)
        {
            if (NumberToTake == -1)
            {
                NumberToTake = list.Count;
            }

            List<string> suggestions = list
                .Select(b => new { Name = b, Distance = b.LevenshteinDistance(value) })
                .Where(x => x.Distance <= distance) // Seuil maximum d'erreurs
                .OrderBy(x => x.Distance)
                .Take(3)
                .Select(x => x.Name)
                .ToList();
            return suggestions;
        }
    }
}
