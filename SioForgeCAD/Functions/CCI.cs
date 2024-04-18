using Autodesk.AutoCAD.Colors;
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
                FirstPointCote = Commun.CotePoints.GetCotePoints("Selectionnez un premier point", null);
                if (CotePoints.NullPointExit(FirstPointCote)) { return; }
                SecondPointCote = Commun.CotePoints.GetCotePoints("Selectionnez un deuxième point", FirstPointCote.Points);
                if (CotePoints.NullPointExit(SecondPointCote)) { return; }
                ObjectId Line = Commun.Drawing.Lines.Draw(FirstPointCote.Points, SecondPointCote.Points, 252);

                bool isMultipleIndermediairePlacement = false;
                do
                {
                    DBObjectCollection ents = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(FirstPointCote.Points));
                    ents.Insert(0, Polylines.GetPolylineFromPoints(FirstPointCote.Points, SecondPointCote.Points, SecondPointCote.Points));
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        HightLighter.UnhighlightAll();
                        CCIInsertionTransientPoints insertionTransientPoints = new CCIInsertionTransientPoints(ents, ComputeValue);
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
                        var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez les emplacements des points cote", Points.Null, KeyWords);
                        Points Indermediaire = InsertionTransientPointsValues.Point;
                        PromptPointResult IndermediairePromptPointResult = InsertionTransientPointsValues.PromptPointResult;
                        PromptStatus IndermediairePromptPointResultStatus = IndermediairePromptPointResult.Status;

                        if (Indermediaire != null && IndermediairePromptPointResultStatus == PromptStatus.OK)
                        {
                            var ComputedValue = ComputeValue(Indermediaire);
                            Generic.WriteMessage($"Altimétrie : {ComputedValue["RAW_ALTIMETRIE"]}");
                            BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, Indermediaire, ed.GetUSCRotation(AngleUnit.Radians), ComputedValue);
                        }
                        else if (IndermediairePromptPointResultStatus == PromptStatus.Keyword)
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
            return new Dictionary<string, string>() {
                {"ALTIMETRIE", CotePoints.FormatAltitude(Altitude) },
                {"RAW_ALTIMETRIE", CotePoints.FormatAltitude(Altitude, 3) },
                {"PENTE", $"{Slope}%" },
            };
        }
    }

    internal class CCIInsertionTransientPoints : GetPointTransient
    {
        public CCIInsertionTransientPoints(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction) { }

        public override void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
        {
            foreach (var ent in Drawable)
            {
                if (ent is Polyline polyline)
                {
                    polyline.SetPointAt(1, moveToPt.ToPoint2d());
                }
            }
            base.UpdateTransGraphics(curPt, moveToPt);
        }

        public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
        {
            if (Drawable is Polyline)
            {
                return Color.FromColorIndex(ColorMethod.ByColor, (short)Settings.TransientSecondaryColorIndex);
            }
            return base.GetTransGraphicsColor(Drawable, IsStaticDrawable);
        }

        public override void TransformEntities(Entity entity, Point3d currentPoint, Point3d destinationPoint)
        {
            if (entity is Polyline)
            {
                return;
            }
            base.TransformEntities(entity, currentPoint, destinationPoint);
        }
    }
}
