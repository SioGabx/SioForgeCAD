using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class DBObjectExtensions
    {
        public static void RemoveAllXdata(this DBObject dbObj)
        {
            if (dbObj == null)
            {
                throw new ArgumentNullException(nameof(dbObj));
            }

            if (!dbObj.IsWriteEnabled)
            {
                throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.NotOpenForWrite);
            }

            ResultBuffer data = dbObj.XData;
            if (data != null)
            {
                foreach (TypedValue tv in data)
                {
                    if (tv.TypeCode == 1001)
                    {
                        dbObj.XData = new ResultBuffer(tv);
                    }
                }
            }
        }
    }
}
