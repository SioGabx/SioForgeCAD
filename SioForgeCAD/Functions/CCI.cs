using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
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
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        HightLighter.UnhighlightAll();
                        ObjectId blockRef = CotationElements.InsertBlocFromBlocName("_APUd_COTATIONS_Altimetries", Points.Empty, Generic.GetUSCRotation(Generic.AngleUnit.Radians));
                        DBObjectCollection ents = Generic.Explode(new List<ObjectId>() { blockRef });
                        InsertionTransientPoints insertionTransientPoints = new InsertionTransientPoints(ents, ComputeValue);
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
                            var Values = ComputeValue(Indermediaire);
                            ed.WriteMessage($"Pente : {Values["PENTE"]}\n");
                            CotationElements.InsertBlocFromBlocName("_APUd_COTATIONS_Altimetries", Indermediaire, Generic.GetUSCRotation(Generic.AngleUnit.Radians), Values);
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
