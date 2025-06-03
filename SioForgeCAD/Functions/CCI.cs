using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public class CCI
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

                Line Line = new Line(FirstPointCote.Points.SCG, SecondPointCote.Points.SCG);
                ObjectId LineObjId = Line.AddToDrawing(252);


                bool isMultipleIndermediairePlacement = false;
                do
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        HightLighter.UnhighlightAll();
                        Generic.WriteMessage($"Pente : {ComputeValue(FirstPointCote.Points)["PENTE"]}");
                        string[] KeyWords;
                        if (!isMultipleIndermediairePlacement)
                        {
                            KeyWords = new string[] { "Multiple" };
                        }
                        else
                        {
                            KeyWords = Array.Empty<string>();
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
                            foreach (var item in jig.StaticEntities.ToList())
                            {
                                if (item is Polyline polyline)
                                {
                                    polyline.SetPointAt(1, CurrentPoint.SCG.ToPoint2d());
                                }
                                if (item is Line ln)
                                {
                                    jig.StaticEntities.Remove(ln);
                                    ln.Dispose();
                                }

                            }
                            var Center = Arythmetique.FindDistanceToAltitudeBetweenTwoPoint(FirstPointCote, SecondPointCote, Convert.ToDouble(Values["ALTIMETRIE"]));
                            jig.StaticEntities.AddRange(GetScale(Center.Points, Line, Convert.ToDouble(Values["DISTANCEPERCM"]), 50));
                            return true;
                        }

                        var Pe = Polylines.GetPolylineFromPoints(FirstPointCote.Points, SecondPointCote.Points, SecondPointCote.Points);
                        Pe.Color = Colors.GetTransGraphicsColor(null, true);
                        using (var GetPointJig = new GetPointJig()
                        {
                            Entities = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(FirstPointCote.Points)),
                            StaticEntities = new DBObjectCollection() { Pe },
                            UpdateFunction = UpdateFunction
                        })
                        {
                            var GetPointTransientResult = GetPointJig.GetPoint("Indiquez les emplacements des points cote", KeyWords);

                            if (GetPointTransientResult.Point != null && GetPointTransientResult.PromptPointResult.Status == PromptStatus.OK)
                            {
                                var ComputedValue = ComputeValue(GetPointTransientResult.Point);
                                Generic.WriteMessage($"Altimétrie : {ComputedValue["RAW_ALTIMETRIE"]}");
                                BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, GetPointTransientResult.Point, ed.GetUSCRotation(AngleUnit.Radians), ComputedValue);
                            }
                            else if (GetPointTransientResult.PromptPointResult.Status == PromptStatus.Keyword)
                            {
                                isMultipleIndermediairePlacement = true;
                            }
                            else
                            {
                                isMultipleIndermediairePlacement = false;
                            }
                            if (!isMultipleIndermediairePlacement)
                            {
                                LineObjId.EraseObject();
                            }
                        }
                        tr.Commit();
                    }
                } while (isMultipleIndermediairePlacement);
            }
        }

        private static DBObjectCollection GetScale(Points center, Line baseLine, double segmentSpacing, int nbSegments)
        {
            if (nbSegments % 2 == 0) { nbSegments++; }
            var collection = new DBObjectCollection();
            if (segmentSpacing == 0) { return collection; }

            Vector3d baseDir = baseLine.GetVector3d();
            Vector3d perpDir = baseDir.GetPerpendicularVector();

            const double segmentHalfLength = 0.01;

            for (int i = 0; i < nbSegments; i++)
            {
                double offset = (i - ((nbSegments - 1) / 2.0)) * segmentSpacing;
                Point3d centerPoint = center.SCG.Add(baseDir.MultiplyBy(offset));

                // Check if the projected point lies within the limits of the segment
                Vector3d vecFromStart = centerPoint - baseLine.StartPoint;
                double projectedLength = vecFromStart.DotProduct(baseDir);

                if (projectedLength < 0 || projectedLength > baseLine.Length)
                {
                    continue; // skip
                }


                Point3d p1 = centerPoint.Add(perpDir.Negate().MultiplyBy(segmentHalfLength));
                Point3d p2 = centerPoint.Add(perpDir.MultiplyBy(segmentHalfLength));

                Line segment = new Line(p1, p2)
                {
                    Color = Colors.GetTransGraphicsColor(null, true)
                };
                collection.Add(segment);
            }

            return collection;
        }


        public Dictionary<string, string> ComputeValue(Points Intermediaire)
        {
            var ComputeSlopeAndIntermediate = Arythmetique.ComputeSlopeAndIntermediate(FirstPointCote, SecondPointCote, Intermediaire);
            double Altitude = ComputeSlopeAndIntermediate.Altitude;
            double Slope = ComputeSlopeAndIntermediate.Slope;

            //Compute DISTANCEPERCM
            double DistancePerCm = 0;
            double AltitudeDifference = Math.Abs(FirstPointCote.Altitude - SecondPointCote.Altitude);
            double DistanceBetweenPoints = FirstPointCote.Points.SCG.DistanceTo(SecondPointCote.Points.SCG);
            if (AltitudeDifference > 0 && DistanceBetweenPoints > 0)
            {
                DistancePerCm = DistanceBetweenPoints / (AltitudeDifference / 0.01);
            }

            return new Dictionary<string, string>() {
                {"ALTIMETRIE", CotePoints.FormatAltitude(Altitude) },
                {"RAW_ALTIMETRIE", CotePoints.FormatAltitude(Altitude, 3) },
                {"DISTANCEPERCM", DistancePerCm.ToString() },
                {"PENTE", $"{Slope}%" },
            };
        }
    }
}
