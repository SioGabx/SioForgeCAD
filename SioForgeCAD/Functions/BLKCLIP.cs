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
        const string FilterDictName = "ACAD_FILTER";
        const string SpatialName = "SPATIAL";

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
                // Vérification si au moins un bloc sélectionné contient un xclip
                bool hasClippedBlocks = false;
                foreach (var objId in BlockSet)
                {
                    if (HasSpatialFilter(tr, objId))
                    {
                        hasClippedBlocks = true;
                        break;
                    }
                }

                // Configuration du PromptEntityOptions avec le Keyword si nécessaire
                PromptEntityOptions polyOpts = new PromptEntityOptions("\nSélectionnez la polyligne de découpe : ")
                {
                    AllowNone = false,
                    AppendKeywordsToMessage = true
                };
                polyOpts.SetRejectMessage("\nL'objet doit être une polyligne.");
                polyOpts.AddAllowedClass(typeof(Polyline), true);

                if (hasClippedBlocks)
                {
                    polyOpts.Keywords.Add("SUPPRIMER", "SUPPRIMER", "Supprimer XCLIP existant (S)");
                    polyOpts.Message = "\nSélectionnez la polyligne de découpe ou ";
                }

                PromptEntityResult polyRes = ed.GetEntity(polyOpts);

                // Cas 1 : L'utilisateur choisit le mot-clé pour supprimer les xclips
                if (polyRes.Status == PromptStatus.Keyword && polyRes.StringResult == "SUPPRIMER")
                {
                    int removedCount = 0;
                    foreach (var objId in BlockSet)
                    {
                        if (RemoveSpatialFilter(tr, objId))
                        {
                            removedCount++;
                        }
                    }

                    tr.Commit();
                    Generic.Regen();
                    Generic.WriteMessage($"{removedCount} xclip(s) supprimé(s) sur {BlockSet.Length} bloc(s) sélectionné(s).");
                    return;
                }

                if (polyRes.Status != PromptStatus.OK)
                {
                    return;
                }

                // Cas 2 : L'utilisateur a sélectionné une polyligne
                Polyline poly = polyRes.ObjectId.GetDBObject(OpenMode.ForRead) as Polyline;
                if (poly == null)
                {
                    return;
                }

                Point3dCollection polyWcsPoints = new Point3dCollection();
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    polyWcsPoints.Add(poly.GetPoint3dAt(i));
                }

                int clippedCount = 0;

                foreach (var selectedObj in BlockSet)
                {
                    if (selectedObj == null)
                    {
                        continue;
                    }

                    BlockReference br = selectedObj.GetDBObject(OpenMode.ForWrite) as BlockReference;
                    if (br == null)
                    {
                        continue;
                    }

                    // Verification de la collision
                    Extents3d blockExtents = br.GeometricExtents;
                    if (!IsPolylineCollidingWithExtents(poly, blockExtents))
                    {
                        continue;
                    }

                    Matrix3d invTransform = br.BlockTransform.Inverse();
                    Point2dCollection localPts = new Point2dCollection();

                    foreach (Point3d ptWcs in polyWcsPoints)
                    {
                        Point3d localPt = ptWcs.TransformBy(invTransform);
                        localPts.Add(new Point2d(localPt.X, localPt.Y));
                    }

                    SpatialFilterDefinition sfd = new SpatialFilterDefinition(localPts, Vector3d.ZAxis, 0.0, double.PositiveInfinity, double.NegativeInfinity, true);

                    SpatialFilter sf = new SpatialFilter
                    {
                        Definition = sfd
                    };

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
                            fDict.Remove(SpatialName);
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

                    clippedCount++;
                }

                tr.Commit();
                Generic.Regen();
                Generic.WriteMessage($"{clippedCount} bloc(s) délimité(s) sur {BlockSet.Length} sélectionné(s).");
            }
        }

        /// <summary>
        /// Teste si le bloc possède un SpatialFilter (xclip).
        /// </summary>
        private static bool HasSpatialFilter(Transaction tr, ObjectId blockId)
        {
            BlockReference br = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
            if (br == null || br.ExtensionDictionary == ObjectId.Null)
            {
                return false;
            }

            DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
            if (xDict != null && xDict.Contains(FilterDictName))
            {
                DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForRead) as DBDictionary;
                return fDict != null && fDict.Contains(SpatialName);
            }

            return false;
        }

        /// <summary>
        /// Supprime le SpatialFilter (xclip) du bloc s'il existe.
        /// </summary>
        private static bool RemoveSpatialFilter(Transaction tr, ObjectId blockId)
        {
            BlockReference br = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;
            if (br == null || br.ExtensionDictionary == ObjectId.Null)
            {
                return false;
            }

            DBDictionary xDict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
            if (xDict != null && xDict.Contains(FilterDictName))
            {
                DBDictionary fDict = tr.GetObject(xDict.GetAt(FilterDictName), OpenMode.ForWrite) as DBDictionary;
                if (fDict != null && fDict.Contains(SpatialName))
                {
                    fDict.Remove(SpatialName);
                    return true;
                }
            }

            return false;
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
                        if (polySeg.IntersectWith(edge) != null)
                        {
                            return true;
                        }
                    }
                }
                else if (segType == SegmentType.Arc)
                {
                    CircularArc2d polyArc = poly.GetArcSegment2dAt(i);
                    foreach (var edge in bboxEdges)
                    {
                        if (polyArc.IntersectWith(edge) != null)
                        {
                            return true;
                        }
                    }
                }
            }

            Point3d bboxCenter = new Point3d((min.X + max.X) / 2.0, (min.Y + max.Y) / 2.0, extents.MinPoint.Z);
            return bboxCenter.IsInsidePolyline(poly);
        }
    }
}