﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

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
                Editor ed = Generic.GetEditor();
                Database db = Generic.GetDatabase();
                FirstPointCote = CotePoints.GetCotePoints("Selectionnez un premier point", null);
                if (CotePoints.NullPointExit(FirstPointCote)) { return; }
                SecondPointCote = CotePoints.GetCotePoints("Selectionnez un deuxième point", FirstPointCote.Points);
                if (CotePoints.NullPointExit(SecondPointCote)) { return; }
                ObjectId Line = Commun.Drawing.Lines.Draw(FirstPointCote.Points, SecondPointCote.Points, 252);

                var Values = ComputeValue();
                DBObjectCollection ents = Commun.Drawing.BlockReferences.InitForTransient(Settings.BlocNamePente, Values);
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    using (GetPointTransient insertionTransientPoints = new GetPointTransient(ents, null))
                    {
                        var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("Indiquer l'emplacement du bloc pente à ajouter", Points.Null, false);
                        Points Indermediaire = InsertionTransientPointsValues.Point;
                        PromptPointResult IndermediairePromptPointResult = InsertionTransientPointsValues.PromptPointResult;
                        PromptStatus IndermediairePromptPointResultStatus = IndermediairePromptPointResult.Status;
                        Line.EraseObject();
                        tr.Commit();
                        if (Indermediaire != null && IndermediairePromptPointResultStatus == PromptStatus.OK)
                        {
                            Generic.WriteMessage($"Pente : {Values["PENTE"]}");
                            Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNamePente, Indermediaire, ed.GetUSCRotation(AngleUnit.Radians), Values);
                        }
                    }
                }
            }
        }

        private Dictionary<string, string> ComputeValue()
        {
            if (FirstPointCote?.Points?.SCG == null || SecondPointCote?.Points?.SCG == null)
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

            Point3d StartPoint;
            Point3d EndPoint;

            Point3d FirstPointCoteLocation = FirstPointCote.Points.SCG.Flatten();
            Point3d SecondPointCoteLocation = SecondPointCote.Points.SCG.Flatten();
            if (FirstPointCote.Altitude > SecondPointCote.Altitude)
            {
                StartPoint = FirstPointCoteLocation;
                EndPoint = SecondPointCoteLocation;
            }
            else
            {
                StartPoint = SecondPointCoteLocation;
                EndPoint = FirstPointCoteLocation;
            }

            using (Line acLine = new Line(StartPoint, EndPoint))
            {
                try
                {
                    PointsAngleVectorInRadians = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(StartPoint), Vector3d.ZAxis);
                }
                catch (Exception)
                {
                    PointsAngleVectorInRadians = -1;
                }
            }

            int BlocInverseState = 0;

            double DegreesAngles = PointsAngleVectorInRadians * 180 / Math.PI;
            double USCRotation = Generic.GetEditor().GetUSCRotation(AngleUnit.Degrees);
            DegreesAngles -= 1 * USCRotation;
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
