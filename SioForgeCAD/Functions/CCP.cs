using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
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

                ObjectId Line = ObjectId.Null;
                using (Line acLine = new Line(FirstPointCote.Points.SCG, SecondPointCote.Points.SCG))
                {
                    acLine.ColorIndex = 252;
                    Line = acLine.AddToDrawing();
                }

                var Values = ComputeValue();
                DBObjectCollection ents = Commun.Drawing.BlockReferences.InitForTransient(Settings.BlkSlopePercentage, nameof(Settings.BlkSlopePercentage), Values);
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
                            Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(Settings.BlkSlopePercentage, nameof(Settings.BlkSlopePercentage), Indermediaire, ed.GetUSCRotation(AngleUnit.Radians), Values);
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

        //private (string AnglePente, string SensPente) GetPenteBlocSettings()
        //{
        //    double NormalizeDegrees(double AngleInDegreesToNormalize)
        //    {
        //        double NormalizedAngle = AngleInDegreesToNormalize;
        //        if (NormalizedAngle > 360)
        //        {
        //            NormalizedAngle -= 360;
        //        }
        //        else if (NormalizedAngle < 0)
        //        {
        //            NormalizedAngle = 360 - AngleInDegreesToNormalize;
        //        }
        //        return NormalizedAngle;
        //    }

        //    double PointsAngleVectorInRadians = 0;

        //    Point3d StartPoint;
        //    Point3d EndPoint;

        //    Point3d FirstPointCoteLocation = FirstPointCote.Points.SCG.Flatten();
        //    Point3d SecondPointCoteLocation = SecondPointCote.Points.SCG.Flatten();
        //    if (FirstPointCote.Altitude > SecondPointCote.Altitude)
        //    {
        //        StartPoint = FirstPointCoteLocation;
        //        EndPoint = SecondPointCoteLocation;
        //    }
        //    else
        //    {
        //        StartPoint = SecondPointCoteLocation;
        //        EndPoint = FirstPointCoteLocation;
        //    }

        //    using (Line acLine = new Line(StartPoint, EndPoint))
        //    {
        //        try
        //        {
        //            PointsAngleVectorInRadians = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(StartPoint), Vector3d.ZAxis);
        //        }
        //        catch (Exception)
        //        {
        //            PointsAngleVectorInRadians = -1;
        //        }
        //    }

        //    int BlocInverseState = 0;

        //    double DegreesAngles = PointsAngleVectorInRadians * 180 / Math.PI;
        //    double USCRotation = Generic.GetEditor().GetUSCRotation(AngleUnit.Degrees);
        //    DegreesAngles -= 1 * USCRotation;
        //    if (DegreesAngles > 90 + USCRotation && DegreesAngles < 270 + USCRotation)
        //    {
        //        if (BlocInverseState == 0)
        //        {
        //            BlocInverseState = 1;
        //        }
        //        else
        //        {
        //            BlocInverseState = 0;
        //        }

        //        DegreesAngles += 180;
        //        DegreesAngles = NormalizeDegrees(DegreesAngles);
        //    }
        //    double RadiansAngle = DegreesAngles * Math.PI / 180;
        //    return (RadiansAngle.ToString(), BlocInverseState.ToString());
        //}

        // Fonction principale qui orchestre la logique
        private (string AnglePente, string SensPente) GetPenteBlocSettings()
        {
            // 1. Déterminer les points de départ et de fin en fonction de l'altitude
            (Point3d startPoint, Point3d endPoint) = GetOrderedPointsByAltitude(FirstPointCote, SecondPointCote);

            // 2. Calculer l'angle brut du vecteur en radians
            double angleInRadians = CalculateVectorAngle(startPoint, endPoint);

            // 4. Ajuster l'angle et déterminer si le bloc doit être inversé
            (double finalRadians, int blocInverseState) = AdjustAngleAndInversion(angleInRadians);

            return (finalRadians.ToString(), blocInverseState.ToString());
        }

        private static (Point3d StartPoint, Point3d EndPoint) GetOrderedPointsByAltitude(CotePoints pt1, CotePoints pt2)
        {
            Point3d loc1 = pt1.Points.SCG.Flatten();
            Point3d loc2 = pt2.Points.SCG.Flatten();

            // Le point le plus haut devient le StartPoint
            if (pt1.Altitude > pt2.Altitude)
            {
                return (loc1, loc2);
            }
            return (loc2, loc1);
        }

        private static double CalculateVectorAngle(Point3d startPoint, Point3d endPoint)
        {
            try
            {
                Vector3d direction = endPoint - startPoint;
                return Vector3d.XAxis.GetAngleTo(direction, Vector3d.ZAxis);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static (double AdjustedAngleRadians, int BlocInverseState) AdjustAngleAndInversion(double radiansAngle)
        {
            int blocInverseState = 0;
            double degreesAngle = radiansAngle * (180.0 / Math.PI);

            double uscRotation = Generic.GetEditor().GetUSCRotation(AngleUnit.Degrees);

            degreesAngle -= uscRotation;

            // Si le texte/bloc se retrouve à l'envers par rapport à la vue
            if (degreesAngle > (90 + uscRotation) && degreesAngle < (270 + uscRotation))
            {
                blocInverseState = 1; // Simplification du toggle 0/1
                degreesAngle += 180;
                degreesAngle = NormalizeDegrees(degreesAngle);
            }

            double finalRadians = degreesAngle * (Math.PI / 180.0);
            return (finalRadians, blocInverseState);
        }

        private static double NormalizeDegrees(double angleInDegrees)
        {
            // Normalisation d'angle plus robuste (ramène n'importe quel angle entre 0 et 360)
            double normalizedAngle = angleInDegrees % 360.0;
            if (normalizedAngle < 0)
            {
                normalizedAngle += 360.0;
            }
            return normalizedAngle;
        }
    }
}
