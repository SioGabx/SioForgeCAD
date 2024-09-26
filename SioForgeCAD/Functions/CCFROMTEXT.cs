using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class CCFROMTEXT
    {
        public static void CreateCotationBlocFromText()
        {
            while (true)
            {
                Database db = Generic.GetDatabase();
                Editor ed = Generic.GetEditor();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        const string SelectMessage = "\nVeuillez selectionner une côte inscrite dans un texte";
                        var PromptEntOption = new PromptEntityOptions(SelectMessage) { AllowNone = false, AllowObjectOnLockedLayer = true };
                        PromptEntOption.SetRejectMessage(SelectMessage);
                        PromptEntOption.AddAllowedClass(typeof(DBText), true);
                        PromptEntOption.AddAllowedClass(typeof(MText), true);
                        PromptEntOption.AddAllowedClass(typeof(AttributeDefinition), true);

                        var promptStatus = ed.GetEntity(PromptEntOption);
                        if (promptStatus.Status != PromptStatus.OK) { return; }
                        if (promptStatus.ObjectId == ObjectId.Null)
                        {
                            continue;
                        }
                        Entity Ent = promptStatus.ObjectId.GetEntity();

                        string Text = "";
                        Point3d Location = Point3d.Origin;
                        if (Ent is DBText DBTextEnt)
                        {
                            Text = DBTextEnt.TextString;
                            Location = DBTextEnt.Position;
                        }
                        else if (Ent is AttributeDefinition AttributeDefinitionEnt)
                        {
                            Text = AttributeDefinitionEnt.TextString;
                            Location = AttributeDefinitionEnt.Position;
                        }
                        else if (Ent is MText MTextEnt)
                        {
                            Text = MTextEnt.Text;
                            Location = MTextEnt.Location;
                        }
                        double Altimetrie = GetDoubleInString(Text);

                        if (Altimetrie == 0)
                        {
                            continue;
                        }

                        string AltimetrieStr = CotePoints.FormatAltitude(Altimetrie);

                        Dictionary<string, string> ComputeValue(Points _) => new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };

                        DBObjectCollection ents = BlockReferences.InitForTransient(Settings.BlocNameAltimetrie, ComputeValue(null));
                        GetPointTransient insertionTransientPoints = new GetPointTransient(ents, ComputeValue);
                        var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez l'emplacements du point", Location.ToPoints());
                        Points NewPointLocation = InsertionTransientPointsValues.Point;
                        PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                        if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                        {
                            return;
                        }
                        BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, NewPointLocation, ed.GetUSCRotation(AngleUnit.Radians), ComputeValue(NewPointLocation));
                    }
                    finally
                    {
                        tr.Commit();
                    }
                }
            }
        }

        private static double GetDoubleInString(string Text)
        {
            if (double.TryParse(Text.Trim(), out double Altimetrie))
            {
                return Altimetrie;
            }
            else
            {
                double? ExtractedAltitude = CotePoints.ExtractDoubleInStringFromPoint(Text.Trim());
                return ExtractedAltitude ?? 0;
            }
        }
    }
}
