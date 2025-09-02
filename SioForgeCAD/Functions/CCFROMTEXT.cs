using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

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
                        PromptEntOption.Keywords.Add("Multiples");
                        PromptEntOption.AppendKeywordsToMessage = true;
                        PromptEntOption.SetRejectMessage(SelectMessage);
                        PromptEntOption.AddAllowedClass(typeof(DBText), true);
                        PromptEntOption.AddAllowedClass(typeof(MText), true);
                        PromptEntOption.AddAllowedClass(typeof(AttributeDefinition), true);
                        PromptEntOption.AddAllowedClass(typeof(ProxyEntity), false);

                        PromptEntOption.AddAllowedClass(typeof(BlockReference), true);

                        var promptStatus = ed.GetEntity(PromptEntOption);

                        if (promptStatus.Status == PromptStatus.Keyword)
                        {
                            //MULTIPLES
                            SelectionFilter filterList = new SelectionFilter(new TypedValue[] {
                                new TypedValue((int)DxfCode.Operator, "<or"),
                                new TypedValue((int)DxfCode.Start, "DBTEXT"),
                                new TypedValue((int)DxfCode.Start, "MTEXT"),
                                new TypedValue((int)DxfCode.Start, "ATTDEF"),
                                new TypedValue((int)DxfCode.Operator, "or>"),
                            });
                            var promptSelectionOptions = new PromptSelectionOptions();
                            promptSelectionOptions.MessageForAdding = "\nVeuillez selectionner des côtes inscrite dans des textes";
                            var sel = ed.GetSelection(promptSelectionOptions, filterList);
                            if (sel.Status != PromptStatus.OK) { return; }

                            foreach (var item in sel.Value.GetObjectIds())
                            {
                                Entity Ent = item.GetEntity();
                                if (Ent is DBText || Ent is AttributeDefinition || Ent is MText || Ent is ProxyEntity)
                                {
                                    var Values = GetAltitudeInObject(Ent);
                                    string Text = Values.Text;
                                    Point3d Location = Values.Location;

                                    double Altimetrie = GetDoubleInString(Text);

                                    if (Altimetrie == 0)
                                    {
                                        continue;
                                    }
                                    string AltimetrieStr = CotePoints.FormatAltitude(Altimetrie);
                                    BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, Location.ToPoints(), ed.GetUSCRotation(AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } });
                                }
                            }
                            return;
                        }
                        else if (promptStatus.Status != PromptStatus.OK) { return; }
                        else
                        {
                            //SINGLE TEXT
                            if (promptStatus.ObjectId == ObjectId.Null)
                            {
                                continue;
                            }
                            Entity Ent = promptStatus.ObjectId.GetEntity();

                            string Text = "";
                            Point3d Location = Point3d.Origin;
                            if (Ent is DBText || Ent is AttributeDefinition || Ent is MText || Ent is ProxyEntity)
                            {
                                var Values = GetAltitudeInObject(Ent);
                                Text = Values.Text;
                                Location = Values.Location;
                            }
                            else if (Ent is BlockReference blockReference)
                            {
                                if (blockReference.IsXref())
                                {
                                    List<ObjectId> XrefObjectId;
                                    (ObjectId[] XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) XrefSelection = SelectInXref.Select(SelectMessage, promptStatus.PickedPoint);
                                    XrefObjectId = XrefSelection.XrefObjectId.ToList();
                                    if (XrefSelection.PromptStatus == PromptStatus.OK && XrefSelection.SelectedObjectId != ObjectId.Null)
                                    {
                                        DBObject XrefObject = XrefSelection.SelectedObjectId.GetDBObject();
                                        var Values = GetAltitudeInObject(XrefObject);
                                        Text = Values.Text;
                                        Location = Points.ToSCGFromCurentSCU(promptStatus.PickedPoint);
                                    }
                                }
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
                            var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez l'emplacements du point", Location.ToPoints(), false);
                            Points NewPointLocation = InsertionTransientPointsValues.Point;
                            PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                            if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                            {
                                return;
                            }
                            BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, NewPointLocation, ed.GetUSCRotation(AngleUnit.Radians), ComputeValue(NewPointLocation));
                            return;
                        }
                    }
                    finally
                    {
                        tr.Commit();
                    }
                }
            }
        }


        public static (string Text, Point3d Location) GetAltitudeInObject(DBObject Object)
        {
            if (Object is DBText DBTextEnt)
            {
                return (DBTextEnt.TextString, DBTextEnt.Position);
            }
            else if (Object is AttributeDefinition AttributeDefinitionEnt)
            {
                return (AttributeDefinitionEnt.TextString, AttributeDefinitionEnt.Position);
            }
            else if (Object is MText MTextEnt)
            {
                return (MTextEnt.Text, MTextEnt.Location);
            }
            else if (Object is ProxyEntity ProxyEnt)
            {
                var ProxyInnerEnts = new DBObjectCollection();
                ProxyEnt.Explode(ProxyInnerEnts);

                var PossiblesValues = new List<(string Text, Point3d Location)>();
                foreach (DBObject ProxyInnerEnt in ProxyInnerEnts)
                {
                    var Values = GetAltitudeInObject(ProxyInnerEnt);
                    if (!(string.IsNullOrEmpty(Values.Text)))
                    {
                        PossiblesValues.Add(Values);
                    }
                    ProxyInnerEnt.Dispose();
                }

                if (PossiblesValues.Count == 1)
                {
                    return PossiblesValues.First();
                }
                else if (PossiblesValues.Count > 1)
                {
                    var Possible = Generic.GetEditor().GetKeywords("Plusieurs valeurs possible trouvées :", PossiblesValues.Select(p => p.Text).Distinct().ToArray());
                    if (Possible.Status == PromptStatus.OK)
                    {
                        return PossiblesValues.FirstOrDefault(p => p.Text == Possible.StringResult);
                    }
                }
            }
            return (string.Empty, Point3d.Origin);
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
