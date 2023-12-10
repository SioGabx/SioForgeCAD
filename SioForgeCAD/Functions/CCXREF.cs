using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class CCXREF
    {
        public static void MoveCotationFromXrefToCurrentDrawing()
        {
            while (true)
            {
                Database db = Generic.GetDatabase();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    string SelectMessage = "\nVeuillez selectionner une côte dans une XREF";
                    var GetBlockInXref = CotePoints.GetBlockInXref(SelectMessage, null);
                    if (GetBlockInXref == null)
                    {
                        tr.Commit();
                        return;
                    }
                    Points BlockPosition = GetBlockInXref.Points;
                    double Altimetrie = GetBlockInXref.Altitude;

                    double USCRotation = Generic.GetUSCRotation(Generic.AngleUnit.Radians);
                    string AltimetrieStr = CotePoints.FormatAltitude(Altimetrie);
                    Dictionary<string, string> AltimetrieValue = new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };
                    if (BlockReferenceExtensions.DoesBlockExist(BlockPosition.SCG, Settings.BlocNameAltimetrie, AltimetrieStr))
                    {
                        Generic.WriteMessage("Un block ayant la même valeur existe déja à cette position");
                    }
                    else
                    {
                        CotationElements.InsertBlocFromBlocName(Settings.BlocNameAltimetrie, BlockPosition, USCRotation, AltimetrieValue);
                    }

                    tr.Commit();
                }
            }
        }
    }

}
