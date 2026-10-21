using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;


namespace SioForgeCAD.Functions
{
    public static class SOLIDTOHATCH
    {
        public static void Convert()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            // Sélection des solides 2D
            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\nSélectionnez les solides 2D : ";

            TypedValue[] filterValues = { new TypedValue((int)DxfCode.Start, "SOLID") };
            SelectionFilter filter = new SelectionFilter(filterValues);
            PromptSelectionResult sel = ed.GetSelection(opts, filter);

            if (sel.Status != PromptStatus.OK)
            {
                return;
            }

            int converted = 0;
            using (LongOperationProcess LongOperation = new LongOperationProcess("Conversion des solides en hachure..."))

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LongOperation.SetTotalOperations(sel.Value.Count);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (SelectedObject selected in sel.Value)
                {
                    if (selected == null)
                    {
                        continue;
                    }

                    Solid solid = tr.GetObject(selected.ObjectId, OpenMode.ForRead) as Solid;

                    if (solid == null)
                    {
                        continue;
                    }

                    try
                    {

                        Point3d p0 = solid.GetPointAt(0);
                        Point3d p1 = solid.GetPointAt(1);
                        Point3d p2 = solid.GetPointAt(2);
                        Point3d p3 = solid.GetPointAt(3);

                        using (Polyline boundary = new Polyline())
                        {
                            // Ordre du contour AutoCAD : 0, 1, 3, 2
                            Point3d[] points = { p0, p1, p3, p2 };

                            // Éliminer les sommets consécutifs confondus
                            int index = 0;

                            foreach (Point3d p in points)
                            {
                                if (index > 0 &&
                                    boundary.GetPoint3dAt(index - 1).DistanceTo(p) < 1e-8)
                                {
                                    continue;
                                }

                                boundary.AddVertexAt(
                                    index,
                                    new Point2d(p.X, p.Y),
                                    0, 0, 0);

                                index++;
                            }

                            // Si le dernier point est identique au premier,
                            // ne pas le dupliquer dans la polyligne.
                            if (index > 1 &&
                                boundary.GetPoint3dAt(index - 1)
                                        .DistanceTo(boundary.GetPoint3dAt(0)) < 1e-8)
                            {
                                boundary.RemoveVertexAt(index - 1);
                            }

                            boundary.Closed = true;
                            boundary.Elevation = p0.Z;

                            ObjectId boundaryId = boundary.AddToDrawingCurrentTransaction();


                            using (Hatch hatch = new Hatch())
                            {
                                hatch.SetDatabaseDefaults();
                                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                hatch.Associative = false;

                                ObjectId hatchId = btr.AppendEntity(hatch);
                                tr.AddNewlyCreatedDBObject(hatch, true);

                                ObjectIdCollection boundaryIds = new ObjectIdCollection
                                {
                                    boundaryId
                                };

                                hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                                hatch.EvaluateHatch(true);
                                solid.CopyPropertiesTo(hatch);
                                solid.CopyDrawOrderTo(hatch);

                                boundary.Erase();
                                solid.UpgradeOpen();
                                solid.Erase();

                                converted++;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage("\nErreur de conversion : " + ex.Message);
                    }

                    LongOperation.UpdateProgress();
                    if (LongOperation.IsCanceled) { return; }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nConversion terminée : " + converted + " solide(s) converti(s) en hachure(s) SOLID.");
        }
    }
}
