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

            PromptEntityOptions polyOpts = new PromptEntityOptions("\nSélectionnez la polyligne de découpe : ")
            {
                AllowNone = false
            };
            polyOpts.SetRejectMessage("\nL'objet doit être une polyligne.");
            polyOpts.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult polyRes = ed.GetEntity(polyOpts);
            if (polyRes.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
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

                // 3. Application du SpatialFilter sur chaque bloc sélectionné
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

                    // --- VÉRIFICATION DE LA COLLISION ---
                    Extents3d blockExtents = br.GeometricExtents;
                    if (!IsPolylineCollidingWithExtents(poly, blockExtents))
                    {
                        // Le bloc ne touche pas la polyligne, on passe au suivant
                        continue;
                    }

                    // Transformer les points de la polyligne dans l'espace local du bloc (OCS)
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
        /// Vérifie si la BoundingBox (Extents3d) collisionne avec les segments de la polyligne 
        /// ou si elle est contenue à l'intérieur.
        /// </summary>
        private static bool IsPolylineCollidingWithExtents(Polyline poly, Extents3d extents)
        {
            // 1. Test rapide AABB (Axis-Aligned Bounding Box) pour éliminer les cas éloignés
            Extents3d polyExtents = poly.GeometricExtents;
            if (polyExtents.MinPoint.X > extents.MaxPoint.X || polyExtents.MaxPoint.X < extents.MinPoint.X ||
                polyExtents.MinPoint.Y > extents.MaxPoint.Y || polyExtents.MaxPoint.Y < extents.MinPoint.Y)
            {
                return false;
            }

            // 2. Construction des 4 segments 2D de la BoundingBox dans le plan XY
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