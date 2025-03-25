using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class GripDataCollectionExtensions
    {
        public static GripData[] ToArray(this GripDataCollection grips)
        {
            GripData[] newArray = new GripData[grips.Count];
            int index = 0;
            foreach (GripData item in grips)
            {
                newArray.SetValue(item, index++);
            }
            return newArray;
        }
    }
}
