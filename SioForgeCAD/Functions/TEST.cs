using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SioForgeCAD.Functions
{
    public static class TEST
    {
        public static void AddAltimetryBlocksAtVertices()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. Sélection des courbes (via votre méthode d'extension ou un filtre standard)
            TypedValue[] filter = new TypedValue[]
            {
            new TypedValue((int)DxfCode.Operator, "<OR"),
            new TypedValue((int)DxfCode.Start, "LINE"),
            new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
            new TypedValue((int)DxfCode.Start, "POLYLINE"),
            new TypedValue((int)DxfCode.Operator, "OR>")
            };

            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\nSélectionnez les courbes (Lignes, Poly 2D/3D) : ";

            PromptSelectionResult selRes = ed.GetSelection(opts, new SelectionFilter(filter));

            if (selRes.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in selRes.Value)
                {
                    Curve curve = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Curve;
                    if (curve == null) continue;

                    // 2. Extraction des points (Vertices)
                    IEnumerable<Point3d> vertices = GetVertices(curve);

                    foreach (Point3d pt in vertices)
                    {
                        // Calcul de la rotation selon l'UCS (comme demandé)
                        double rotation = ed.CurrentUserCoordinateSystem.CoordinateSystem3d.Zaxis.AngleOnPlane(
                            new Plane(Point3d.Origin, Vector3d.ZAxis));

                        // 3. Appel de votre méthode personnalisée
                        // Note : J'adapte l'appel selon votre snippet
                        try
                        {
                            string AltimetrieStr = pt.Z.ToString("#.00");
                            Dictionary<string, string> AltimetrieValue = new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };

                            
                            if (!new Points(pt).SCG.IsThereABlockReference(Settings.BlkAltimetry, AltimetrieStr, out var BlkFound) && BlkFound?.Layer != Layers.GetCurrentLayerName())
                            {
                                BlockReferences.InsertFromNameImportIfNotExist(
                                 Settings.BlkAltimetry,
                                 nameof(Settings.BlkAltimetry),
                                 new Points(pt),
                                 rotation,
                                AltimetrieValue
                             );
                            }


                            // On assume que 'insertionPoint' est le point du vertex 'pt'
                            // et 'altValue' est le Z du point.

                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\nErreur lors de l'insertion au point {pt}: {ex.Message}");
                        }
                    }
                }
                tr.Commit();
            }
            ed.WriteMessage("\nOpération terminée.");
        }

        /// <summary>
        /// Extrait tous les sommets d'une courbe de manière générique
        /// </summary>
        private static IEnumerable<Point3d> GetVertices(Curve curve)
        {
            var points = new List<Point3d>();

            if (curve is Line line)
            {
                points.Add(line.StartPoint);
                points.Add(line.EndPoint);
            }
            else if (curve is Polyline pline) // Polyline optimisée (LW)
            {
                for (int i = 0; i < pline.NumberOfVertices; i++)
                    points.Add(pline.GetPoint3dAt(i));
            }
            else if (curve is Polyline3d pline3d) // Polyline 3D
            {
                // Les sommets d'une Poly3d sont des entités enfants
                foreach (ObjectId id in pline3d)
                {
                    var v = id.GetObject(OpenMode.ForRead) as PolylineVertex3d;
                    if (v != null) points.Add(v.Position);
                }
            }
            else
            {
                // Fallback pour les autres types de courbes (Splines, etc.)
                // On prend le début et la fin au minimum
                points.Add(curve.StartPoint);
                points.Add(curve.EndPoint);
            }

            return points;
        }
    }
}