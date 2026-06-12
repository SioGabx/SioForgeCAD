using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class CCDIF
    {
        public static void Compute()
        {
            while (true)
            {
                CotePoints FirstPointCote = CotePoints.GetCotePoints("Selectionnez un premier point", null);
                if (CotePoints.NullPointExit(FirstPointCote)) { return; }
                CotePoints SecondPointCote = CotePoints.GetCotePoints("Selectionnez un deuxième point", FirstPointCote.Points);
                if (CotePoints.NullPointExit(SecondPointCote)) { return; }

                var DifferenceAltitude = Math.Abs(FirstPointCote.Altitude - SecondPointCote.Altitude);
                Generic.WriteMessage($"Différence d'altitude : {DifferenceAltitude}");
            }
        }
    }
}
