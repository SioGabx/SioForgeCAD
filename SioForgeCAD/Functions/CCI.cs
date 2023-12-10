using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

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
                Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;
                var db = doc.Database;
                FirstPointCote = Commun.CotePoints.GetCotePoints("Selectionnez un premier point", null);
                if (CotePoints.NullPointExit(FirstPointCote)) { return; }
                SecondPointCote = Commun.CotePoints.GetCotePoints("Selectionnez un deuxième point", FirstPointCote.Points);
                if (CotePoints.NullPointExit(SecondPointCote)) { return; }
                ObjectId Line = Commun.Drawing.Lines.Draw(FirstPointCote.Points, SecondPointCote.Points, 252);

                bool isMultipleIndermediairePlacement = false;
                do
                {
                    DBObjectCollection ents = Commun.Drawing.BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(FirstPointCote.Points));
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
                            KeyWords = new string[] { };
                        }
                        var InsertionTransientPointsValues = insertionTransientPoints.GetInsertionPoint("\nIndiquez les emplacements des points cote", Points.Null, KeyWords);
                        Points Indermediaire = InsertionTransientPointsValues.Point;
                        PromptPointResult IndermediairePromptPointResult = InsertionTransientPointsValues.PromptPointResult;
                        PromptStatus IndermediairePromptPointResultStatus = IndermediairePromptPointResult.Status;

                        if (Indermediaire != null && IndermediairePromptPointResultStatus == PromptStatus.OK)
                        {
                            Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, Indermediaire, Generic.GetUSCRotation(Generic.AngleUnit.Radians), ComputeValue(Indermediaire));
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
                {"ALTIMETRIE", Altitude.ToString("#.00") },
                {"PENTE", $"{Slope}%" },
            };
        }
    }


    internal class CCIInsertionTransientPoints : InsertionTransientPoints
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

        public override int GetTransGraphicsColor(Entity Drawable)
        {
            if (Drawable is Polyline)
            {
                return Settings.TransientSecondaryColorIndex;
            }
            return base.GetTransGraphicsColor(Drawable);
        }


        public override void TransformEntities(Entity entity, Matrix3d mat)
        {
            if (entity is Polyline)
            {
                return;
            }
            base.TransformEntities(entity, mat);
        }

    }

}
