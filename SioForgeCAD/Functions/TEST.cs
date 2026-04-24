using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class TEST
    {
        [CommandMethod("ExportLayoutComplet")]
        public static void ExportLayoutComplet()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"ExportLayout_Final_{Guid.NewGuid()}.dwg");

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    string currentLayoutName = LayoutManager.Current.CurrentLayout;
                    if (currentLayoutName == "Model")
                    {
                        ed.WriteMessage("\nErreur : Veuillez vous placer sur une présentation (Layout).");
                        return;
                    }

                    DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                    Layout layout = (Layout)tr.GetObject(layoutDict.GetAt(currentLayoutName), OpenMode.ForRead);
                    BlockTableRecord layoutBtr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);

                    using (Database newDb = new Database(true, true))
                    {
                        using (Transaction newTr = newDb.TransactionManager.StartTransaction())
                        {
                            newDb.SetCustomProperties(db.GetCustomProperties()); //Transfer GetCustomProperties

                            BlockTable newBt = (BlockTable)newTr.GetObject(newDb.BlockTableId, OpenMode.ForWrite);
                            ObjectId newModelSpaceId = newBt[BlockTableRecord.ModelSpace];

                            // --- 1. TRI DES ENTITÉS DU PAPIER ET DES VIEWPORTS ---
                            ObjectIdCollection paperIdsToClone = new ObjectIdCollection();
                            List<Viewport> viewports = new List<Viewport>();

                            foreach (ObjectId entId in layoutBtr)
                            {
                                Entity ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                                if (ent is Viewport vp && vp.Number != 1)
                                {
                                    viewports.Add(vp);
                                }
                                else if (!(ent is Viewport)) // On exclut le viewport 1 (le papier lui-même)
                                {
                                    paperIdsToClone.Add(entId);
                                }
                            }

                            // --- 2. CLONAGE DU PAPIER ---
                            if (paperIdsToClone.Count > 0)
                            {
                                IdMapping paperMapping = new IdMapping();
                                db.WblockCloneObjects(paperIdsToClone, newModelSpaceId, paperMapping, DuplicateRecordCloning.Ignore, false);
                            }

                            // --- 3. PRÉPARATION DU MODÈLE ET MISE EN CACHE ---
                            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord modelBtr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                            ObjectIdCollection modelIdsToClone = new ObjectIdCollection();
                            foreach (ObjectId modelEntId in modelBtr)
                            {
                                modelIdsToClone.Add(modelEntId);
                            }

                            if (modelIdsToClone.Count > 0)
                            {
                                // Création du Bloc Cache dans la nouvelle DB
                                BlockTableRecord cacheBtr = new BlockTableRecord();
                                cacheBtr.Name = "TEMP_CACHE_" + Guid.NewGuid().ToString("N");
                                ObjectId cacheBtrId = newBt.Add(cacheBtr);
                                newTr.AddNewlyCreatedDBObject(cacheBtr, true);

                                // Transfert depuis l'ancienne DB vers le Bloc Cache de la nouvelle DB
                                IdMapping cacheMapping = new IdMapping();
                                db.WblockCloneObjects(modelIdsToClone, cacheBtrId, cacheMapping, DuplicateRecordCloning.Ignore, false);

                                // On récupère les ObjectIds des entités maintenant présentes dans le cache
                                ObjectIdCollection cachedModelIds = new ObjectIdCollection();
                                foreach (ObjectId id in cacheBtr)
                                {
                                    cachedModelIds.Add(id);
                                }


                                // --- 4. CLONAGE ET PROJECTION POUR CHAQUE VIEWPORT (AVEC FILTRE DE VISIBILITÉ) ---
                                foreach (Viewport vp in viewports)
                                {
                                    Dictionary<string, Layers.LayerStatus> LayersProps = Layers.GetAllLayersPropertiesInDrawing(vp.ObjectId);

                                    Matrix3d modelToPaper = vp.GetModelToPaperTransform();
                                    Extents3d vpExtents = vp.GeometricExtents;
                                    ObjectIdCollection vpIdsToClone = new ObjectIdCollection();

                                    foreach (ObjectId cachedId in cachedModelIds)
                                    {
                                        Entity cachedEnt = (Entity)newTr.GetObject(cachedId, OpenMode.ForRead);
                                        if (cachedEnt is Ray)
                                        {
                                            Debug.WriteLine(cachedEnt.GetType().Name);
                                        }
                                        if (!LayersProps[cachedEnt.Layer].IsPlottable) { continue; }
                                        if (!IsVisibleInViewport(cachedEnt, vpExtents, modelToPaper)) { continue; }
                                        vpIdsToClone.Add(cachedId);
                                    }

                                    // Si aucune entité n'est visible dans ce viewport, on passe au suivant
                                    if (vpIdsToClone.Count == 0) continue;

                                    // DeepClone force la création de nouvelles entités à l'intérieur de la même DB
                                    IdMapping vpMapping = new IdMapping();
                                    newDb.DeepCloneObjects(vpIdsToClone, newModelSpaceId, vpMapping, false);

                                    // Application de la matrice de transformation aux nouveaux clones
                                    foreach (ObjectId cachedId in vpIdsToClone)
                                    {
                                        if (vpMapping.Contains(cachedId))
                                        {
                                            IdPair pair = vpMapping.Lookup(cachedId);
                                            if (pair.IsCloned)
                                            {
                                                Entity clonedEnt = (Entity)newTr.GetObject(pair.Value, OpenMode.ForWrite, false, true);
                                                clonedEnt.TransformBy(modelToPaper);
                                            }
                                        }
                                    }
                                }

                                // --- NETTOYAGE ---
                                // On supprime le bloc cache qui ne sert plus à rien pour alléger le fichier
                                cacheBtr.Erase();
                            }

                            // --- 5. DESSIN DU CADRE DU PAPIER ---
                            // --- 5. DESSIN DU CADRE DU PAPIER ---
                            BlockTableRecord newModelBtrWrite = (BlockTableRecord)newTr.GetObject(newModelSpaceId, OpenMode.ForWrite);

                            using (Polyline paperFrame = new Polyline())
                            {
                                var PaperExtents = layout.GetPaperExtents();
                                paperFrame.AddVertexAt(0, PaperExtents.MinPoint, 0, 0, 0);
                                paperFrame.AddVertexAt(1, new Point2d(PaperExtents.MaxPoint.X, PaperExtents.MinPoint.Y), 0, 0, 0);
                                paperFrame.AddVertexAt(2, PaperExtents.MaxPoint, 0, 0, 0);
                                paperFrame.AddVertexAt(3, new Point2d(PaperExtents.MinPoint.X, PaperExtents.MaxPoint.Y), 0, 0, 0);
                                paperFrame.Closed = true;
                                paperFrame.ColorIndex = 1; // Rouge

                                newModelBtrWrite.AppendEntity(paperFrame);
                                newTr.AddNewlyCreatedDBObject(paperFrame, true);
                            }

                            newTr.Commit();
                        }
                        newDb.SaveAs(tempFilePath, DwgVersion.Current);
                    }
                    tr.Commit();
                }
            }

            if (File.Exists(tempFilePath))
            {
                Application.DocumentManager.Open(tempFilePath, false);
                ed.WriteMessage($"\nExport terminé : {tempFilePath}");
            }
        }

        /// <summary>
        /// Vérifie si la Bounding Box de l'entité (projetée en espace papier) croise la Bounding Box du Viewport.
        /// </summary>
        private static bool IsVisibleInViewport(Entity ent, Extents3d vpExtents, Matrix3d modelToPaper)
        {
            try
            {
                if (ent is null)
                {
                    return false;
                }
                

                Extents3d extents = ent.GetExtents();
                if (ent?.Bounds.HasValue == false)
                {
                    return true;
                }
                Point3d min = extents.MinPoint;
                Point3d max = extents.MaxPoint;

                // Les 8 coins de la Bounding Box 3D de l'entité
                Point3d[] corners = new Point3d[]
                {
                    new Point3d(min.X, min.Y, min.Z),
                    new Point3d(max.X, min.Y, min.Z),
                    new Point3d(max.X, max.Y, min.Z),
                    new Point3d(min.X, max.Y, min.Z),
                    new Point3d(min.X, min.Y, max.Z),
                    new Point3d(max.X, min.Y, max.Z),
                    new Point3d(max.X, max.Y, max.Z),
                    new Point3d(min.X, max.Y, max.Z)
                };

                double eMinX = double.MaxValue, eMinY = double.MaxValue;
                double eMaxX = double.MinValue, eMaxY = double.MinValue;

                // On transforme les 8 coins en coordonnées Espace Papier pour trouver les limites de l'entité projetée
                foreach (Point3d corner in corners)
                {
                    Point3d pt = corner.TransformBy(modelToPaper);
                    if (pt.X < eMinX) eMinX = pt.X;
                    if (pt.Y < eMinY) eMinY = pt.Y;
                    if (pt.X > eMaxX) eMaxX = pt.X;
                    if (pt.Y > eMaxY) eMaxY = pt.Y;
                }

                // Test d'intersection AABB 2D (Axes X et Y de l'espace papier)
                return eMaxX >= vpExtents.MinPoint.X && eMinX <= vpExtents.MaxPoint.X &&
                    eMaxY >= vpExtents.MinPoint.Y && eMinY <= vpExtents.MaxPoint.Y;
            }
            catch
            {
                // Certaines entités infinies ou vides (Xline, Ray, Textes vides) lèvent une exception sur GeometricExtents.
                // Par sécurité, on retourne true pour ne pas perdre d'informations potentielles. AutoCAD coupera ce qui dépasse.
                return true;
            }
        }
    }
}