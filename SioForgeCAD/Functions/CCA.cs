using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
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
                CotePoints PointCote = CotePoints.GetCotePoints("Selectionnez un point", null);
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
                bool UpdateFunction(Points CurrentPoint, GetPointJig jig)
                {
                    var Values = ComputeValue(CurrentPoint);
                    foreach (var item in jig.Entities)
                    {
                        if (item is BlockReference blkRef)
                        {
                            blkRef.SetAttributeValues(Values);
                        }
                    }
                    return true;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();


                    var GetPointJig = new GetPointJig()
                    {
                        Entities = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(PointCote.Points)),
                        StaticEntities = new DBObjectCollection(),
                        UpdateFunction = UpdateFunction
                    };

                    string Signe = string.Empty;
                    if (StepValue > 0)
                    {
                        Signe = "+";
                    }
                    var GetPointTransientResult = GetPointJig.GetPoint("Indiquez les emplacements des points cote", $"{Altitude - StepValue}{Signe}{StepValue}");

                    if (GetPointTransientResult.Point == null || GetPointTransientResult.PromptPointResult.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return;
                    }
                    BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, GetPointTransientResult.Point, ed.GetUSCRotation(AngleUnit.Radians), ComputeValue(GetPointTransientResult.Point));
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
