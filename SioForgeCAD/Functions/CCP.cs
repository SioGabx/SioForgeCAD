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
    public class CCP
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
                DBObjectCollection ents = new DBObjectCollection();
                var Values = ComputeValue();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    ObjectId blockRef = CotationElements.InsertBlocFromBlocName("_APUd_COTATIONS_Pentes", Points.Empty, Generic.GetUSCRotation(Generic.AngleUnit.Radians), Values);
                    DBObject dBObject = blockRef.GetDBObject();
                    Generic.Erase(blockRef);
                    ents.Add(dBObject);
                    tr.Commit();
                    
                }
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
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
                        CotationElements.InsertBlocFromBlocName("_APUd_COTATIONS_Pentes", Indermediaire, Generic.GetUSCRotation(Generic.AngleUnit.Radians), Values);
                    }
                    
                    
                }

            }
        }
        public Dictionary<string, string> ComputeValue()
        {
            if (FirstPointCote?.Points?.SCU == null || SecondPointCote?.Points?.SCU == null)
            {
                return null;
            }
            var ComputeSlopeAndIntermediate = Arythmetique.ComputeSlopeAndIntermediate(FirstPointCote, SecondPointCote, Points.Empty);
            var PenteBlocSettings = GetPenteBlocSettings();
            double Slope = ComputeSlopeAndIntermediate.Slope;
            return new Dictionary<string, string>() {
                {"PENTE", $"{Slope}%" },
                {"ANGLE_PENTE", PenteBlocSettings.AnglePente },
                {"SENS_PENTE", PenteBlocSettings.SensPente },
            };
        }

        public (string AnglePente, string SensPente) GetPenteBlocSettings()
        {
            double deg_sup_to_normal(double func_anglef)
            {
                double func_anglef_return = func_anglef;
                if (func_anglef_return > 360)
                {
                    func_anglef_return -= 360;
                }
                else if (func_anglef_return < 0)
                {
                    func_anglef_return = 360 - func_anglef;
                }
                return func_anglef_return;
            }

            double angle = 0;

            using (Line acLine = new Line(FirstPointCote.Points.SCU, SecondPointCote.Points.SCU))
            {
                try
                {
                    angle = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(FirstPointCote.Points.SCU), Vector3d.ZAxis);
                }
                catch (System.Exception)
                {
                    angle = -1;
                }
            }

            double anglef = angle;
            int invf = 0;
            if (SecondPointCote.Altitude > FirstPointCote.Altitude)
            {
                invf = 1;
            }
            double angle_degres = angle * 180 / Math.PI;

            double USCRotation = Generic.GetUSCRotation(Generic.AngleUnit.Degrees);
            if (angle_degres > 90 + USCRotation && angle_degres < 270 + USCRotation)
            {
                if (invf == 0)
                {
                    invf = 1;
                }
                else
                {
                    invf = 0;
                }

                anglef = angle_degres + 180;
                anglef = deg_sup_to_normal(anglef);
                anglef = anglef * Math.PI / 180;
            }
            return (anglef.ToString(), invf.ToString());
        }
    }
}
