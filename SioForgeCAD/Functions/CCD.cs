using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
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
                OriginCotePoint = CotePoints.GetCotePoints("Selectionnez la cote de base", null);
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
                {"ALTIMETRIE", CotePoints.FormatAltitude(NewAltitude) },
                {"RAW_ALTIMETRIE", CotePoints.FormatAltitude(NewAltitude, 3) },
            };
        }

        public void PlacePoint(CotePoints PointCote)
        {
            bool IsMultiplePlacement = false;
            do
            {
                Database db = Generic.GetDatabase();
                Editor ed = Generic.GetEditor();
                DBObjectCollection ents = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(PointCote.Points));
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    GetPointTransient insertionTransientPoints = new GetPointTransient(ents, ComputeValue);
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
                    var InsertionTransientPointsValues = insertionTransientPoints.GetPoint($"\n{GetInsertionPointMessage}", PointCote.Points, KeyWord);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    bool IsOkStatus = NewPointPromptPointResult.Status == PromptStatus.OK;
                    bool IsKeyWordStatus = NewPointPromptPointResult.Status == PromptStatus.Keyword;
                    IsMultiplePlacement = IsMultiplePlacement || (IsKeyWordStatus && NewPointPromptPointResult.StringResult == "Multiples");
                    if (IsOkStatus)
                    {
                        if (NewPointLocation != null)
                        {
                            var ComputedValue = ComputeValue(NewPointLocation);
                            Generic.WriteMessage($"Altimétrie : {ComputedValue["RAW_ALTIMETRIE"]}");
                            BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, NewPointLocation, ed.GetUSCRotation(AngleUnit.Radians), ComputedValue);
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

        private static double? GetSlopeValue()
        {
            Editor ed = Generic.GetEditor();
            PromptDoubleOptions pDoubleOpts = new PromptDoubleOptions("\nIndiquez un pourcentage de pente (chiffres négatifs pour descendre)")
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
