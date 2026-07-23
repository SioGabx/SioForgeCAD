using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class CCFROMPOINT
    {
        public static void CreateCotationBlocFromDbPoint()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            List<Point3d> Points = GetVisiblePoints();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Dictionary<string, string> GetAttributes(double Altitude)
                    {
                        return new Dictionary<string, string>() {
                        { "ALTIMETRIE", CotePoints.FormatAltitude(Altitude) }
                    };
                    }

                    bool UpdateFunction(Point3d currentPoint, InputPointContext context, GetEntityJig jig)
                    {
                        Point3d nearest = Point3d.Origin;
                        double best = 1.0 * 1.0;

                        foreach (var p in Points)
                        {
                            double dx = p.X - currentPoint.X;
                            if (Math.Abs(dx) > 1.0)
                            {
                                continue;
                            }

                            double dy = p.Y - currentPoint.Y;
                            if (Math.Abs(dy) > 1.0)
                            {
                                continue;
                            }

                            double d2 = dx * dx + dy * dy;

                            if (d2 < best)
                            {
                                best = d2;
                                nearest = p;
                            }
                        }


                        //Debug.WriteLine("Update");
                        if (nearest == Point3d.Origin)
                        {
                            // Debug.WriteLine("Update null");
                            return true;
                        }
                        Debug.WriteLine(nearest.Z);
                        var values = GetAttributes(nearest.Z);
                        foreach (Entity entity in jig.StaticEntities)
                        {
                            if (entity is BlockReference blkRef)
                            {
                                // Position du bloc
                                blkRef.TransformBy(Matrix3d.Displacement(blkRef.Position.GetVectorTo(nearest)));

                                // Mise à jour des attributs
                                blkRef.SetAttributeValues(values);
                            }
                        }
                        jig.RegenTransients();
                        return true;
                    }

                    using (var GetEntJig = new GetEntityJig()
                    {
                        StaticEntities = BlockReferences.InitForTransient(Settings.BlkAltimetry, nameof(Settings.BlkAltimetry), GetAttributes(0)),
                        UpdateFunction = UpdateFunction
                    })
                    {
                        var JigResults = GetEntJig.GetEntity("Veuillez sélectionner un point", new System.Type[] { typeof(DBPoint), typeof(BlockReference) }, true, "Multiples");


                        // Sélection multiple
                        if (JigResults.Result.Status == PromptStatus.Keyword)
                        {
                            SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "POINT") });

                            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
                            {
                                MessageForAdding = "\nSélectionnez les points"
                            };

                            PromptSelectionResult selection = ed.GetSelection(selectionOptions, filter);

                            if (selection.Status != PromptStatus.OK)
                            {
                                return;
                            }

                            foreach (ObjectId id in selection.Value.GetObjectIds())
                            {
                                DBPoint point = id.GetEntity() as DBPoint;

                                if (point == null)
                                {
                                    continue;
                                }

                                InsertAltitudeBlock(point.Position, ed);
                            }

                            return;
                        }

                        // Sélection simple
                        if (JigResults.Result.Status != PromptStatus.OK)
                        {
                            return;
                        }

                        Entity Ent = JigResults.Entity;
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
                                (ObjectId[] XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) XrefSelection = SelectInXref.Select("", JigResults.Result.PickedPoint);
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
                }
                finally
                {
                    tr.Commit();
                }
            }
        }

        private static List<Point3d> GetVisiblePoints()
        {
            List<Point3d> points = new List<Point3d>();

            Database db = Generic.GetDatabase(); ;
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTableRecord ms =
                    (BlockTableRecord)tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForRead);

                Extents3d viewExtents = ed.GetCurrentViewBound(2);
                SelectionFilter filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "POINT") });
                PromptSelectionResult res = ed.SelectCrossingWindow(viewExtents.MinPoint, viewExtents.MaxPoint, filter);
                viewExtents.GetGeometry().AddToDrawing();
                if (res.Status == PromptStatus.OK)
                {
                    foreach (SelectedObject so in res.Value)
                    {
                        if (so == null)
                            continue;

                        DBPoint pt = tr.GetObject(so.ObjectId, OpenMode.ForRead) as DBPoint;
                        points.Add(pt.Position);
                    }
                }
            }
            return points;
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