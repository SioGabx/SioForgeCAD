using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    internal class BLKCLIP
    {
        const string FilterDictName = "ACAD_FILTER";
        const string SpatialName = "SPATIAL";

        public static void ClipBlocksWithPolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. Sélection de 1 ou plusieurs blocs
            TypedValue[] blockFilter = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT")
            };

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSélectionnez le ou les blocs à délimiter : "
            };

            PromptSelectionResult selRes = ed.GetSelection(selOpts, new SelectionFilter(blockFilter));
            if (selRes.Status != PromptStatus.OK)
                return;

            SelectionSet blockSet = selRes.Value;

            // 2. Sélection de la polyligne de découpe
            PromptEntityOptions polyOpts = new PromptEntityOptions("\nSélectionnez la polyligne de découpe : ")
            {
                AllowNone = false
            };
            polyOpts.SetRejectMessage("\nL'objet doit être une polyligne.");
            polyOpts.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult polyRes = ed.GetEntity(polyOpts);
            if (polyRes.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Récupération des points de la polyligne dans le repère WCS
                Polyline poly = tr.GetObject(polyRes.ObjectId, OpenMode.ForRead) as Polyline;
                if (poly == null)
                    return;

                Point3dCollection polyWcsPoints = new Point3dCollection();
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    polyWcsPoints.Add(poly.GetPoint3dAt(i));
                }

                // 3. Application du SpatialFilter sur chaque bloc sélectionné
                foreach (SelectedObject selectedObj in blockSet)
                {
                    if (selectedObj == null) continue;

                    BlockReference br = tr.GetObject(selectedObj.ObjectId, OpenMode.ForWrite) as BlockReference;
                    if (br == null) continue;

                    // Transformer les points de la polyligne dans l'espace local du bloc (OCS)
                    Matrix3d invTransform = br.BlockTransform.Inverse();
                    Point2dCollection localPts = new Point2dCollection();

                    foreach (Point3d ptWcs in polyWcsPoints)
                    {
                        Point3d localPt = ptWcs.TransformBy(invTransform);
                        localPts.Add(new Point2d(localPt.X, localPt.Y));
                    }

                    // Création de la définition du filtre spatial
                    SpatialFilterDefinition sfd = new SpatialFilterDefinition(
                        localPts,
                        Vector3d.ZAxis,
                        0.0,
                        0.0,
                        0.0,
                        true
                    );

                    SpatialFilter sf = new SpatialFilter
                    {
                        Definition = sfd
                    };

                    // Ajout ou mise à jour du dictionnaire d'extension
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

            ed.Regen();
            ed.WriteMessage($"\n{blockSet.Count} bloc(s) délimité(s) avec succès.");
        }
    }
}
