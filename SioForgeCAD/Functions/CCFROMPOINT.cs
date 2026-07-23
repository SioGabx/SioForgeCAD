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
                    var HeightHintText = new DBText()
                    {
                        TextString = CotePoints.FormatAltitude(0),
                        Height = 0.05,
                        ColorIndex = 8, // gris AutoCAD
                        Position = Point3d.Origin,
                        HorizontalMode = TextHorizontalMode.TextCenter,
                        VerticalMode = TextVerticalMode.TextVerticalMid,
                        AlignmentPoint = Point3d.Origin
                    };

                    //const double MaxDistance = .1;
                    //const double MaxDistanceSquared = MaxDistance * MaxDistance;

                    bool UpdateFunction(Point3d currentPoint, InputPointContext context, GetEntityJig jig)
                    {
                        double maxDistance;

                        double pdSize = db.Pdsize;

                        if (pdSize > 0)
                        {
                            maxDistance = pdSize;
                        }
                        else
                        {
                            Extents3d extents = ed.GetDisplayAreaExtents();
                            double viewSize = Math.Max(
                                extents.MaxPoint.X - extents.MinPoint.X,
                                extents.MaxPoint.Y - extents.MinPoint.Y);

                            // PDSIZE = 0 -> 5 % de la taille de la vue
                            // PDSIZE < 0 -> |PDSIZE| %
                            double percent = pdSize == 0 ? 5.0 : -pdSize;

                            maxDistance = viewSize * percent / 100.0;
                        }

                        double maxDistanceSquared = maxDistance * maxDistance;

                        Point3d nearest = Point3d.Origin;
                        double best = maxDistanceSquared;

                        foreach (var p in Points)
                        {
                            double dx = p.X - currentPoint.X;
                            if (Math.Abs(dx) > maxDistance)
                            {
                                continue;
                            }

                            double dy = p.Y - currentPoint.Y;
                            if (Math.Abs(dy) > maxDistance)
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

                        if (nearest == Point3d.Origin)
                        {
                            HeightHintText.TextString = "";
                        }
                        else
                        {
                            HeightHintText.Position = nearest;
                            HeightHintText.AlignmentPoint = nearest;
                            HeightHintText.TextString = CotePoints.FormatAltitude(nearest.Z);
                        }

                        jig.RegenTransients();
                        return true;
                    }



                    using (var GetEntJig = new GetEntityJig()
                    {
                        StaticEntities = new DBObjectCollection() { HeightHintText },
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

                Extents3d viewExtents = ed.GetDisplayAreaExtents();
                viewExtents.Expand(1.2);
                SelectionFilter filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "POINT") });
                var polygonWcs = viewExtents.GetPointsCollection();
                Matrix3d wcsToUcs = ed.CurrentUserCoordinateSystem.Inverse();
                Point3dCollection polygonUcs = new Point3dCollection();
                foreach (Point3d p in polygonWcs)
                {
                    polygonUcs.Add(p.TransformBy(wcsToUcs));
                }


                PromptSelectionResult res = ed.SelectWindowPolygon(polygonUcs, filter);
                //viewExtents.GetGeometry().AddToDrawing();
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