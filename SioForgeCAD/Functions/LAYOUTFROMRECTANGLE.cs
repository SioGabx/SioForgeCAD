using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using System;

namespace SioForgeCAD.Functions
{
    internal static class LAYOUTFROMRECTANGLE
    {
        public static void Execute()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;




            // 1. Sélectionner le rectangle (polyligne)
            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez le rectangle délimitant la zone : ");
            peo.SetRejectMessage("\nVeuillez sélectionner une polyligne.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            // 2. Demander l'échelle
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez le dénominateur de l'échelle (ex: 500 pour 1/500e) : ")
            {
                AllowNegative = false,
                AllowZero = false,
                DefaultValue = 500
            };

            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;

            double echelleDenominateur = pdr.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Récupérer la polyligne sélectionnée
                Polyline poly = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                Extents3d ext = poly.GeometricExtents;

                // Calculer les dimensions et le centre dans l'Espace Objet
                double modelWidth = ext.MaxPoint.X - ext.MinPoint.X;
                double modelHeight = ext.MaxPoint.Y - ext.MinPoint.Y;
                Point2d modelCenter = new Point2d(
                    ext.MinPoint.X + (modelWidth / 2),
                    ext.MinPoint.Y + (modelHeight / 2)
                );

                // 3. Calculs pour l'Espace Papier
                // Rapport : 1000 unités papier = X unités dessin
                double customScale = 1000.0 / echelleDenominateur;

                double paperWidth = modelWidth * customScale;
                double paperHeight = modelHeight * customScale;

                // 4. Création de la présentation (Layout)
                LayoutManager lm = LayoutManager.Current;
                string layoutName = "Plan_1-" + echelleDenominateur + "_" + DateTime.Now.ToString("HHmmss");

                ObjectId layoutId = lm.CreateLayout(layoutName);
                lm.CurrentLayout = layoutName; // Rend la présentation active

                Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForWrite);

                // 5. Création de la fenêtre (Viewport)
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);

                Viewport vp = new Viewport();
                vp.SetDatabaseDefaults();

                // Le centre du viewport dans l'espace papier (on le place à partir de 0,0)
                vp.CenterPoint = new Point3d(paperWidth / 2, paperHeight / 2, 0);
                vp.Width = paperWidth;
                vp.Height = paperHeight;

                btr.AppendEntity(vp);
                tr.AddNewlyCreatedDBObject(vp, true);

                // Appliquer la vue et l'échelle
                vp.ViewCenter = modelCenter;
                vp.CustomScale = customScale;

                // Verrouiller la vue (recommandé) et activer le viewport
                vp.Locked = true;
                vp.On = true;

                tr.Commit();

                Generic.WriteMessage($"Succès ! Présentation '{layoutName}' créée.");
                Generic.WriteMessage($"Dimensions Papier générées : {paperWidth:F2} x {paperHeight:F2} mm.");
            }
        }
    }
}
