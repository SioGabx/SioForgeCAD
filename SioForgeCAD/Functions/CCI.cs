using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System.Diagnostics;

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
                ObjectId Line = Lines.Draw(FirstPointCote.Points, SecondPointCote.Points, 252);

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
                            foreach (var item in jig.StaticEntities)
                            {
                                if (item is Polyline polyline)
                                {
                                    polyline.SetPointAt(1, CurrentPoint.SCG.ToPoint2d());
                                }
                                if (item is Circle circle)
                                {
                                    var Center = Arythmetique.FindDistanceToAltitudeBetweenTwoPoint(FirstPointCote, SecondPointCote, Convert.ToDouble(Values["ALTIMETRIE"]));
                                    if (Center == CotePoints.Null || Center?.Points == Points.Null)
                                    {
                                        circle.Visible = false;
                                    }
                                    else
                                    {
                                        circle.Visible = true;
                                        circle.Center = Center.Points.SCG;
                                    }
                                }
                            }
                            return true;
                        }


                        using (var GetPointJig = new GetPointJig()
                        {
                            Entities = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(FirstPointCote.Points)),
                            StaticEntities = new DBObjectCollection() {
                                Polylines.GetPolylineFromPoints(FirstPointCote.Points, SecondPointCote.Points, SecondPointCote.Points),
                                new Circle(Point3d.Origin, Vector3d.ZAxis, 0.01)
                            },
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
                                Line.EraseObject();
                            }
                        }
                        tr.Commit();
                    }
                } while (isMultipleIndermediairePlacement);
            }
        }


        public Dictionary<string, string> ComputeValue(Points Intermediaire)
        {
            var ComputeSlopeAndIntermediate = Arythmetique.ComputeSlopeAndIntermediate(FirstPointCote, SecondPointCote, Intermediaire);
            double Altitude = ComputeSlopeAndIntermediate.Altitude;
            double Slope = ComputeSlopeAndIntermediate.Slope;

            double DistancePerCm = -1;
            var DistancePerCmRatioPart = FirstPointCote.Points.SCG.DistanceTo(SecondPointCote.Points.SCG) * Math.Abs(FirstPointCote.Altitude - SecondPointCote.Altitude);
            if (DistancePerCmRatioPart > 0)
            {
                DistancePerCm = DistancePerCmRatioPart / 0.01;
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
