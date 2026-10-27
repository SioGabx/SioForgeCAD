using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;
using System.IO;

namespace SioForgeCAD.Functions
{
    internal class BLKCLIP
    {
        private const string FilterDictName = "ACAD_FILTER";
        private const string SpatialName = "SPATIAL";

        public static void ClipBlocksWithPolyline()
        {
            Database srcDb = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetBlocks(out var BlockSet, "Sélectionnez le ou les blocs à délimiter", false, true))
            {
                return;
            }

            using (Transaction tr = srcDb.TransactionManager.StartTransaction())
            {
                bool hasClippedBlocks = false;
                foreach (var objId in BlockSet)
                {
                    if (HasSpatialFilter(tr, objId))
                    {
                        hasClippedBlocks = true;
                        break;
                    }
                }

                PromptEntityOptions polyOpts = new PromptEntityOptions("\nSélectionnez la polyligne de découpe : ")
                {
                    AllowNone = false,
                    AppendKeywordsToMessage = true,
                    Message = "\nSélectionnez la polyligne de découpe ou "
                };
                polyOpts.SetRejectMessage("\nL'objet doit être une polyligne.");
                polyOpts.AddAllowedClass(typeof(Polyline), true);

                if (hasClippedBlocks)
                {
                    polyOpts.Keywords.Add("OPTIMISER", "OPTIMISER", "Optimiser (O)");
                    polyOpts.Keywords.Add("SUPPRIMER", "SUPPRIMER", "Supprimer XCLIP existant (S)");
                    polyOpts.Keywords.Add("INVERSER", "INVERSER", "Inverser XCLIP existant (I)");
                }

                PromptEntityResult polyRes = ed.GetEntity(polyOpts);

                // --- CAS 1 : SUPPRIMER ---
                if (polyRes.Status == PromptStatus.Keyword && polyRes.StringResult == "SUPPRIMER")
                {
                    int removedCount = ProcessBlocks(tr, BlockSet, RemoveSpatialFilter);
                    tr.Commit();
                    Generic.Regen();
                    ed.SetImpliedSelection(BlockSet);
                    Generic.WriteMessage($"\n{removedCount} xclip(s) supprimé(s) sur {BlockSet.Length} bloc(s) sélectionné(s).");
                    return;
                }

                // --- CAS 2 : INVERSER ---
                if (polyRes.Status == PromptStatus.Keyword && polyRes.StringResult == "INVERSER")
                {
                    Debug.WriteLine("[LOG] Traitement mot-clé INVERSER en mémoire...");
                    int invertedCount = ProcessBlocksInMemoryAndReimportExploded(srcDb, tr, BlockSet, (tempTr, tempBr, tempDb) => InvertSpatialFilter(tempTr, tempBr, tempDb));
                    tr.Commit();
                    Generic.Regen();
                    Generic.WriteMessage($"\n{invertedCount} xclip(s) inversé(s) et réimporté(s) décomposé(s).");
                    return;
                }

                // --- CAS 3 : OPTIMISER ---
                if (polyRes.Status == PromptStatus.Keyword && polyRes.StringResult == "OPTIMISER")
                {
                    int removedCount = 0;
                    int keptCount = 0;
                    int skippedCount = 0;

                    foreach (var selectedObj in BlockSet)
                    {
                        if (selectedObj == null || !HasSpatialFilter(tr, selectedObj))
                        {
                            skippedCount++;
                            continue;
                        }

                        if (tr.GetObject(selectedObj, OpenMode.ForRead) is BlockReference br)
                        {
                            Point3dCollection polyWcsPoints = GetSpatialFilterPointsWcs(tr, br);
                            if (polyWcsPoints == null || polyWcsPoints.Count == 0)
                            {
                                skippedCount++;
                                continue;
                            }

                            using (Polyline polyOpt = new Polyline())
                            {
                                for (int i = 0; i < polyWcsPoints.Count; i++)
                                {
                                    Point3d pt = polyWcsPoints[i];
                                    polyOpt.AddVertexAt(i, new Point2d(pt.X, pt.Y), 0, 0, 0);
                                }
                                polyOpt.Closed = true;

                                Extents3d blockExtents = br.GeometricExtents;

                                if (IsPolylineCollidingWithExtents(polyOpt, blockExtents))
                                {
                                    keptCount++;
                                }
                                else if (RemoveSpatialFilter(tr, selectedObj))
                                {
                                    removedCount++;
                                }
                            }
                        }
                    }

                    tr.Commit();
                    Generic.Regen();
                    ed.SetImpliedSelection(BlockSet);
                    Generic.WriteMessage($"\nOptimisation : {removedCount} XCLIP(s) inutile(s) supprimé(s), {keptCount} conservé(s), {skippedCount} bloc(s) non clippé(s) ignoré(s).");
                    return;
                }

                if (polyRes.Status != PromptStatus.OK)
                {
                    return;
                }

                // --- CAS 4 : DÉCOUPE CLASSIQUE ---
                if (polyRes.ObjectId.GetDBObject(OpenMode.ForRead) is Polyline poly)
                {
                    Point2dCollection wcs2dPoints = new Point2dCollection();
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        Point3d pt = poly.GetPoint3dAt(i);
                        wcs2dPoints.Add(new Point2d(pt.X, pt.Y));
                    }

                    int clippedCount = ProcessBlocksInMemoryAndReimportExploded(srcDb, tr, BlockSet, (tempTr, tempBr, tempDb) =>
                    {
                        if (!IsPolylineInsideOrCollidingWithExtents(poly, tempBr.GeometricExtents))
                        {
                            return new SpatialFilterResult();
                        }
                        return CreateOrReplaceSpatialFilter(tempTr, tempBr, wcs2dPoints, false, tempDb);
                    });

                    tr.Commit();
                    Generic.Regen();
                    Generic.WriteMessage($"\n{clippedCount} bloc(s) délimité(s), réimporté(s) et décomposé(s).");
                }
            }
        }

        /// <summary>
        /// Effectue l'opération dans une base mémoire, sauvegarde le résultat temporaire,
        /// insère le DWG temporaire comme bloc dans le dessin d'origine, puis l'explose.
        /// </summary>
        /// <summary>
        /// Effectue l'opération dans une base mémoire, sauvegarde le résultat temporaire,
        /// insère le DWG temporaire comme bloc dans le dessin d'origine, puis parcourt
        /// chaque entité du bloc pour la cloner et copier son ExtensionDictionary.
        /// </summary>
        /// <summary>
        /// Effectue l'opération dans une base mémoire, sauvegarde le résultat temporaire,
        /// insère le DWG temporaire comme bloc dans le dessin d'origine, puis clone
        /// exactement chaque entité du bloc avec DeepCloneObjects et IdMapping.
        /// </summary>
        private static int ProcessBlocksInMemoryAndReimportExploded(
            Database srcDb,
            Transaction srcTr,
            ObjectId[] blockSet,
            Func<Transaction, BlockReference, Database, SpatialFilterResult> action)
        {
            int processedCount = 0;
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"BLKCLIP_Import_{Guid.NewGuid():N}.dwg");

            using (Database tempDb = new Database(true, true))
            {
                foreach (var selectedObj in blockSet)
                {
                    if (selectedObj == null) continue;

                    // 1. Cloner la BlockReference originale vers la base temporaire
                    ObjectIdCollection idsToClone = new ObjectIdCollection { selectedObj };
                    IdMapping mappingToTemp = new IdMapping();
                    srcDb.WblockCloneObjects(
                        idsToClone,
                        tempDb.CurrentSpaceId,
                        mappingToTemp,
                        DuplicateRecordCloning.Ignore,
                        false
                    );

                    ObjectId tempBlockRefId = mappingToTemp[selectedObj].Value;

                    // 2. Traitement dans la base temporaire
                    using (Transaction tempTr = tempDb.TransactionManager.StartTransaction())
                    {
                        if (tempTr.GetObject(tempBlockRefId, OpenMode.ForWrite) is BlockReference tempBr)
                        {
                            SpatialFilterResult result = action(tempTr, tempBr, tempDb);
                            if (result != null && result.Success)
                            {
                                CreateOrReplaceSpatialFilterInDb(tempTr, tempBr, result.Definition, result.Inverted);
                                processedCount++;

                                // Supprimer le bloc d'origine du dessin actif puisqu'on réimporte une version modifiée
                                if (srcTr.GetObject(selectedObj, OpenMode.ForWrite) is BlockReference srcBr)
                                {
                                    srcBr.Erase();
                                }
                            }
                        }
                        tempTr.Commit();
                    }
                }

                if (processedCount > 0)
                {
                    // 3. Sauvegarder le dessin mémoire sur disque
                    tempDb.SaveAs(tempFilePath, DwgVersion.Current);
                }
            }

            // 4. Réimporter le DWG temporaire et cloner exactement son contenu via DeepCloneObjects
            if (processedCount > 0 && File.Exists(tempFilePath))
            {
                try
                {
                    using (Database importDb = new Database(false, true))
                    {
                        importDb.ReadDwgFile(tempFilePath, FileOpenMode.OpenForReadAndAllShare, true, "");

                        string tempBlockName = $"TMP_IMPORT_{Guid.NewGuid():N}";
                        ObjectId importedBlockDefId = srcDb.Insert(tempBlockName, importDb, true);

                        if (srcTr.GetObject(importedBlockDefId, OpenMode.ForRead) is BlockTableRecord importedBtr)
                        {
                            // Récupérer les ObjectId de toutes les entités de la définition du bloc
                            ObjectIdCollection entitiesToClone = new ObjectIdCollection();
                            foreach (ObjectId entId in importedBtr)
                            {
                                entitiesToClone.Add(entId);
                            }

                            if (entitiesToClone.Count > 0)
                            {
                                IdMapping idMap = new IdMapping();

                                // Clonage profond (DeepClone) vers l'espace de dessin actif (ModelSpace ou PaperSpace)
                                srcDb.DeepCloneObjects(
                                    entitiesToClone,
                                    srcDb.CurrentSpaceId,
                                    idMap,
                                    false
                                );

                                // Copie explicite du dictionnaire d'extension si nécessaire pour chaque entité clonée
                                foreach (ObjectId srcEntId in entitiesToClone)
                                {
                                    if (idMap.Contains(srcEntId))
                                    {
                                        IdPair pair = idMap[srcEntId];
                                        if (pair.IsCloned && pair.Value != ObjectId.Null)
                                        {
                                            DBObject srcEnt = srcTr.GetObject(srcEntId, OpenMode.ForRead);
                                            DBObject clonedEnt = srcTr.GetObject(pair.Value, OpenMode.ForWrite);

                                            CopyExtensionDictionary(srcTr, srcEnt, clonedEnt);
                                        }
                                    }
                                }
                            }

                            // Supprimer la définition de bloc temporaire
                            importedBtr.UpgradeOpen();
                            importedBtr.Erase();
                        }
                    }
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                    {
                        try { File.Delete(tempFilePath); } catch { }
                    }
                }
            }

            return processedCount;
        }

        /// <summary>
        /// Duplique intégralement l'ExtensionDictionary d'un objet source vers un objet cible.
        /// </summary>
        private static void CopyExtensionDictionary(Transaction tr, DBObject source, DBObject target)
        {
            if (source.ExtensionDictionary == ObjectId.Null) return;

            // S'assurer que l'objet cible possède un ExtensionDictionary
            if (target.ExtensionDictionary == ObjectId.Null)
            {
                target.CreateExtensionDictionary();
            }

            DBDictionary srcDict = tr.GetObject(source.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
            DBDictionary targetDict = tr.GetObject(target.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;

            CopyDictionaryRecursive(tr, srcDict, targetDict);
        }

        /// <summary>
        /// Copie de manière récursive le contenu d'un DBDictionary vers un autre.
        /// </summary>
        private static void CopyDictionaryRecursive(Transaction tr, DBDictionary srcDict, DBDictionary targetDict)
        {
            foreach (DBDictionaryEntry entry in srcDict)
            {
                // Ignorer les filtres système AutoCAD automatiques si nécessaire (ex: ACAD_FILTER)
                DBObject srcObj = tr.GetObject(entry.Value, OpenMode.ForRead);

                DBObject clonedObj = srcObj.Clone() as DBObject;
                if (clonedObj == null) continue;

                // Si l'élément est lui-même un sous-dictionnaire
                if (srcObj is DBDictionary subSrcDict && clonedObj is DBDictionary subTargetDict)
                {
                    targetDict.SetAt(entry.Key, subTargetDict);
                    tr.AddNewlyCreatedDBObject(subTargetDict, true);

                    // Copie récursive des sous-éléments
                    CopyDictionaryRecursive(tr, subSrcDict, subTargetDict);
                }
                else
                {
                    targetDict.SetAt(entry.Key, clonedObj);
                    tr.AddNewlyCreatedDBObject(clonedObj, true);
                }
            }
        }

        private class SpatialFilterResult
        {
            public bool Success { get; set; }
            public SpatialFilterDefinition Definition { get; set; }
            public bool Inverted { get; set; }
        }

        private static SpatialFilterResult InvertSpatialFilter(Transaction tr, BlockReference br, Database targetDb = null)
        {
            if (br != null && br.ExtensionDictionary != ObjectId.Null)
            {
                DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                if (xDict != null && xDict.Contains(FilterDictName))
                {
                    DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForRead) as DBDictionary;
                    if (fDict != null && fDict.Contains(SpatialName))
                    {
                        SpatialFilter currentSf = tr.GetObject(fDict.GetAt(SpatialName), OpenMode.ForRead) as SpatialFilter;
                        if (currentSf != null)
                        {
                            Point3dCollection polyWcsPoints = GetSpatialFilterPointsWcs(tr, br);
                            if (polyWcsPoints == null || polyWcsPoints.Count == 0)
                                return new SpatialFilterResult { Success = false };

                            bool newInvertedState = !currentSf.Inverted;
                            return CreateOrReplaceSpatialFilter(tr, br, polyWcsPoints.ToPoint2dCollection(), newInvertedState, targetDb);
                        }
                    }
                }
            }
            return new SpatialFilterResult { Success = false };
        }

        private static SpatialFilterResult CreateOrReplaceSpatialFilter(Transaction tr, BlockReference br, Point2dCollection wcsPts, bool inverted, Database targetDb = null)
        {
            Matrix3d invTransform = br.BlockTransform.Inverse();
            Point2dCollection localPts = new Point2dCollection();

            foreach (Point2d ptWcs in wcsPts)
            {
                Point3d localPt = new Point3d(ptWcs.X, ptWcs.Y, 0.0).TransformBy(invTransform);
                localPts.Add(new Point2d(localPt.X, localPt.Y));
            }

            Vector3d normal;
            double elev = 0;
            Database acCurDb = targetDb ?? Generic.GetDatabase();

            if (acCurDb.TileMode)
            {
                normal = acCurDb.Ucsxdir.CrossProduct(acCurDb.Ucsydir);
                elev = acCurDb.Elevation;
            }
            else
            {
                normal = acCurDb.Pucsxdir.CrossProduct(acCurDb.Pucsydir);
                elev = acCurDb.Pelevation;
            }

            SpatialFilterDefinition sfd = new SpatialFilterDefinition(
                localPts,
                normal,
                elev,
                double.PositiveInfinity,
                double.NegativeInfinity,
                true
            );

            return new SpatialFilterResult
            {
                Success = true,
                Definition = sfd,
                Inverted = inverted
            };
        }

        private static void CreateOrReplaceSpatialFilterInDb(Transaction tr, BlockReference br, SpatialFilterDefinition sfd, bool inverted)
        {
            if (br.ExtensionDictionary == ObjectId.Null)
            {
                br.CreateExtensionDictionary();
            }

            RemoveSpatialFilter(tr, br);

            SpatialFilter sf = new SpatialFilter { Definition = sfd, Inverted = inverted };
            DBDictionary fDict = new DBDictionary();
            DBDictionary xDict = (DBDictionary)tr.GetObject(br.ExtensionDictionary, OpenMode.ForWrite);

            xDict.SetAt(FilterDictName, fDict);
            tr.AddNewlyCreatedDBObject(fDict, true);

            fDict.SetAt(SpatialName, sf);
            tr.AddNewlyCreatedDBObject(sf, true);
        }

        private static bool RemoveSpatialFilter(Transaction tr, ObjectId blockId)
        {
            if (tr.GetObject(blockId, OpenMode.ForWrite) is BlockReference br)
            {
                RemoveSpatialFilter(tr, br);
            }
            return false;
        }

        private static bool RemoveSpatialFilter(Transaction tr, BlockReference br)
        {
            if (br != null && br.ExtensionDictionary != ObjectId.Null)
            {
                DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
                if (xDict != null && xDict.Contains(FilterDictName))
                {
                    DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForWrite) as DBDictionary;
                    if (fDict != null && fDict.Contains(SpatialName))
                    {
                        DBObject sf = tr.GetObject(fDict.GetAt(SpatialName), OpenMode.ForWrite);
                        sf.Erase();
                        fDict.Remove(SpatialName);
                        return true;
                    }
                    xDict.Remove(FilterDictName);
                }
            }
            return false;
        }

        private static bool HasSpatialFilter(Transaction tr, ObjectId blockId)
        {
            if (tr.GetObject(blockId, OpenMode.ForRead) is BlockReference br && br.ExtensionDictionary != ObjectId.Null)
            {
                DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                if (xDict != null && xDict.Contains(FilterDictName))
                {
                    DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForRead) as DBDictionary;
                    return fDict != null && fDict.Contains(SpatialName);
                }
            }
            return false;
        }

        private static Point3dCollection GetSpatialFilterPointsWcs(Transaction tr, BlockReference br)
        {
            if (br == null || br.ExtensionDictionary == ObjectId.Null) return null;

            DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
            if (xDict == null || !xDict.Contains(FilterDictName)) return null;

            DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForRead) as DBDictionary;
            if (fDict == null || !fDict.Contains(SpatialName)) return null;

            SpatialFilter sf = tr.GetObject(fDict.GetAt(SpatialName), OpenMode.ForRead) as SpatialFilter;
            if (sf == null) return null;

            Point2dCollection localPts = sf.Definition.GetPoints();
            if (localPts == null || localPts.Count == 0) return null;

            Matrix3d blockTransform = br.BlockTransform;
            Point3dCollection wcsPts = new Point3dCollection();

            foreach (Point2d pt2d in localPts)
            {
                wcsPts.Add(new Point3d(pt2d.X, pt2d.Y, 0.0).TransformBy(blockTransform));
            }

            return wcsPts;
        }

        private static int ProcessBlocks(Transaction tr, ObjectId[] blockSet, Func<Transaction, ObjectId, bool> action)
        {
            int count = 0;
            foreach (var objId in blockSet)
            {
                if (action(tr, objId)) count++;
            }
            return count;
        }

        private static bool IsPolylineCollidingWithExtents(Polyline poly, Extents3d extents)
        {
            Extents3d polyExtents = poly.GeometricExtents;
            if (polyExtents.MinPoint.X > extents.MaxPoint.X || polyExtents.MaxPoint.X < extents.MinPoint.X ||
                polyExtents.MinPoint.Y > extents.MaxPoint.Y || polyExtents.MaxPoint.Y < extents.MinPoint.Y)
            {
                return false;
            }

            Point2d min = new Point2d(extents.MinPoint.X, extents.MinPoint.Y);
            Point2d max = new Point2d(extents.MaxPoint.X, extents.MaxPoint.Y);

            LineSegment2d[] bboxEdges = new LineSegment2d[]
            {
                new LineSegment2d(min, new Point2d(max.X, min.Y)),
                new LineSegment2d(new Point2d(max.X, min.Y), max),
                new LineSegment2d(max, new Point2d(min.X, max.Y)),
                new LineSegment2d(new Point2d(min.X, max.Y), min)
            };

            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                SegmentType segType = poly.GetSegmentType(i);

                if (segType == SegmentType.Line)
                {
                    LineSegment2d polySeg = poly.GetLineSegment2dAt(i);
                    foreach (var edge in bboxEdges)
                    {
                        if (polySeg.IntersectWith(edge) != null) return true;
                    }
                }
                else if (segType == SegmentType.Arc)
                {
                    CircularArc2d polyArc = poly.GetArcSegment2dAt(i);
                    foreach (var edge in bboxEdges)
                    {
                        if (polyArc.IntersectWith(edge) != null) return true;
                    }
                }
            }
            return false;
        }

        private static bool IsPolylineInsideOrCollidingWithExtents(Polyline poly, Extents3d extents)
        {
            if (IsPolylineCollidingWithExtents(poly, extents)) return true;

            Extents3d polyExtents = poly.GeometricExtents;
            if (polyExtents.MinPoint.X > extents.MaxPoint.X || polyExtents.MaxPoint.X < extents.MinPoint.X ||
                polyExtents.MinPoint.Y > extents.MaxPoint.Y || polyExtents.MaxPoint.Y < extents.MinPoint.Y)
            {
                return false;
            }

            Point2d min = new Point2d(extents.MinPoint.X, extents.MinPoint.Y);
            Point2d max = new Point2d(extents.MaxPoint.X, extents.MaxPoint.Y);

            Point3d bboxCenter = new Point3d((min.X + max.X) / 2.0, (min.Y + max.Y) / 2.0, extents.MinPoint.Z);
            return bboxCenter.IsInsidePolyline(poly);
        }
    }
}