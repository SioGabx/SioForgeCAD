using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class CREATELABELSONCONTOURELEVATIONS
    {
        public static void Create()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // ---------------------------------------------------------
            // Demande de l'intervalle
            // ---------------------------------------------------------
            PromptDistanceOptions distOpts =
                new PromptDistanceOptions("\nIntervalle entre les textes (m) : ");

            distOpts.AllowZero = false;
            distOpts.AllowNegative = false;

            PromptDoubleResult distRes = ed.GetDistance(distOpts);

            if (distRes.Status != PromptStatus.OK)
                return;

            double interval = distRes.Value;

            // ---------------------------------------------------------
            // Sélection des polylignes
            // ---------------------------------------------------------
            PromptSelectionOptions selOpts =
                new PromptSelectionOptions();

            selOpts.MessageForAdding =
                "\nSélectionnez les polylignes ayant une élévation : ";

            // Filtre : LWPOLYLINE et POLYLINE 2D
            SelectionFilter filter = new SelectionFilter(
                new TypedValue[]
                {
                new TypedValue(
                    (int)DxfCode.Operator,
                    "<OR"
                ),

                new TypedValue(
                    (int)DxfCode.Start,
                    "LWPOLYLINE"
                ),

                new TypedValue(
                    (int)DxfCode.Start,
                    "POLYLINE"
                ),

                new TypedValue(
                    (int)DxfCode.Operator,
                    "OR>"
                )
                });

            PromptSelectionResult selRes =
                ed.GetSelection(selOpts, filter);

            if (selRes.Status != PromptStatus.OK)
                return;

            // ---------------------------------------------------------
            // Transaction
            // ---------------------------------------------------------
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead);

                BlockTableRecord modelSpace =
                    (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite);

                foreach (SelectedObject selected in selRes.Value)
                {
                    if (selected == null)
                        continue;

                    Entity entity =
                        tr.GetObject(
                            selected.ObjectId,
                            OpenMode.ForRead) as Entity;

                    if (entity == null)
                        continue;

                    // -------------------------------------------------
                    // POLYLIGNE 2D (LWPOLYLINE)
                    // -------------------------------------------------
                    if (entity is Polyline pline)
                    {
                        double elevation = pline.Elevation;

                        PlaceTextsOnPolyline(
                            pline,
                            elevation,
                            interval,
                            modelSpace,
                            tr);
                    }

                    // -------------------------------------------------
                    // Ancienne POLYLINE 2D
                    // -------------------------------------------------
                    else if (entity is Polyline2d pline2d)
                    {
                        // La Polyline2d ne possède pas directement
                        // la propriété Elevation comme la LWPOLYLINE.
                        // On récupère l'altitude du premier sommet.

                        double elevation = pline2d.GetElevation();

                        PlaceTextsOnPolyline2d(
                            pline2d,
                            elevation,
                            interval,
                            modelSpace,
                            tr);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage(      "Les textes d'élévation ont été placés.");
        }

        // =============================================================
        // LWPOLYLINE
        // =============================================================
        private static void PlaceTextsOnPolyline(
            Polyline pline,
            double elevation,
            double interval,
            BlockTableRecord modelSpace,
            Transaction tr)
        {
            double length = pline.Length;

            // Distance du premier texte.
            // Ici : X mètres depuis le début.
            for (double distance = 0.0;
                 distance <= length;
                 distance += interval)
            {
                try
                {
                    Point3d point =
                        pline.GetPointAtDist(distance);

                    // Tangente de la courbe
                    Vector3d tangent =
                        pline.GetFirstDerivative(point);

                    if (tangent.Length < 1e-9)
                        continue;

                    tangent = tangent.GetNormal();

                    // Angle de la tangente dans le plan XY
                    double angle =
                        System.Math.Atan2(
                            tangent.Y,
                            tangent.X);

                    // Évite les textes complètement à l'envers.
                    if (angle > System.Math.PI / 2.0 &&
                        angle < 3.0 * System.Math.PI / 2.0)
                    {
                        angle += System.Math.PI;
                    }

                    // Position avec Z = élévation
                    Point3d textPosition =
                        new Point3d(
                            point.X,
                            point.Y,
                            elevation);

                    DBText text = new DBText();

                    text.Position = textPosition;

                    // Affichage de l'élévation
                    text.TextString =
                        elevation.ToString("0.00");

                    // Hauteur du texte
                    text.Height = 0.20;

                    // Rotation suivant la courbe
                    text.Rotation = angle;

                    // Alignement centré
                    text.HorizontalMode =
                        TextHorizontalMode.TextCenter;

                    text.VerticalMode =
                        TextVerticalMode.TextVerticalMid;

                    text.AlignmentPoint =
                        textPosition;

                    // Le texte est dans le même plan XY
                    // avec son élévation réelle.
                    text.Normal = Vector3d.ZAxis;

                    modelSpace.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);
                }
                catch
                {
                    // On ignore un point impossible à calculer
                }
            }
        }

        // =============================================================
        // POLYLINE2D
        // =============================================================
        private static void PlaceTextsOnPolyline2d(
            Polyline2d pline,
            double elevation,
            double interval,
            BlockTableRecord modelSpace,
            Transaction tr)
        {
            // Conversion temporaire en Polyline moderne
            // pour faciliter le calcul des distances et tangentes.

            Polyline converted =
                new Polyline();

            int index = 0;

            foreach (ObjectId vertexId in pline)
            {
                Vertex2d vertex =
                    tr.GetObject(
                        vertexId,
                        OpenMode.ForRead)
                    as Vertex2d;

                if (vertex == null)
                    continue;

                Point3d p = vertex.Position;

                converted.AddVertexAt(
                    index++,
                    new Point2d(p.X, p.Y),
                    0.0,
                    0.0,
                    0.0);
            }

            converted.Elevation = elevation;

            PlaceTextsOnPolyline(
                converted,
                elevation,
                interval,
                modelSpace,
                tr);

            converted.Dispose();
        }
    }

}
