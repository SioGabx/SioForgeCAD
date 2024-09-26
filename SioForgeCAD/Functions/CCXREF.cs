using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
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
                Editor ed = Generic.GetEditor();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        const string SelectMessage = "\nVeuillez selectionner une côte dans une XREF";
                        var GetBlockInXref = CotePoints.GetBlockInXref(SelectMessage, null, out PromptStatus promptStatus);
                        if (promptStatus != PromptStatus.OK) { return; }
                        if (GetBlockInXref == null)
                        {
                            continue;
                        }
                        Points BlockPosition = GetBlockInXref.Points;
                        double Altimetrie = GetBlockInXref.Altitude;
                        if (Altimetrie == 0)
                        {
                            continue;
                        }
                        double USCRotation = ed.GetUSCRotation(AngleUnit.Radians);
                        string AltimetrieStr = CotePoints.FormatAltitude(Altimetrie);

                        Dictionary<string, string> AltimetrieValue = new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };
                        if (BlockPosition.SCG.IsThereABlockReference(Settings.BlocNameAltimetrie, AltimetrieStr, out var BlkFound) && BlkFound.Layer == Layers.GetCurrentLayerName())
                        {
                            Generic.WriteMessage("Un bloc ayant la même valeur existe déja à cette position");
                        }
                        else
                        {
                            Generic.WriteMessage($"L'altimétrie {AltimetrieStr} à été ajoutée sur le calque {Layers.GetCurrentLayerName()}");
                            Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, BlockPosition, USCRotation, AltimetrieValue);
                        }
                    }
                    finally
                    {
                        tr.Commit();
                    }
                }
            }
        }
    }
}
