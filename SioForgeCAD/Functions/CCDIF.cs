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
            Database db = Generic.GetDatabase();
            while (true)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {

                        CotePoints FirstPointCote = CotePoints.GetCotePoints("Selectionnez un premier point", null);
                        if (CotePoints.NullPointExit(FirstPointCote)) { return; }
                        CotePoints SecondPointCote = CotePoints.GetCotePoints("Selectionnez un deuxième point", FirstPointCote.Points);
                        if (CotePoints.NullPointExit(SecondPointCote)) { return; }
                        var DynModeOldValue = Generic.GetSystemVariable("DYNMODE");
                        Generic.SetSystemVariable("DYNMODE", 0, false);
                        var DifferenceAltitude = Math.Abs(FirstPointCote.Altitude - SecondPointCote.Altitude);
                        var pts = GetOrderedPointsByAltitude(FirstPointCote, SecondPointCote);
                        string Message = $"Différence d'altitude entre {CotePoints.FormatAltitude(pts.Lowest.Altitude)} > {CotePoints.FormatAltitude(pts.Hightest.Altitude)}\n = {Generic.FormatNumberForPrint((DifferenceAltitude))} m";
                        Application.ShowAlertDialog(Message);
                        System.Windows.Clipboard.SetText(DifferenceAltitude.ToString());
                        Generic.WriteMessage(Message);
                        Generic.SetSystemVariable("DYNMODE", DynModeOldValue, false);
                    }
                    finally
                    {
                        HightLighter.UnhighlightAll();
                        tr.Commit();
                    }
                }
            }
        }

        private static (CotePoints Lowest, CotePoints Hightest) GetOrderedPointsByAltitude(CotePoints pt1, CotePoints pt2)
        {
            if (pt1.Altitude > pt2.Altitude)
            {
                return (pt2, pt1);
            }
            return (pt1, pt2);
        }
    }
}
