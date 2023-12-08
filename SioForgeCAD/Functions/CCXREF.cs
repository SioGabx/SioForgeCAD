using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public static class CCXREF
    {
        public static void MoveCotationFromXrefToCurrentDrawing()
        {
            while (true)
            {
                var doc = AcAp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                var ed = doc.Editor;
                string SelectMessage = "Veuillez selectionner une côte dans une XREF";
                (ObjectId XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) XrefSelection = Commun.SelectInXref.Select(SelectMessage);
                if (XrefSelection.PromptStatus != PromptStatus.OK)
                {
                    return;
                }
                if (XrefSelection.SelectedObjectId == ObjectId.Null)
                {
                    continue;
                }
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    XrefSelection.SelectedObjectId.RegisterHighlight();
                    DBObject XrefObject = XrefSelection.SelectedObjectId.GetDBObject();
                    BlockReference blkRef = null;
                    if (XrefObject is AttributeReference blkChildAttribute)
                    {
                        var DbObj = blkChildAttribute.OwnerId.GetDBObject();
                        blkRef = DbObj as BlockReference;
                    }
                    else if (XrefObject is BlockReference)
                    {
                        blkRef = XrefObject as BlockReference;
                    }
                    if (blkRef is null)
                    {
                        continue;
                    }
                    double? Altimetrie = CotePoints.GetAltitudeFromBloc(blkRef);
                    if (Altimetrie == null)
                    {
                        Altimetrie = blkRef.Position.Z;
                        if (Altimetrie == 0)
                        {
                            continue;
                        }
                        PromptKeywordOptions options = new PromptKeywordOptions($"Aucune côte n'as été trouvée pour ce bloc, cependant une altitude Z à été définie à {CotePoints.FormatAltitude(Altimetrie)}.\nVoullez vous utilisez cette valeur ?");
                        options.Keywords.Add("OUI");
                        options.Keywords.Add("NON");
                        options.AllowNone = true;
                        PromptResult result = ed.GetKeywords(options);
                        if (result.Status == PromptStatus.OK && result.StringResult != "OUI")
                        {
                            continue;
                        }
                    }

                    

                    Points BlockPosition = Points.From3DPoint(BlockReferenceExtensions.ProjectPointToCurrentSpace(XrefSelection.XrefObjectId, blkRef.Position));
                    double USCRotation = Generic.GetUSCRotation(Generic.AngleUnit.Radians);
                    string AltimetrieStr = CotePoints.FormatAltitude(Altimetrie);
                    Dictionary<string, string> AltimetrieValue = new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };
                    if (BlockReferenceExtensions.DoesBlockExist(BlockPosition.SCG, Settings.BlocNameAltimetrie, AltimetrieStr))
                    {
                        ed.WriteMessage("Un block ayant la même valeur existe déja à cette position\n");
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
