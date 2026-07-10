using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class LAZPOINTSIMPORT
    {

        private struct ImportZone
        {
            public double MinX;
            public double MaxX;
            public double MinY;
            public double MaxY;


            public bool Contains(double x, double y)
            {
                return x >= MinX &&
                       x <= MaxX &&
                       y >= MinY &&
                       y <= MaxY;
            }
        }



        public static void Import()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            string FileName = GetFile();


            if (string.IsNullOrEmpty(FileName))
            {
                return;
            }



            // Sélection de la zone d'import
            ImportZone zone;

            if (!GetImportZone(ed, out zone))
            {
                return;
            }

            using (LongOperationProcess LongOperation = new LongOperationProcess("Import points..."))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = tr.GetObject(
                            db.BlockTableId,
                            OpenMode.ForRead) as BlockTable;


                        BlockTableRecord btr = tr.GetObject(
                            bt[BlockTableRecord.ModelSpace],
                            OpenMode.ForWrite) as BlockTableRecord;



                        int Count = 0;
                        int Ignored = 0;



                        using (StreamReader reader = new StreamReader(FileName))
                        {
                            string line;


                            while ((line = reader.ReadLine()) != null)
                            {

                                if (LongOperation.IsCanceled)
                                {
                                    return;
                                }

                                string[] values = line.Split(
                                    new[] { ' ', '\t' },
                                    StringSplitOptions.RemoveEmptyEntries);



                                if (values.Length < 3)
                                {
                                    continue;
                                }

                                if (
                                    double.TryParse(values[0],
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out double East) &&


                                    double.TryParse(values[1],
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out double North) &&


                                    double.TryParse(values[2],
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out double Z)
                                   )
                                {


                                    // Filtre zone rectangle
                                    if (!zone.Contains(East, North))
                                    {
                                        Ignored++;
                                        continue;
                                    }



                                    DBPoint Point = new DBPoint(
                                        new Point3d(East, North, Z));



                                    btr.AppendEntity(Point);
                                    tr.AddNewlyCreatedDBObject(Point, true);



                                    Count++;
                                }




                                if (Count % 5000 == 0)
                                {
                                    Generic.WriteMessage(
                                        $"Import : {Count} points");


                                    Application.DoEvents();
                                }

                            }
                        }




                        Generic.WriteMessage(
                            $"Import terminé : {Count} points créés");


                        Generic.WriteMessage(
                            $"Points ignorés hors zone : {Ignored}");



                        tr.Commit();

                    }
                    catch (Exception ex)
                    {
                        Generic.WriteMessage(
                            $"Erreur import TXT : {ex.Message}");
                    }
                }
            }


            ed.UpdateScreen();
        }






        private static bool GetImportZone(Editor ed, out ImportZone zone)
        {
            zone = new ImportZone();



            PromptPointResult p1 = ed.GetPoint(
                "\nPremier coin de la zone d'import : ");



            if (p1.Status != PromptStatus.OK)
            {
                return false;
            }

            PromptPointResult p2 = ed.GetPoint(
                "\nDeuxième coin de la zone d'import : ");



            if (p2.Status != PromptStatus.OK)
            {
                return false;
            }

            zone.MinX = Math.Min(
                p1.Value.X,
                p2.Value.X);


            zone.MaxX = Math.Max(
                p1.Value.X,
                p2.Value.X);



            zone.MinY = Math.Min(
                p1.Value.Y,
                p2.Value.Y);


            zone.MaxY = Math.Max(
                p1.Value.Y,
                p2.Value.Y);




            ed.WriteMessage(
                $"\nZone sélectionnée : " +
                $"X={zone.MinX} -> {zone.MaxX} " +
                $"Y={zone.MinY} -> {zone.MaxY}");



            return true;
        }






        private static string GetFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Fichier TXT (*.txt)|*.txt",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                InitialDirectory = Generic.GetCurrentDocumentPath(),
                Title = "Sélectionnez un fichier TXT de points"
            };



            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }



            return string.Empty;
        }

    }
}