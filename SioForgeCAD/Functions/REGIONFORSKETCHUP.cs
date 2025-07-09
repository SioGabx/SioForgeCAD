using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using Region = Autodesk.AutoCAD.DatabaseServices.Region;

namespace SioForgeCAD.Functions
{
    public static class REGIONFORSKETCHUP
    {
        public static void GenerateRegionFromBoundaries()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            PromptSelectionResult psr = ed.GetSelectionRedraw(
                "Sélectionnez les entités fermées (polylignes, cercles...) :",
                false,
                false,
                new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,CIRCLE,ELLIPSE,POLYLINE,INSERT") }));

            if (psr.Status != PromptStatus.OK)
            {
                Generic.WriteMessage("Aucune entité valide sélectionnée.");
                return;
            }

            List<ObjectId> ForSketchupEntities = new List<ObjectId>();

            using (DocumentLock currentLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (SelectedObject selObj in psr.Value)
                {
                    if (!(selObj?.ObjectId.GetObject(OpenMode.ForRead) is Entity ent)) continue;
                    if (ent is Curve)
                    {
                        // Clone l'entité sélectionnée pour ne pas modifier l'original
                        Entity tempEnt = ent.Clone() as Entity;
                        if (tempEnt == null) continue;
                        tempEnt.Flatten();
                        DBObjectCollection curves = new DBObjectCollection { tempEnt };
                        DBObjectCollection regions = null;

                        try
                        {
                            regions = Region.CreateFromCurves(curves);
                        }
                        catch { }

                        curves.DeepDispose();

                        if (regions == null || regions.Count == 0)
                        {
                            Generic.WriteMessage($"Échec : Impossible de créer une région pour {ent.GetType().Name} (ID: {ent.ObjectId}).");
                            continue;
                        }

                        foreach (Region region in regions)
                        {
                            if (region == null) continue;

                            string layer = ent.Layer;
                            ObjectId layerId = Layers.GetLayerIdByName(layer);
                            Autodesk.AutoCAD.Colors.Color color = ent.Color.IsByLayer ? Layers.GetLayerColor(layerId) : ent.Color;

                            region.Layer = "0";
                            region.Color = color;

                            btr.AppendEntity(region);
                            tr.AddNewlyCreatedDBObject(region, true);

                            ForSketchupEntities.Add(region.ObjectId);
                        }
                    }
                    else
                    {
                        var EntClone = ent.Clone() as Entity;
                        btr.AppendEntity(EntClone);
                        tr.AddNewlyCreatedDBObject(EntClone, true);
                        ForSketchupEntities.Add(EntClone.ObjectId);
                    }
                }

                var InNewDrawingPrompt = ed.GetOptions("Voullez vous créer un nouveau dessin avec les régions ?", "Oui", "Non");
                bool InNewDrawing = InNewDrawingPrompt.Status == PromptStatus.OK && InNewDrawingPrompt.StringResult == "Oui";

                if (InNewDrawing)
                {
                    // Crée un nouveau dessin vide (sans template)
                    Document newDoc = Application.DocumentManager.Add("");
                    Database newDb = newDoc.Database;
                    // Copie les régions vers le nouveau dessin
                    using (DocumentLock newLock = newDoc.LockDocument())
                    using (Transaction newTr = newDb.TransactionManager.StartTransaction())
                    {
                        BlockTable newBt = newTr.GetObject(newDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord newBtr = newTr.GetObject(newBt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        IdMapping mapping = new IdMapping();
                        ObjectIdCollection toClone = new ObjectIdCollection(ForSketchupEntities.ToArray());

                        db.WblockCloneObjects(
                            toClone,
                            newBtr.ObjectId,
                            mapping,
                            DuplicateRecordCloning.Replace,
                            false
                        );

                        // Récupère les régions clonées et calcule les extents
                        List<Entity> clonedRegions = new List<Entity>();

                        foreach (ObjectId id in toClone)
                        {
                            if (mapping.Contains(id))
                            {
                                ObjectId newId = mapping[id].Value;
                                Entity e = newTr.GetObject(newId, OpenMode.ForWrite) as Entity;
                                if (e != null)
                                    clonedRegions.Add(e);
                            }
                        }

                        // Recalage à 0,0
                        Extents3d? extents = clonedRegions.GetExtents();
                        if (extents.HasValue)
                        {
                            Point3d minPt = extents.Value.MinPoint;
                            Vector3d translation = Point3d.Origin - new Point3d(minPt.X, minPt.Y, 0);

                            foreach (Entity region in clonedRegions)
                            {
                                region.TransformBy(Matrix3d.Displacement(translation));
                            }
                        }

                        newTr.Commit();
                        Application.DocumentManager.MdiActiveDocument = newDoc;
                        extents?.ZoomExtents();
                    }

                    foreach (var item in ForSketchupEntities)
                    {
                        item.EraseObject();
                    }
                    ed.WriteMessage($"\n{ForSketchupEntities.Count} région(s) transférée(s) dans le nouveau dessin.");
                }
                else
                {
                    foreach (SelectedObject selObj in psr.Value)
                    {
                        selObj.ObjectId.EraseObject();
                    }

                    ed.WriteMessage($"\n{ForSketchupEntities.Count} région(s) créée(s).");
                }
                tr.Commit();
            }

            Generic.RegenALL();
        }

    }
}
