using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public class CCP
    {
        private CotePoints FirstPointCote;
        private CotePoints SecondPointCote;
        public void Compute()
        {
            while (true)
            {
                Document doc = AcAp.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;
                var db = doc.Database;
                FirstPointCote = Commun.CotePoints.GetCotePoints("Selectionnez un premier point", null);
                if (CotePoints.NullPointExit(FirstPointCote)) { return; }
                SecondPointCote = Commun.CotePoints.GetCotePoints("Selectionnez un deuxième point", FirstPointCote.Points);
                if (CotePoints.NullPointExit(SecondPointCote)) { return; }
                ObjectId Line = Commun.Drawing.Lines.Draw(FirstPointCote.Points, SecondPointCote.Points, 252);

                var Values = ComputeValue();
                DBObjectCollection ents = CotationElements.InitBlocForTransient(Settings.BlocNamePente, Values);
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    InsertionTransientPoints insertionTransientPoints = new InsertionTransientPoints(ents, (_) => { return null; });
                    var InsertionTransientPointsValues = insertionTransientPoints.GetInsertionPoint("Indiquer l'emplacement du bloc pente à ajouter");
                    Points Indermediaire = InsertionTransientPointsValues.Point;
                    PromptPointResult IndermediairePromptPointResult = InsertionTransientPointsValues.PromptPointResult;
                    PromptStatus IndermediairePromptPointResultStatus = IndermediairePromptPointResult.Status;
                    Generic.Erase(Line);
                    tr.Commit();
                    if (Indermediaire != null && IndermediairePromptPointResultStatus == PromptStatus.OK)
                    {
                        ed.WriteMessage($"Pente : {Values["PENTE"]}\n");
                        CotationElements.InsertBlocFromBlocName(Settings.BlocNamePente, Indermediaire, Generic.GetUSCRotation(Generic.AngleUnit.Radians), Values);
                    }
                }
            }
        }

        private Dictionary<string, string> ComputeValue()
        {
            if (FirstPointCote?.Points?.SCU == null || SecondPointCote?.Points?.SCU == null)
            {
                return null;
            }
            var ComputeSlopeAndIntermediate = Arythmetique.ComputeSlopeAndIntermediate(FirstPointCote, SecondPointCote, Points.Empty);
            var (AnglePente, SensPente) = GetPenteBlocSettings();
            double Slope = ComputeSlopeAndIntermediate.Slope;
            return new Dictionary<string, string>() {
                {"PENTE", $"{Slope}%" },
                {"ANGLE_PENTE", AnglePente },
                {"SENS_PENTE", SensPente },
            };
        }

        private (string AnglePente, string SensPente) GetPenteBlocSettings()
        {
            double NormalizeDegrees(double AngleInDegreesToNormalize)
            {
                double NormalizedAngle = AngleInDegreesToNormalize;
                if (NormalizedAngle > 360)
                {
                    NormalizedAngle -= 360;
                }
                else if (NormalizedAngle < 0)
                {
                    NormalizedAngle = 360 - AngleInDegreesToNormalize;
                }
                return NormalizedAngle;
            }

            double PointsAngleVectorInRadians = 0;

            using (Line acLine = new Line(FirstPointCote.Points.SCU, SecondPointCote.Points.SCU))
            {
                try
                {
                    PointsAngleVectorInRadians = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(FirstPointCote.Points.SCU), Vector3d.ZAxis);
                }
                catch (System.Exception)
                {
                    PointsAngleVectorInRadians = -1;
                }
            }

            int BlocInverseState = 0;

            double DegreesAngles = PointsAngleVectorInRadians * 180 / Math.PI;
            if (SecondPointCote.Altitude > FirstPointCote.Altitude)
            {
                BlocInverseState = 1;
            }

            double USCRotation = Generic.GetUSCRotation(Generic.AngleUnit.Degrees);
            DegreesAngles -= 2 * USCRotation;
            if (DegreesAngles > 90 + USCRotation && DegreesAngles < 270 + USCRotation)
            {
                if (BlocInverseState == 0)
                {
                    BlocInverseState = 1;
                }
                else
                {
                    BlocInverseState = 0;
                }

                DegreesAngles += 180;
                DegreesAngles = NormalizeDegrees(DegreesAngles);
            }
            double RadiansAngle = DegreesAngles * Math.PI / 180;
            return (RadiansAngle.ToString(), BlocInverseState.ToString());
        }
    }
}
