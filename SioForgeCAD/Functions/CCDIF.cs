using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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

                HightLighter.UnhighlightAll();
                var DifferenceAltitude = Math.Abs(FirstPointCote.Altitude - SecondPointCote.Altitude);
                string Message = $"Différence d'altitude : {Generic.FormatNumberForPrint((DifferenceAltitude))}";
                Generic.WriteMessage(Message);
                Application.ShowAlertDialog(Message);
                System.Windows.Clipboard.SetText(DifferenceAltitude.ToString());
            }
        }
    }
}
