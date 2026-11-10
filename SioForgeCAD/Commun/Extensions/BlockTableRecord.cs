using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class BlockTableRecordExtensions
    {
        public static bool IsModelSpace(this BlockTableRecord btr)
        {
            return (btr.Name == BlockTableRecord.ModelSpace);
        }
    }
}
