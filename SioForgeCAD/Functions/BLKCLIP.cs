using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Functions
{
    internal class BLKCLIP
    {
        private const string FilterDictName = "ACAD_FILTER";
        private const string SpatialName = "SPATIAL";

        public static void ClipBlocksWithPolyline()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetBlocks(out var BlockSet, "Sélectionnez le ou les blocs à délimiter", false, true))
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Vérification si au moins un bloc sélectionné contient un XCLIP
                bool hasClippedBlocks = false;
                foreach (var objId in BlockSet)
                {
                    if (HasSpatialFilter(tr, objId))
                    {
                        hasClippedBlocks = true;
                        break;
                    }
                }

                // Configuration du PromptEntityOptions
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
                    Generic.WriteMessage($"{removedCount} xclip(s) supprimé(s) sur {BlockSet.Length} bloc(s) sélectionné(s).");
                    return;
                }

                // --- CAS 2 : INVERSER ---
                if (polyRes.Status == PromptStatus.Keyword && polyRes.StringResult == "INVERSER")
                {
                    int invertedCount = ProcessBlocks(tr, BlockSet, InvertSpatialFilter);
                    tr.Commit();
                    Generic.Regen();
                    ed.SetImpliedSelection(BlockSet);
                    Generic.WriteMessage($"{invertedCount} xclip(s) inversé(s) sur {BlockSet.Length} bloc(s) sélectionné(s).");
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
                    Generic.WriteMessage($"Optimisation : {removedCount} XCLIP(s) inutile(s) supprimé(s), {keptCount} conservé(s), {skippedCount} bloc(s) non clippé(s) ignoré(s).");
                    return;
                }

                if (polyRes.Status != PromptStatus.OK)
                    return;

                // --- CAS 4 : DÉCOUPE CLASSIQUE ---
                if (polyRes.ObjectId.GetDBObject(OpenMode.ForRead) is Polyline poly)
                {
                    Point2dCollection wcs2dPoints = new Point2dCollection();
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        Point3d pt = poly.GetPoint3dAt(i);
                        wcs2dPoints.Add(new Point2d(pt.X, pt.Y));
                    }

                    int clippedCount = 0;
                    foreach (var selectedObj in BlockSet)
                    {
                        if (selectedObj == null) continue;

                        if (selectedObj.GetDBObject(OpenMode.ForWrite) is BlockReference br)
                        {
                            if (!IsPolylineInsideOrCollidingWithExtents(poly, br.GeometricExtents))
                                continue;

                            CreateOrReplaceSpatialFilter(tr, br, wcs2dPoints, true);
                            clippedCount++;
                        }
                    }

                    tr.Commit();
                    Generic.Regen();
                    ed.SetImpliedSelection(BlockSet);
                    Generic.WriteMessage($"{clippedCount} bloc(s) délimité(s) sur {BlockSet.Length} sélectionné(s).");
                }
            }
        }

        /// <summary>
        /// Re-crée un filtre spatial (XCLIP) en supprimant l'existant si nécessaire.
        /// </summary>
        private static bool CreateOrReplaceSpatialFilter(Transaction tr, BlockReference br, Point2dCollection wcsPts, bool clipEnabled)
        {
            Matrix3d invTransform = br.BlockTransform.Inverse();
            Point2dCollection localPts = new Point2dCollection();

            foreach (Point2d ptWcs in wcsPts)
            {
                Point3d localPt = new Point3d(ptWcs.X, ptWcs.Y, 0.0).TransformBy(invTransform);
                localPts.Add(new Point2d(localPt.X, localPt.Y));
            }

            SpatialFilterDefinition sfd = new SpatialFilterDefinition(
                localPts,
                Vector3d.ZAxis,
                0.0,
                double.PositiveInfinity,
                double.NegativeInfinity,
                clipEnabled
            );

            SpatialFilter sf = new SpatialFilter { Definition = sfd };

            if (br.ExtensionDictionary == ObjectId.Null)
            {
                br.CreateExtensionDictionary();
            }

            DBDictionary xDict = (DBDictionary)tr.GetObject(br.ExtensionDictionary, OpenMode.ForWrite);
            DBDictionary fDict;

            if (xDict.Contains(FilterDictName))
            {
                fDict = (DBDictionary)tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForWrite);
                if (fDict.Contains(SpatialName))
                {
                    DBObject oldSf = tr.GetObject(fDict.GetAt(SpatialName), OpenMode.ForWrite);
                    oldSf.Erase(); // Supprime l'ancien filtre proprement
                }
            }
            else
            {
                fDict = new DBDictionary();
                xDict.SetAt(FilterDictName, fDict);
                tr.AddNewlyCreatedDBObject(fDict, true);
            }

            fDict.SetAt(SpatialName, sf);
            tr.AddNewlyCreatedDBObject(sf, true);

            return true;
        }

        /// <summary>
        /// Inverse le filtre existant en le supprimant puis en le recréant avec le flag inversé.
        /// </summary>
        private static bool InvertSpatialFilter(Transaction tr, ObjectId blockId)
        {
            if (tr.GetObject(blockId, OpenMode.ForWrite) is BlockReference br && br.ExtensionDictionary != ObjectId.Null)
            {
                DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                if (xDict != null && xDict.Contains(FilterDictName))
                {
                    DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForRead) as DBDictionary;
                    if (fDict != null && fDict.Contains(SpatialName))
                    {
                        SpatialFilter sf = tr.GetObject(fDict.GetAt(SpatialName), OpenMode.ForRead) as SpatialFilter;
                        if (sf != null)
                        {
                            SpatialFilterDefinition sfd = sf.Definition;
                            Point2dCollection localPts = sfd.GetPoints();

                            // Re-conversion des points locaux vers WCS pour la méthode de re-création
                            Matrix3d blockTransform = br.BlockTransform;
                            Point2dCollection wcsPts = new Point2dCollection();
                            foreach (Point2d pt2d in localPts)
                            {
                                Point3d wcsPt = new Point3d(pt2d.X, pt2d.Y, 0.0).TransformBy(blockTransform);
                                wcsPts.Add(new Point2d(wcsPt.X, wcsPt.Y));
                            }

                            // Suppression et récréation avec le statut Inverted inversé
                            return CreateOrReplaceSpatialFilter(tr, br, wcsPts, !sfd.Inverted);
                        }
                    }
                }
            }

            return false;
        }

        private static bool RemoveSpatialFilter(Transaction tr, ObjectId blockId)
        {
            if (tr.GetObject(blockId, OpenMode.ForWrite) is BlockReference br && br.ExtensionDictionary != ObjectId.Null)
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