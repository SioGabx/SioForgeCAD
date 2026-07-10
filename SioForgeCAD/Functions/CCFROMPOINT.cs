using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class CCFROMPOINT
    {
        public static void CreateCotationBlocFromDbPoint()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    const string SelectMessage = "\nVeuillez sélectionner un point";

                    PromptEntityOptions promptEntityOptions = new PromptEntityOptions(SelectMessage)
                    {
                        AllowNone = false,
                        AllowObjectOnLockedLayer = true
                    };

                    promptEntityOptions.Keywords.Add("Multiples");
                    promptEntityOptions.AppendKeywordsToMessage = true;
                    promptEntityOptions.SetRejectMessage("\nSélectionnez uniquement des points.");
                    promptEntityOptions.AddAllowedClass(typeof(DBPoint), true);
                    promptEntityOptions.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult result = ed.GetEntity(promptEntityOptions);


                    // Sélection multiple
                    if (result.Status == PromptStatus.Keyword)
                    {
                        SelectionFilter filter = new SelectionFilter(
                            new TypedValue[]
                            {
                                new TypedValue((int)DxfCode.Start, "POINT")
                            });

                        PromptSelectionOptions selectionOptions = new PromptSelectionOptions
                        {
                            MessageForAdding = "\nSélectionnez les points"
                        };

                        PromptSelectionResult selection = ed.GetSelection(selectionOptions, filter);

                        if (selection.Status != PromptStatus.OK)
                            return;


                        foreach (ObjectId id in selection.Value.GetObjectIds())
                        {
                            DBPoint point = id.GetEntity() as DBPoint;

                            if (point == null)
                                continue;

                            InsertAltitudeBlock(point.Position, ed);
                        }

                        return;
                    }


                    // Sélection simple
                    if (result.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    Entity Ent = result.ObjectId.GetEntity();
                    Point3d Location = Point3d.Origin;
                    if (Ent is DBPoint dbPoint)
                    {
                        InsertAltitudeBlock(dbPoint.Position, ed);
                    }
                    else if (Ent is BlockReference blockReference)
                    {
                        if (blockReference.IsXref())
                        {
                            List<ObjectId> XrefObjectId;
                            (ObjectId[] XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) XrefSelection = SelectInXref.Select(SelectMessage, result.PickedPoint);
                            XrefObjectId = XrefSelection.XrefObjectId.ToList();
                            if (XrefSelection.PromptStatus == PromptStatus.OK && XrefSelection.SelectedObjectId != ObjectId.Null)
                            {
                                DBObject XrefObject = XrefSelection.SelectedObjectId.GetDBObject();
                                if (XrefObject is DBPoint dbPointInXref)
                                {
                                    InsertAltitudeBlock(dbPointInXref.Position.ProjectXrefPointToCurrentSpace(blockReference.ObjectId), ed);
                                }
                            }
                        }
                    }



                    
                }
                finally
                {
                    tr.Commit();
                }
            }
        }


        private static void InsertAltitudeBlock(Point3d position, Editor ed)
        {
            double altitude = position.Z;

            string altitudeString = CotePoints.FormatAltitude(altitude);


            Dictionary<string, string> values =
                new Dictionary<string, string>()
                {
                    {
                        "ALTIMETRIE",
                        altitudeString
                    }
                };


            BlockReferences.InsertFromNameImportIfNotExist(
                Settings.BlkAltimetry,
                nameof(Settings.BlkAltimetry),
                position.ToPoints(),
                ed.GetUSCRotation(AngleUnit.Radians),
                values
            );
        }
    }
}