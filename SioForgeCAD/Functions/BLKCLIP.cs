using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

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
                }

                tr.Commit();
            }

            Generic.Regen();
            Generic.WriteMessage($"{BlockSet.Length} bloc(s) délimité(s) avec succès.");
        }
    }
}
