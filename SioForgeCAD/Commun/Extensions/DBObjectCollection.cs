using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DBObjectCollectionExtensions
    {
        public static DBObjectCollection Join(this DBObjectCollection A, DBObjectCollection B)
        {
            foreach (DBObject ent in B)
            {
                if (!A.Contains(ent))
                {
                    A.Add(ent);
                }
            }
            return A;
        }
    }
}
