using System;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Extensions
{
    public class AutoDisposeList<T> : List<T>, IDisposable where T : IDisposable
    {
        public void Dispose()
        {
            foreach (var obj in this)
            {
                obj.Dispose();
            }
        }
    }

    public static class ListExtensions
    {

    }
}
