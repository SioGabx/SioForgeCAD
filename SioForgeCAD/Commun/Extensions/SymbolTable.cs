using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class SymbolTableExtensions
    {
        public static IEnumerable<T> GetObjects<T>(this SymbolTable source, Transaction tr, OpenMode mode = OpenMode.ForRead, bool openErased = false)
           where T : SymbolTableRecord
        {
            foreach (ObjectId id in openErased ? source.IncludingErased : source)
            {
                yield return (T)tr.GetObject(id, mode, openErased, false);
            }
        }
    }
}
