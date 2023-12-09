using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class CCA
    {
        public static void Compute()
        {
            while (true)
            {
                double? StepValueNullable = GetStepValueToAdd();
                if (StepValueNullable == null)
                {
                    return;
                }
                double StepValue = (double)StepValueNullable;
                CotePoints PointCote = Commun.CotePoints.GetCotePoints("Selectionnez un point", null);
                if (CotePoints.NullPointExit(PointCote)) { return; }
                PlacePoint(PointCote, StepValue);
            }
        }

        public static void PlacePoint(CotePoints PointCote, double StepValue)
        {
            Database db = Generic.GetDatabase();
            int NumberOfPointArealdyInserted = 0;

            while (true)
            {
                NumberOfPointArealdyInserted++;
                double Altitude = PointCote.Altitude + StepValue * NumberOfPointArealdyInserted;
                Dictionary<string, string> ComputeValue(Points Intermediaire)
                {
                    return new Dictionary<string, string>() {
                        { "ALTIMETRIE", Altitude.ToString("#.00") }
                    };
                }


                DBObjectCollection ents = CotationElements.InitBlocForTransient(Settings.BlocNameAltimetrie, ComputeValue(PointCote.Points));
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    InsertionTransientPoints insertionTransientPoints = new InsertionTransientPoints(ents, ComputeValue);
                    string KeyWord = $"[{Altitude - StepValue}+{StepValue}]";
                    var InsertionTransientPointsValues = insertionTransientPoints.GetInsertionPoint($"\nIndiquez l'emplacements du point cote", KeyWord);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return;
                    }
                    CotationElements.InsertBlocFromBlocName(Settings.BlocNameAltimetrie, NewPointLocation, Generic.GetUSCRotation(Generic.AngleUnit.Radians), ComputeValue(NewPointLocation));
                    tr.Commit();
                }
            }
        }


        public static double? GetStepValueToAdd()
        {
            Editor ed = Generic.GetEditor();
            PromptDoubleOptions pDoubleOpts = new PromptDoubleOptions($"\nVeuillez indiquer le montant que vous souhaitez additionner ou soustraire.")
            {
                DefaultValue = Properties.Settings.Default.StepValue
            };

            PromptDoubleResult pDoubleRes = ed.GetDouble(pDoubleOpts);
            if (pDoubleRes.Status == PromptStatus.OK)
            {
                Properties.Settings.Default.StepValue = pDoubleRes.Value;
                Properties.Settings.Default.Save();
                return pDoubleRes.Value;
            }
            else
            {
                return null;
            }
        }




    }
}
