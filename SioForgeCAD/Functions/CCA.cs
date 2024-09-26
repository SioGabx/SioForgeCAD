using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

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
            Editor ed = Generic.GetEditor();
            int NumberOfPointArealdyInserted = 0;

            while (true)
            {
                NumberOfPointArealdyInserted++;
                double Altitude = PointCote.Altitude + (StepValue * NumberOfPointArealdyInserted);
                Dictionary<string, string> ComputeValue(Points _)
                {
                    return new Dictionary<string, string>() {
                        { "ALTIMETRIE", CotePoints.FormatAltitude(Altitude) }
                    };
                }

                DBObjectCollection ents = Commun.Drawing.BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(PointCote.Points));
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    GetPointTransient insertionTransientPoints = new GetPointTransient(ents, ComputeValue);
                    string Signe = string.Empty;
                    if (StepValue > 0)
                    {
                        Signe = "+";
                    }
                    string KeyWord = $"{Altitude - StepValue}{Signe}{StepValue}";
                    var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez l'emplacements du point cote", PointCote.Points, KeyWord);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return;
                    }
                    Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, NewPointLocation, ed.GetUSCRotation(AngleUnit.Radians), ComputeValue(NewPointLocation));
                    tr.Commit();
                }
            }
        }

        public static double? GetStepValueToAdd()
        {
            Editor ed = Generic.GetEditor();
            PromptDoubleOptions pDoubleOpts = new PromptDoubleOptions("\nVeuillez indiquer le montant que vous souhaitez additionner ou soustraire.")
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
