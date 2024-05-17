using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Windows.Shapes;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Functions
{
    public static class TLEN
    {
        public static void Compute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();

            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                AllSelectedObject = ed.GetCurves("\nVeuillez sélectionner les courbes à mesurer", false, false);
            }

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }

            var AllSelectedObjectIds = AllSelectedObject.Value.GetObjectIds();
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                double TotalLength = 0;
                foreach (ObjectId ObjId in AllSelectedObjectIds)
                {
                    var Ent = ObjId.GetDBObject();
                    if (Ent is Curve CurveEnt && !(CurveEnt is Ray || CurveEnt is Xline))
                    {
                        TotalLength += CurveEnt.GetDistanceAtParameter(CurveEnt.EndParam);
                    }else if (Ent is Region Reg)
                    {
                        TotalLength += Reg.Perimeter;
                    }
                }
                short DisplayPrecision = (short)Application.GetSystemVariable("LUPREC");
                var Message = $"La longueur totale des courbes sélectionnées est égale à {Math.Round(TotalLength, DisplayPrecision)}";
                Generic.WriteMessage(Message);
                Application.ShowAlertDialog(Message);
                System.Windows.Clipboard.SetText(TotalLength.ToString());
                ed.SetImpliedSelection(AllSelectedObjectIds);
                tr.Commit();
            }
        }


    }
}
