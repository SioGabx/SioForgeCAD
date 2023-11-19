using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
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
                ObjectId Line = Commun.Drawing.Lines.SingleLine(FirstPointCote.Points, SecondPointCote.Points, 252);

                bool isMultipleIndermediairePlacement = false;
                do
                {
                    DBObjectCollection ents = CotationElements.InitBlocForTransient(Settings.BlocNameAltimetrie, ComputeValue(FirstPointCote.Points));

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        HightLighter.UnhighlightAll();

                        InsertionTransientPoints insertionTransientPoints = new InsertionTransientPoints(ents, ComputeValue);

                        ed.WriteMessage($"Pente : {ComputeValue(FirstPointCote.Points)["PENTE"]}\n");
                        string[] KeyWords;
                        if (!isMultipleIndermediairePlacement)
                        {
                            KeyWords = new string[] { "Multiple" };
                        }
                        else
                        {
                            KeyWords = new string[] { };
                        }
                        var InsertionTransientPointsValues = insertionTransientPoints.GetInsertionPoint("Indiquer les emplacements des points côte", KeyWords);
                        Points Indermediaire = InsertionTransientPointsValues.Point;
                        PromptPointResult IndermediairePromptPointResult = InsertionTransientPointsValues.PromptPointResult;
                        PromptStatus IndermediairePromptPointResultStatus = IndermediairePromptPointResult.Status;

                        if (Indermediaire != null && IndermediairePromptPointResultStatus == PromptStatus.OK)
                        {
                            CotationElements.InsertBlocFromBlocName(Settings.BlocNameAltimetrie, Indermediaire, Generic.GetUSCRotation(Generic.AngleUnit.Radians), ComputeValue(Indermediaire));
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
                            Generic.Erase(Line);
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
}
