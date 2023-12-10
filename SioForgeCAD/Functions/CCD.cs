using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public class CCD
    {
        private CotePoints OriginCotePoint = CotePoints.Null;
        private double SlopePourcentage = 0;
        public void Compute()
        {
            while (true)
            {
                double? SlopeValueNullable = GetSlopeValue();
                if (SlopeValueNullable == null)
                {
                    return;
                }
                SlopePourcentage = (double)SlopeValueNullable;
                OriginCotePoint = Commun.CotePoints.GetCotePoints("Selectionnez la cote de base", null);
                if (CotePoints.NullPointExit(OriginCotePoint)) { continue; }
                PlacePoint(OriginCotePoint);
            }
        }

        public Dictionary<string, string> ComputeValue(Points NewPoint)
        {
            double OriginAltitude = OriginCotePoint.Altitude;
            double DistanceFromOrigin = Lines.GetLength(OriginCotePoint.Points, NewPoint);
            double NewAltitude = Arythmetique.ComputePointFromSlopePourcentage(OriginAltitude, DistanceFromOrigin, SlopePourcentage);
            return new Dictionary<string, string>() {
                { "ALTIMETRIE", NewAltitude.ToString("#.00") }
            };
        }


        public void PlacePoint(CotePoints PointCote)
        {
            bool IsMultiplePlacement = false;
            do
            {
                Database db = Generic.GetDatabase();
                DBObjectCollection ents = CotationElements.InitBlocForTransient(Settings.BlocNameAltimetrie, ComputeValue(PointCote.Points));
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    InsertionTransientPoints insertionTransientPoints = new InsertionTransientPoints(ents, ComputeValue);
                    string KeyWord;
                    string GetInsertionPointMessage;
                    if (IsMultiplePlacement)
                    {
                        KeyWord = string.Empty;
                        GetInsertionPointMessage = "Indiquez les emplacements des points cotes";
                    }
                    else
                    {
                        KeyWord = "Multiples";
                        GetInsertionPointMessage = "Indiquez l'emplacements du point cote";
                    }
                    var InsertionTransientPointsValues = insertionTransientPoints.GetInsertionPoint($"\n{GetInsertionPointMessage}", PointCote.Points, KeyWord);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    bool IsOkStatus = NewPointPromptPointResult.Status == PromptStatus.OK;
                    bool IsKeyWordStatus = NewPointPromptPointResult.Status == PromptStatus.Keyword;
                    IsMultiplePlacement = IsMultiplePlacement || (IsKeyWordStatus && NewPointPromptPointResult.StringResult == "Multiples");
                    if (IsOkStatus)
                    {
                        if (NewPointLocation != null)
                        {
                            CotationElements.InsertBlocFromBlocName(Settings.BlocNameAltimetrie, NewPointLocation, Generic.GetUSCRotation(Generic.AngleUnit.Radians), ComputeValue(NewPointLocation));
                        }
                        else
                        {
                            IsOkStatus = false;
                        }
                    }
                    tr.Commit();
                    if (!(IsOkStatus || IsKeyWordStatus))
                    {
                        return;
                    }
                }
            } while (IsMultiplePlacement);
        }


        private double? GetSlopeValue()
        {
            Editor ed = Generic.GetEditor();
            PromptDoubleOptions pDoubleOpts = new PromptDoubleOptions($"\nIndiquez un pourcentage de pente (chiffres négatifs pour descendre)")
            {
                DefaultValue = Properties.Settings.Default.SlopeValue
            };

            PromptDoubleResult pDoubleRes = ed.GetDouble(pDoubleOpts);
            if (pDoubleRes.Status == PromptStatus.OK)
            {
                Properties.Settings.Default.SlopeValue = pDoubleRes.Value;
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
