using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;

using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Mist.Helpers.Projections;
using SioForgeCAD.Forms;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class LASIMPORT
    {
        [CommandMethod("IMPORTLAS")]
        public static void ImportLAS()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            string file = GetFile();

            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            // ------------------------------------------------------------
            // Conversion LAZ automatique
            // ------------------------------------------------------------

            if (Path.GetExtension(file).Equals(".laz", StringComparison.OrdinalIgnoreCase))
            {
                file = ConvertLAZtoLAS(file);

                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
            }

            // ------------------------------------------------------------
            // Projection du dessin
            // ------------------------------------------------------------

            const int drawingEPSG = 2154;

            int lasEPSG = GetProjectionOption(ed, drawingEPSG);

            if (lasEPSG == 0)
            {
                return;
            }

            CoordinateTransform transform = new CoordinateTransform(drawingEPSG, lasEPSG);

            // ------------------------------------------------------------
            // Ouverture du LAS
            //
            // Le constructeur ne lit que le HEADER.
            // Aucun point n'est encore parcouru.
            // ------------------------------------------------------------

            using (LasReader las = new LasReader(file))
            {
                // --------------------------------------------------------
                // Demande si on veut importer tout le fichier
                // --------------------------------------------------------

                PromptKeywordOptions keywordOpts = new PromptKeywordOptions("\nImporter toute la LAS ? [Oui/Non] <Non> : ", "Oui Non");

                keywordOpts.AllowNone = true;
                PromptResult keywordResult = ed.GetKeywords(keywordOpts);

                if (keywordResult.Status != PromptStatus.OK &&
                    keywordResult.Status != PromptStatus.None)
                {
                    return;
                }
                bool importAll = keywordResult.StringResult == "Oui";

                // --------------------------------------------------------
                // Limites dans le système de coordonnées LAS
                // --------------------------------------------------------

                double lasXmin = double.MinValue;
                double lasXmax = double.MaxValue;
                double lasYmin = double.MinValue;
                double lasYmax = double.MaxValue;

                // --------------------------------------------------------
                // Sélection de la zone
                // --------------------------------------------------------

                if (!importAll)
                {
                    // Affichage de l'emprise du fichier LAS
                    TransientGeometry bbox = ShowLASBoundingBox(ed, las, transform);

                    try
                    {
                        // ------------------------------------------------
                        // Demande du premier coin
                        // ------------------------------------------------

                        PromptPointResult p1 = ed.GetPoint("\nPremier coin de la zone : ");

                        if (p1.Status != PromptStatus.OK)
                        {
                            return;
                        }

                        // ------------------------------------------------
                        // Demande du deuxième coin
                        // ------------------------------------------------

                        PromptCornerOptions opts = new PromptCornerOptions("\nDeuxième coin : ", p1.Value);
                        PromptPointResult p2 = ed.GetCorner(opts);

                        if (p2.Status != PromptStatus.OK)
                        {
                            return;
                        }

                        // ------------------------------------------------
                        // Emprise sélectionnée dans le dessin
                        // ------------------------------------------------

                        double xmin = Math.Min(p1.Value.X, p2.Value.X);

                        double xmax = Math.Max(p1.Value.X, p2.Value.X);

                        double ymin = Math.Min(p1.Value.Y, p2.Value.Y);

                        double ymax = Math.Max(p1.Value.Y, p2.Value.Y);

                        // ------------------------------------------------
                        // Transformation dessin -> LAS
                        //
                        // On transforme les 4 coins plutôt que seulement
                        // 2 coins afin de gérer correctement la projection.
                        // ------------------------------------------------

                        Point2d lasP1 = transform.Inverse(xmin, ymin);

                        Point2d lasP2 = transform.Inverse(xmax, ymin);

                        Point2d lasP3 = transform.Inverse(xmax, ymax);

                        Point2d lasP4 = transform.Inverse(xmin, ymax);

                        lasXmin = Math.Min(Math.Min(lasP1.X, lasP2.X), Math.Min(lasP3.X, lasP4.X));

                        lasXmax = Math.Max(Math.Max(lasP1.X, lasP2.X), Math.Max(lasP3.X, lasP4.X));

                        lasYmin = Math.Min(Math.Min(lasP1.Y, lasP2.Y), Math.Min(lasP3.Y, lasP4.Y));

                        lasYmax = Math.Max(Math.Max(lasP1.Y, lasP2.Y), Math.Max(lasP3.Y, lasP4.Y));

                        Generic.WriteMessage($"Zone LAS : " + $"{lasXmin} , {lasYmin} -> " + $"{lasXmax} , {lasYmax}");
                    }
                    finally
                    {
                        // ------------------------------------------------
                        // Suppression de la bbox temporaire
                        // ------------------------------------------------

                        HideLASBoundingBox(bbox);
                    }
                }

                // --------------------------------------------------------
                // IMPORT DES POINTS
                // --------------------------------------------------------

                int count = 0;

                using (LongOperationProcess op = new LongOperationProcess("Import LAS"))
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                        BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        // ------------------------------------------------
                        // Le LasReader est déjà ouvert.
                        //
                        // On commence maintenant seulement à lire
                        // les points.
                        // ------------------------------------------------

                        while (las.ReadPoint(out LasPoint pt))
                        {
                            if (op.IsCanceled)
                            {
                                return;
                            }

                            // --------------------------------------------
                            // Filtrage dans les coordonnées LAS
                            // --------------------------------------------

                            if (!importAll)
                            {
                                if (pt.X < lasXmin ||
                                    pt.X > lasXmax ||
                                    pt.Y < lasYmin ||
                                    pt.Y > lasYmax)
                                {
                                    continue;
                                }
                            }

                            // --------------------------------------------
                            // Transformation LAS -> dessin
                            // --------------------------------------------

                            Point2d converted = transform.Transform(pt.X, pt.Y);

                            // --------------------------------------------
                            // Création du point AutoCAD
                            // --------------------------------------------

                            DBPoint acPoint = new DBPoint(new Point3d(converted.X, converted.Y, pt.Z));

                            // --------------------------------------------
                            // Couleur RGB
                            // --------------------------------------------

                            if (pt.HasRGB)
                            {
                                acPoint.Color = Color.FromRgb(pt.R, pt.G, pt.B);
                            }
                            else
                            {
                                acPoint.Color = GetClassificationColor(pt.Classification);
                            }

                            ms.AppendEntity(acPoint);

                            tr.AddNewlyCreatedDBObject(acPoint, true);

                            count++;

                            // --------------------------------------------
                            // Affichage progression
                            // --------------------------------------------

                            if (count % 10000 == 0)
                            {
                                Generic.WriteMessage($"{count} points");

                                System.Windows.Forms.Application.DoEvents();
                            }
                        }

                        tr.Commit();
                    }
                }

                Generic.WriteMessage($"Import terminé : {count} points");
            }
        }


        // ================================================================
        // BOUNDING BOX TRANSIENTE
        // ================================================================

        private class TransientGeometry
        {
            public Autodesk.AutoCAD.DatabaseServices.Polyline Polyline;
            public IntegerCollection TransientIds;
        }


        private static TransientGeometry ShowLASBoundingBox(Editor ed, LasReader las, CoordinateTransform transform)
        {
            // ------------------------------------------------------------
            // Transformation des 4 coins LAS -> dessin
            // ------------------------------------------------------------

            Point2d p1 = transform.Transform(las.MinX, las.MinY);

            Point2d p2 = transform.Transform(las.MaxX, las.MinY);

            Point2d p3 = transform.Transform(las.MaxX, las.MaxY);

            Point2d p4 = transform.Transform(las.MinX, las.MaxY);

            // ------------------------------------------------------------
            // On calcule l'emprise dans le dessin.
            //
            // Important :
            // on ne suppose pas que la transformation conserve
            // exactement l'orientation du rectangle.
            // ------------------------------------------------------------

            double xmin = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));

            double xmax = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));

            double ymin = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));

            double ymax = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

            // ------------------------------------------------------------
            // Création de la polyline temporaire
            // ------------------------------------------------------------

            Autodesk.AutoCAD.DatabaseServices.Polyline pl = new Autodesk.AutoCAD.DatabaseServices.Polyline();

            pl.AddVertexAt(0, new Point2d(xmin, ymin), 0, 0, 0);

            pl.AddVertexAt(1, new Point2d(xmax, ymin), 0, 0, 0);

            pl.AddVertexAt(2, new Point2d(xmax, ymax), 0, 0, 0);

            pl.AddVertexAt(3, new Point2d(xmin, ymax), 0, 0, 0);

            pl.Closed = true;

            // ------------------------------------------------------------
            // Affichage Transient
            // ------------------------------------------------------------
            TransientManager tm = TransientManager.CurrentTransientManager;
            IntegerCollection ids = new IntegerCollection();

            tm.AddTransient(pl, TransientDrawingMode.DirectShortTerm, 128, ids);

            // ------------------------------------------------------------
            // Message utilisateur
            // ------------------------------------------------------------

            Generic.WriteMessage(
                $"\nEmprise LAS : " +
                $"\nX : {las.MinX:0.###} -> {las.MaxX:0.###}" +
                $"\nY : {las.MinY:0.###} -> {las.MaxY:0.###}" +
                $"\nZ : {las.MinZ:0.###} -> {las.MaxZ:0.###}" +
                $"\nPoints : {las.PointCount}");

            return new TransientGeometry
            {
                Polyline = pl,
                TransientIds = ids
            };
        }


        private static void HideLASBoundingBox(TransientGeometry bbox)
        {
            if (bbox == null || bbox.Polyline == null)
            {
                return;
            }

            try
            {
                TransientManager tm = TransientManager.CurrentTransientManager;
                tm.EraseTransient(bbox.Polyline, bbox.TransientIds);
            }
            catch
            {
                // Rien à faire si le transient est déjà supprimé
            }
            finally
            {
                bbox.Polyline.Dispose();
            }
        }


        // ================================================================
        // TRANSFORMATION COORDONNEES
        // ================================================================

        public class CoordinateTransform
        {
            private readonly Lambert93 source;
            private readonly Lambert93 target;


            public CoordinateTransform(int sourceEPSG, int targetEPSG)
            {
                source = Lambert93.Get(sourceEPSG);

                target = Lambert93.Get(targetEPSG);
            }


            // LAS -> dessin
            public Point2d Transform(double x, double y)
            {
                var geo = source.Inverse(x, y);

                var result = target.Forward(geo.Lon, geo.Lat);

                return new Point2d(result.X, result.Y);
            }


            // dessin -> LAS
            public Point2d Inverse(double x, double y)
            {
                var geo = target.Inverse(x, y);

                var result = source.Forward(geo.Lon, geo.Lat);

                return new Point2d(result.X, result.Y);
            }
        }


        // ================================================================
        // CHOIX PROJECTION
        // ================================================================

        private static int GetProjectionOption(Editor ed, int drawingEPSG)
        {
            PromptKeywordOptions opts = new PromptKeywordOptions("\nLes fichiers LAS Lidar en FRANCE sont en Lambert93. " + "Sélectionnez la projection du dessin courant : ");

            opts.Keywords.Add("Aucune");
            opts.Keywords.Add("Lambert93");
            opts.Keywords.Add("CC42");
            opts.Keywords.Add("CC43");
            opts.Keywords.Add("CC44");
            opts.Keywords.Add("CC45");
            opts.Keywords.Add("CC46");
            opts.Keywords.Add("CC47");
            opts.Keywords.Add("CC48");
            opts.Keywords.Add("CC49");
            opts.Keywords.Add("CC50");

            opts.AllowNone = false;

            PromptResult res = ed.GetKeywords(opts);

            if (res.Status != PromptStatus.OK)
            {
                return 0;
            }

            switch (res.StringResult)
            {
                case "Aucune":
                    return drawingEPSG;

                case "Lambert93":
                    return 2154;

                case "CC42":
                    return 3942;

                case "CC43":
                    return 3943;

                case "CC44":
                    return 3944;

                case "CC45":
                    return 3945;

                case "CC46":
                    return 3946;

                case "CC47":
                    return 3947;

                case "CC48":
                    return 3948;

                case "CC49":
                    return 3949;

                case "CC50":
                    return 3950;
            }

            return 0;
        }


        // ================================================================
        // COULEURS CLASSIFICATION
        // ================================================================

        private static Color GetClassificationColor(
            byte classification)
        {
            switch (classification)
            {
                case 0: // Never classified
                    return Color.FromRgb(200, 200, 200);

                case 1: // Unassigned
                    return Color.FromRgb(160, 160, 160);

                case 2: // Ground
                    return Color.FromRgb(150, 100, 50);

                case 3: // Low vegetation
                    return Color.FromRgb(170, 220, 80);

                case 4: // Medium vegetation
                    return Color.FromRgb(80, 180, 80);

                case 5: // High vegetation
                    return Color.FromRgb(0, 120, 0);

                case 6: // Building
                    return Color.FromRgb(220, 170, 120);

                case 7: // Low point / noise
                    return Color.FromRgb(255, 0, 0);

                case 8: // Reserved
                    return Color.FromRgb(255, 255, 0);

                case 9: // Water
                    return Color.FromRgb(0, 120, 255);

                case 10: // Rail
                    return Color.FromRgb(120, 120, 120);

                case 11: // Road surface
                    return Color.FromRgb(80, 80, 80);

                case 12: // Reserved
                    return Color.FromRgb(255, 180, 0);

                case 13: // Wire guard
                    return Color.FromRgb(255, 0, 255);

                case 14: // Wire conductor
                    return Color.FromRgb(255, 100, 255);

                case 15: // Transmission tower
                    return Color.FromRgb(100, 0, 100);

                case 16: // Wire connector
                    return Color.FromRgb(180, 0, 180);

                case 17: // Bridge deck
                    return Color.FromRgb(0, 180, 180);

                case 18: // High noise
                    return Color.FromRgb(255, 50, 50);


                default:
                    // Classes utilisateur 19-255
                    return Color.FromRgb(100, 100, 255);
            }
        }


        // ================================================================
        // SELECTION FICHIER
        // ================================================================

        private static string GetFile()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Fichiers LAS/LAZ (*.las;*.laz)|*.las;*.laz",
                Title = "Choisir un fichier LAS"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.FileName;
            }

            return null;
        }


        // ================================================================
        // CONVERSION LAZ -> LAS
        // ================================================================

        private static string ConvertLAZtoLAS(string lazFile)
        {
            const string LastToolDownloadPage = "https://github.com/LAStools/LAStools/releases/latest";

            bool isPathOk = false;

            string laszip = string.Empty;

            do
            {
                string lastoolsPath =
                    Settings.LastoolsPath;

                if (string.IsNullOrWhiteSpace(lastoolsPath) || !Directory.Exists(lastoolsPath))
                {
                    MessageBox.Show("Le dossier LAStools\\bin n'est pas défini " + "ou manquant.\n\n" + "Veuillez télécharger LAStools et redéfinir " + "le chemin.\n\n" + "Téléchargement :\n" + LastToolDownloadPage, "LAStools", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    FolderBrowserDialog dlg = new FolderBrowserDialog
                    {
                        Description = "Sélectionnez le dossier " + "\"LAStools\\bin\".\n\n" + "Téléchargement :\n" + LastToolDownloadPage
                    };

                    if (dlg.ShowDialog() != DialogResult.OK)
                    {
                        return null;
                    }
                    lastoolsPath = dlg.SelectedPath;
                }

                laszip = Path.Combine(lastoolsPath, "laszip.exe");

                if (!File.Exists(laszip))
                {
                    MessageBox.Show("laszip.exe introuvable.\n\n" + "Télécharger LAStools :\n" + LastToolDownloadPage, "LAStools manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    Settings.LastoolsPath = string.Empty;
                    continue;
                }

                Settings.LastoolsPath = lastoolsPath;

                isPathOk = true;

            } while (!isPathOk);


            string output = Path.Combine(Path.GetDirectoryName(lazFile), Path.GetFileNameWithoutExtension(lazFile) + ".las");

            Generic.WriteMessage("\nConversion LAZ -> LAS...");


            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = laszip,
                Arguments = "-i \"" + lazFile + "\" -o \"" + output + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };


            using (ProgressDialog dlg = new ProgressDialog("Conversion LAZ → LAS en cours..."))
            {
                dlg.Show();
                System.Windows.Forms.Application.DoEvents();

                using (Process p = Process.Start(psi))
                {
                    while (!p.HasExited)
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(50);
                    }

                    dlg.Close();

                    if (p.ExitCode != 0)
                    {
                        MessageBox.Show("Erreur pendant la conversion LAZ.", "LAZ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
            }

            Generic.WriteMessage("Conversion LAZ -> LAS terminée !");

            return output;
        }


        // ================================================================
        // LAS POINT
        // ================================================================

        public class LasPoint
        {
            public double X;
            public double Y;
            public double Z;
            public byte R;
            public byte G;
            public byte B;
            public bool HasRGB;

            public byte Classification;
        }


        // ================================================================
        // LAS READER
        // ================================================================

        public class LasReader : IDisposable
        {
            private readonly BinaryReader br;
            private readonly long pointOffset;
            private readonly ushort pointSize;
            private readonly byte pointFormat;
            private uint pointCount;
            private readonly double scaleX;
            private readonly double scaleY;
            private readonly double scaleZ;
            private readonly double offsetX;
            private readonly double offsetY;
            private readonly double offsetZ;


            // ------------------------------------------------------------
            // BOUNDING BOX DU FICHIER
            // ------------------------------------------------------------

            public double MinX { get; private set; }

            public double MaxX { get; private set; }

            public double MinY { get; private set; }

            public double MaxY { get; private set; }

            public double MinZ { get; private set; }

            public double MaxZ { get; private set; }


            public uint PointCount
            {
                get
                {
                    return pointCount;
                }
            }


            public LasReader(string filename)
            {
                br = new BinaryReader(File.OpenRead(filename));


                // --------------------------------------------------------
                // Signature LAS
                // --------------------------------------------------------

                br.BaseStream.Seek(0, SeekOrigin.Begin);

                string signature = new string(br.ReadChars(4));

                if (signature != "LASF")
                {
                    throw new System.Exception("Fichier LAS invalide");
                }


                // --------------------------------------------------------
                // Version LAS
                // --------------------------------------------------------

                br.BaseStream.Seek(24, SeekOrigin.Begin);

                byte versionMajor = br.ReadByte();
                byte versionMinor = br.ReadByte();


                // --------------------------------------------------------
                // Offset début des points
                // --------------------------------------------------------

                br.BaseStream.Seek(96, SeekOrigin.Begin);
                pointOffset = br.ReadUInt32();


                // --------------------------------------------------------
                // Nombre de VLR
                // --------------------------------------------------------

                br.BaseStream.Seek(100, SeekOrigin.Begin);
                uint vlrCount = br.ReadUInt32();
                Generic.WriteMessage($"Nombre VLR : {vlrCount}");


                // --------------------------------------------------------
                // Format du point
                // --------------------------------------------------------

                br.BaseStream.Seek(104, SeekOrigin.Begin);
                pointFormat = br.ReadByte();


                // --------------------------------------------------------
                // Taille du point
                // --------------------------------------------------------

                pointSize = br.ReadUInt16();


                // --------------------------------------------------------
                // Nombre de points
                // --------------------------------------------------------

                if (versionMajor == 1 && versionMinor >= 4)
                {
                    // LAS 1.4
                    br.BaseStream.Seek(247, SeekOrigin.Begin);

                    ulong count64 = br.ReadUInt64();

                    if (count64 > uint.MaxValue)
                    {
                        throw new System.Exception("Trop de points pour ce lecteur");
                    }

                    pointCount = (uint)count64;
                }
                else
                {
                    // LAS <= 1.3

                    br.BaseStream.Seek(107, SeekOrigin.Begin);
                    pointCount = br.ReadUInt32();
                }


                // --------------------------------------------------------
                // Echelles
                // --------------------------------------------------------

                br.BaseStream.Seek(131, SeekOrigin.Begin);
                scaleX = br.ReadDouble();
                scaleY = br.ReadDouble();
                scaleZ = br.ReadDouble();


                // --------------------------------------------------------
                // Offsets
                // --------------------------------------------------------

                offsetX = br.ReadDouble();
                offsetY = br.ReadDouble();
                offsetZ = br.ReadDouble();


                // --------------------------------------------------------
                // BOUNDING BOX LAS
                //
                // Offset 179 :
                //
                // Max X
                // Min X
                // Max Y
                // Min Y
                // Max Z
                // Min Z
                // --------------------------------------------------------

                br.BaseStream.Seek(179, SeekOrigin.Begin);
                MaxX = br.ReadDouble();
                MinX = br.ReadDouble();
                MaxY = br.ReadDouble();
                MinY = br.ReadDouble();
                MaxZ = br.ReadDouble();
                MinZ = br.ReadDouble();


                // --------------------------------------------------------
                // Aller au premier point
                // --------------------------------------------------------

                br.BaseStream.Seek(
                    pointOffset,
                    SeekOrigin.Begin);


                Generic.WriteMessage(
                    $"\nLAS {versionMajor}.{versionMinor}" +
                    $"\nFormat point : {pointFormat}" +
                    $"\nTaille point : {pointSize}" +
                    $"\nNombre points : {pointCount}" +
                    $"\nMin X : {MinX}" +
                    $"\nMax X : {MaxX}" +
                    $"\nMin Y : {MinY}" +
                    $"\nMax Y : {MaxY}" +
                    $"\nMin Z : {MinZ}" +
                    $"\nMax Z : {MaxZ}");
            }


            // ============================================================
            // LECTURE POINT
            // ============================================================

            public bool ReadPoint(out LasPoint point)
            {
                point = null;

                if (pointCount == 0)
                {
                    return false;
                }

                long start = br.BaseStream.Position;


                LasPoint p = new LasPoint();


                // --------------------------------------------------------
                // Coordonnées brutes
                // --------------------------------------------------------

                int rawX = br.ReadInt32();
                int rawY = br.ReadInt32();
                int rawZ = br.ReadInt32();


                // --------------------------------------------------------
                // Coordonnées réelles
                // --------------------------------------------------------

                p.X = (rawX * scaleX) + offsetX;

                p.Y = (rawY * scaleY) + offsetY;

                p.Z = (rawZ * scaleZ) + offsetZ;


                // --------------------------------------------------------
                // Intensité
                // --------------------------------------------------------

                br.ReadUInt16();


                // --------------------------------------------------------
                // Return Number / Flags
                // --------------------------------------------------------

                br.ReadByte();


                // --------------------------------------------------------
                // Classification Flags
                // --------------------------------------------------------

                br.ReadByte();


                // --------------------------------------------------------
                // Classification
                // --------------------------------------------------------

                p.Classification = br.ReadByte();


                // --------------------------------------------------------
                // User Data
                // --------------------------------------------------------

                br.ReadByte();


                // --------------------------------------------------------
                // Scan Angle
                // --------------------------------------------------------

                br.ReadInt16();


                // --------------------------------------------------------
                // Point Source ID
                // --------------------------------------------------------

                br.ReadUInt16();


                // --------------------------------------------------------
                // GPS Time
                // --------------------------------------------------------

                br.ReadDouble();


                // --------------------------------------------------------
                // Lecture du reste du point
                // --------------------------------------------------------

                long afterBasic = br.BaseStream.Position;

                int remaining = pointSize - (int)(afterBasic - start);

                byte[] buffer = br.ReadBytes(remaining);


                // --------------------------------------------------------
                // RGB
                //
                // Formats 2 et 3
                // --------------------------------------------------------

                if (pointFormat == 2 ||
                    pointFormat == 3)
                {
                    if (buffer.Length >= 6)
                    {
                        int index =
                            buffer.Length - 6;

                        ushort r = BitConverter.ToUInt16(buffer, index);
                        ushort g = BitConverter.ToUInt16(buffer, index + 2);
                        ushort b = BitConverter.ToUInt16(buffer, index + 4);

                        // LAS stocke généralement RGB sur 16 bits.
                        // Conversion vers AutoCAD RGB 8 bits.

                        p.R = (byte)(r >> 8);

                        p.G = (byte)(g >> 8);

                        p.B = (byte)(b >> 8);
                        p.HasRGB = true;
                    }
                }


                pointCount--;

                point = p;

                return true;
            }


            // ============================================================
            // DISPOSE
            // ============================================================

            public void Dispose()
            {
                br?.Dispose();
            }
        }
    }
}