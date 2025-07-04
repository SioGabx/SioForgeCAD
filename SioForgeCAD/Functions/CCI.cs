using Autodesk.AutoCAD.Colors;
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
                            if (jig.StaticEntities != null)
                            {
                                jig.StaticEntities.DeepDispose();
                            }
                            jig.StaticEntities = new DBObjectCollection();

                            var Pe = Polylines.GetPolylineFromPoints(FirstPointCote.Points, CurrentPoint, SecondPointCote.Points);
                            Pe.Color = Colors.GetTransGraphicsColor(null, true);
                            jig.StaticEntities.Add(Pe);
                            var Center = Arythmetique.FindDistanceToAltitudeBetweenTwoPoint(FirstPointCote, SecondPointCote, Convert.ToDouble(Values["ALTIMETRIE"]));
                            var Scale = GetScale(Center.Points, Line, Convert.ToDouble(Values["DISTANCEPERCM"]), 50);

                            jig.StaticEntities.AddRange(Scale);
                            return true;
                        }


                        using (var GetPointJig = new GetPointJig()
                        {
                            Entities = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(FirstPointCote.Points)),
                            StaticEntities = new DBObjectCollection(),
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
            const double segmentHalfLength = 0.01;

            Vector3d baseDir = baseLine.GetVector3d();
            Vector3d perpDir = baseDir.GetPerpendicularVector();

            var collection = new DBObjectCollection();
            if (segmentSpacing == 0) { return collection; }
            var MiddleScaleLine = GetScaleLine(baseLine.StartPoint.GetIntermediatePoint(baseLine.EndPoint, 50), segmentHalfLength * 2);
            var MiddleScalePolyline = MiddleScaleLine.ToPolyline();
            MiddleScalePolyline.ConstantWidth = segmentHalfLength * .25;
            MiddleScaleLine.Dispose();

            collection.Add(MiddleScalePolyline);
            for (int i = 0; i < nbSegments; i++)
            {
                double offset = (i - ((nbSegments - 1) / 2.0)) * segmentSpacing;
                Point3d centerPoint = center.SCG.Add(baseDir.MultiplyBy(offset));
                var Segment = GetScaleLine(centerPoint, segmentHalfLength);
                if (Segment != null)
                {
                    double distanceFromCenter = Math.Abs(i - ((nbSegments - 1) / 2.0));
                    double attenuation = 1 - (distanceFromCenter / ((nbSegments - 1) / 2.0)); // entre 0 (centre) et 1 (extrémité)

                    byte alpha = (byte)(attenuation * 200);
                    Segment.Transparency = new Transparency(alpha);

                    collection.Add(Segment);
                }
            }

            Line GetScaleLine(Point3d centerPoint, double Length)
            {
                // Check if the projected point lies within the limits of the segment
                Vector3d vecFromStart = centerPoint - baseLine.StartPoint;
                double projectedLength = vecFromStart.DotProduct(baseDir);

                if (projectedLength < 0 || projectedLength > baseLine.Length)
                {
                    return null;
                }
                //Create segment
                Point3d p1 = centerPoint.Add(perpDir.Negate().MultiplyBy(Length));
                Point3d p2 = centerPoint.Add(perpDir.MultiplyBy(Length));
                return new Line(p1, p2)
                {
                    Color = Colors.GetTransGraphicsColor(null, true)
                };
            }

            return collection;
        }

        public static Point3d GetClosestPointOnLines(Point3d referencePoint, DBObjectCollection dbObjectCollectif)
        {
            Point3d closestPoint = Point3d.Origin;
            double minDistance = double.MaxValue;

            foreach (DBObject obj in dbObjectCollectif)
            {
                if (obj is Line line)
                {
                    Point3d projectedPoint = line.StartPoint.GetMiddlePoint(line.EndPoint);
                    double distance = projectedPoint.DistanceTo(referencePoint);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = projectedPoint;
                    }
                }
            }

            return closestPoint;
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
