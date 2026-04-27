using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SioForgeCAD.Functions
{
    public static class TEST
    {
        [CommandMethod("ExportLayoutComplet")]
        public static void ExportLayoutComplet()
        {
            Document doc = Generic.GetDocument();
            if (doc == null) return;

            if (LayoutManager.Current.CurrentLayoutIsModel())
            {
                Generic.WriteMessage("Erreur : Veuillez vous placer sur une présentation (Layout).");
                return;
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"ExportLayout_Final_{Guid.NewGuid()}.dwg");

            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            using (Database newDb = new Database(true, true))
            using (Transaction newTr = newDb.TransactionManager.StartTransaction())
            {
                ProcessLayoutExport(doc.Database, tr, newDb, newTr);

                newTr.Commit();
                newDb.SaveAs(tempFilePath, DwgVersion.Current);
                tr.Commit();
            }

            if (File.Exists(tempFilePath))
            {
                Application.DocumentManager.Open(tempFilePath, false);
                Generic.WriteMessage($"Export terminé : {tempFilePath}");
            }
        }

        /// <summary>
        /// Gère la logique principale d'exportation.
        /// </summary>
        private static void ProcessLayoutExport(Database sourceDb, Transaction tr, Database newDb, Transaction newTr)
        {
            newDb.SetCustomProperties(sourceDb.GetCustomProperties());

            BlockTable newBt = (BlockTable)newTr.GetObject(newDb.BlockTableId, OpenMode.ForWrite);
            ObjectId newModelSpaceId = newBt[BlockTableRecord.ModelSpace];

            Layout layout = LayoutManager.Current.GetCurrentLayout();
            layout.ExtractLayoutEntities(out List<Viewport> viewports, out ObjectIdCollection paperIdsToClone);
            if (paperIdsToClone.Count > 0)
            {
                sourceDb.WblockCloneObjects(paperIdsToClone, newModelSpaceId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
            }
            BlockTableRecord cacheBtr = CreateModelCache(sourceDb, tr, newTr, newBt, out ObjectIdCollection cachedModelIds);
            ProcessViewports(newDb, newTr, newModelSpaceId, viewports, cachedModelIds);
            cacheBtr.Erase();

            DrawPaperFrame(newTr, newModelSpaceId, layout);
        }


        /// <summary>
        /// Imports the Model Space into a temporary block in the new database
        /// to speed up multiple clonings and avoid cross-database access.
        /// </summary>
        private static BlockTableRecord CreateModelCache(Database sourceDb, Transaction tr, Transaction newTr, BlockTable newBt, out ObjectIdCollection cachedModelIds)
        {
            BlockTable bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
            BlockTableRecord modelBtr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            ObjectIdCollection modelIdsToClone = new ObjectIdCollection();
            foreach (ObjectId modelEntId in modelBtr)
            {
                modelIdsToClone.Add(modelEntId);
            }

            BlockTableRecord cacheBtr = new BlockTableRecord { Name = "TEMP_CACHE_" + Guid.NewGuid().ToString("N") };
            ObjectId cacheBtrId = newBt.Add(cacheBtr);
            newTr.AddNewlyCreatedDBObject(cacheBtr, true);

            if (modelIdsToClone.Count > 0)
            {
                sourceDb.WblockCloneObjects(modelIdsToClone, cacheBtrId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
            }

            cachedModelIds = new ObjectIdCollection();
            foreach (ObjectId id in cacheBtr)
            {
                cachedModelIds.Add(id);
            }

            return cacheBtr;
        }

        private static void ProcessViewports(Database newDb, Transaction newTr, ObjectId newModelSpaceId, List<Viewport> viewports, ObjectIdCollection cachedModelIds)
        {
            foreach (Viewport vp in viewports)
            {
                Dictionary<string, Layers.LayerStatus> LayersProps = Layers.GetAllLayersPropertiesInDrawing(vp.ObjectId);
                Matrix3d modelToPaper = vp.GetModelToPaperTransform();
                Extents3d vpExtents = vp.GeometricExtents;
                ObjectIdCollection vpIdsToClone = new ObjectIdCollection();

                foreach (ObjectId cachedId in cachedModelIds)
                {
                    Entity cachedEnt = (Entity)newTr.GetObject(cachedId, OpenMode.ForRead);

                    if (!LayersProps[cachedEnt.Layer].IsPlottable) continue;
                    if (!vp.IsEntityVisibleInViewport(cachedEnt)) continue;

                    vpIdsToClone.Add(cachedId);
                }

                if (vpIdsToClone.Count == 0) continue;

                IdMapping vpMapping = new IdMapping();
                newDb.DeepCloneObjects(vpIdsToClone, newModelSpaceId, vpMapping, false);

                using (Curve vpBoundary = vp.GetBoundary())
                {
                    foreach (ObjectId cachedId in vpIdsToClone)
                    {
                        if (vpMapping.Contains(cachedId))
                        {
                            IdPair pair = vpMapping.Lookup(cachedId);
                            if (pair.IsCloned)
                            {
                                Entity clonedEnt = (Entity)newTr.GetObject(pair.Value, OpenMode.ForWrite, false, true);
                                clonedEnt.TransformBy(modelToPaper);


                                BlockTableRecord btr = (BlockTableRecord)newTr.GetObject(newModelSpaceId, OpenMode.ForWrite);
                                ClipEntityToBoundary(newTr, btr, clonedEnt, vpBoundary);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawPaperFrame(Transaction newTr, ObjectId newModelSpaceId, Layout layout)
        {
            using (Polyline paperFrame = layout.GetPaperFrame())
            {
                paperFrame.ColorIndex = 1; // Rouge

                BlockTableRecord newModelBtrWrite = (BlockTableRecord)newTr.GetObject(newModelSpaceId, OpenMode.ForWrite);
                newModelBtrWrite.AppendEntity(paperFrame);
                newTr.AddNewlyCreatedDBObject(paperFrame, true);
            }
        }

        /// <summary>
        /// Aiguillage selon le type d'entité pour appliquer la bonne méthode de découpe.
        /// </summary>
        private static void ClipEntityToBoundary(Transaction tr, BlockTableRecord btr, Entity ent, Curve boundary)
        {
            if (boundary == null) return;

            if (ent is Line || ent is Polyline || ent is Arc)
            {
                ((Curve)ent).ClipCurve(boundary, tr, btr);
            }
            else if (ent is Hatch)
            {
                //TODO
            }
        }
    }
}