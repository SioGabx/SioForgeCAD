using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    internal static class DEBUG
    {
        public static void DEBUG_RANDOM_POINTS()
        {
            Random _random = new Random();

            // Generates a random number within a range.
            int RandomNumber(int min, int max)
            {
                return _random.Next(min, max);
            }
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < 150; i++)
                {
                    double x = RandomNumber(-200, 200);
                    double y = RandomNumber(-50, 50);
                    double alti = RandomNumber(100, 120) + (RandomNumber(0, 99) * 0.01);
                    Point3d point = new Point3d(x, y, alti);
                    BlockReferences.InsertFromNameImportIfNotExist(Settings.BlkAltimetry,nameof(Settings.BlkAltimetry), new Points(point), ed.GetUSCRotation(AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", alti.ToString("#.00") } });
                }
                tr.Commit();
            }
            Generic.Command("_PLAN", "");
        }
        public static void GETOBJECTBYTESIZE()
        {
            var ed = Generic.GetEditor();
            var x = ed.GetEntity("Selectionnez une entité");
            if (x.Status == PromptStatus.OK)
            {
                Generic.WriteMessage($"Taille : {Files.FormatFileSizeFromByte(x.ObjectId.GetObjectByteSize())}");
            }
        }
        public static void TRIANGLECC()
        {
            DelaunayTriangulate.TriangulateCommand();
        }

        public static void DRAWRAINBOWLIGNES()
        {
            // Récupération du document courant (tu peux utiliser Generic.GetDocument() si tu préfères)
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Ouverture de l'espace courant (Objet ou Papier) en écriture
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                const double lineLength = 1.0;
                const double verticalSpacing = 0.2;

                // Boucle sur les 255 couleurs indexées d'AutoCAD
                for (short i = 1; i <= 255; i++)
                {
                    // Calcul de la hauteur Y (la première ligne sera à Y=0)
                    double yPosition = (i - 1) * verticalSpacing;

                    Point3d startPt = new Point3d(0, yPosition, 0);
                    Point3d endPt = new Point3d(lineLength, yPosition, 0);

                    // Création de la ligne
                    Line colorLine = new Line(startPt, endPt)
                    {
                        // Assignation de la couleur
                        ColorIndex = i
                    };

                    // Ajout à la base de données
                    btr.AppendEntity(colorLine);
                    tr.AddNewlyCreatedDBObject(colorLine, true);
                }

                tr.Commit();
            }

            // Message de confirmation
            Generic.WriteMessage("Lignes générées avec succès.");
        }
    }
}
