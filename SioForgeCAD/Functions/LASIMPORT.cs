using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
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
        public static void ImportLAS()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            string selectedFile = GetFile();
            if (string.IsNullOrEmpty(selectedFile))
            {
                return;
            }

            string lasFile = selectedFile;

            // Conversion LAZ automatique
            if (Path.GetExtension(selectedFile).Equals(".laz", StringComparison.OrdinalIgnoreCase))
            {
                lasFile = ConvertLAZtoLAS(selectedFile);
                if (string.IsNullOrEmpty(lasFile))
                {
                    return;
                }
            }

            // Les fichiers LAS Lidar IGN/France sont en Lambert 93 (EPSG:2154)
            const int lasEPSG = 2154;

            int drawingEPSG = GetProjectionOption(ed, lasEPSG);
            if (drawingEPSG == 0)
            {
                return;
            }

            // Transformation : LAS (source) -> Dessin (target)
            CoordinateTransform transform = new CoordinateTransform(lasEPSG, drawingEPSG);

            using (LasReader las = new LasReader(lasFile))
            {
                if (las.PointCount == 0)
                {
                    Generic.WriteMessage("Le fichier LAS ne contient aucun point.");
                    return;
                }

                ObjectId bboxId = ShowLASBoundingBox(las, transform);

                try
                {
                    PromptKeywordOptions keywordOpts = new PromptKeywordOptions("\nImporter toute la LAS ? [Oui/Non] <Non> : ", "Oui Non")
                    {
                        AllowNone = true
                    };
                    PromptResult keywordResult = ed.GetKeywords(keywordOpts);

                    if (keywordResult.Status != PromptStatus.OK && keywordResult.Status != PromptStatus.None)
                    {
                        return;
                    }

                    bool importAll = keywordResult.StringResult == "Oui";

                    // Limites englobantes et polygone dans le système de coordonnées LAS
                    double lasXmin = double.MinValue;
                    double lasXmax = double.MaxValue;
                    double lasYmin = double.MinValue;
                    double lasYmax = double.MaxValue;
                    Point3dCollection lasPolygonPts = null;

                    // Sélection de la polyligne fermée
                    if (!importAll)
                    {

                        using (Transaction trPoly = db.TransactionManager.StartTransaction())
                        {

                            Polyline selectedPoly = ed.GetPolyline("Sélectionnez une polyligne fermée de délimitation :", false, false);

                            if (selectedPoly == null)
                            {
                                return;
                            }

                            bool isClosed = selectedPoly.Closed || (selectedPoly.NumberOfVertices > 2 && selectedPoly.StartPoint.IsEqualTo(selectedPoly.EndPoint, Tolerance.Global));

                            if (!isClosed)
                            {
                                Generic.WriteMessage("La polyligne sélectionnée n'est pas fermée.");
                                return;
                            }

                            // Discrétisation des arcs (calculée une seule fois avant la boucle de points)
                            Polyline noArcPoly = selectedPoly.ToPolygon(15);
                            Point3dCollection drawingPts = noArcPoly.GetPoints().ToPoint3dCollection();
                            if (noArcPoly != selectedPoly)
                            {
                                noArcPoly.Dispose();
                            }

                            // Conversion des sommets du polygone : Dessin (WCS) -> Système LAS
                            // et calcul de la boîte englobante (Bounding Box) pour le pré-filtrage rapide
                            lasPolygonPts = new Point3dCollection();
                            lasXmin = double.MaxValue;
                            lasXmax = double.MinValue;
                            lasYmin = double.MaxValue;
                            lasYmax = double.MinValue;

                            foreach (Point3d pt in drawingPts)
                            {
                                Point2d lasPt = transform.Inverse(pt.X, pt.Y);
                                lasPolygonPts.Add(new Point3d(lasPt.X, lasPt.Y, 0));

                                if (lasPt.X < lasXmin) lasXmin = lasPt.X;
                                if (lasPt.X > lasXmax) lasXmax = lasPt.X;
                                if (lasPt.Y < lasYmin) lasYmin = lasPt.Y;
                                if (lasPt.Y > lasYmax) lasYmax = lasPt.Y;
                            }

                            trPoly.Commit();
                        }

                        Generic.WriteMessage($"Emprise polyligne (LAS) : {lasXmin:0.###} , {lasYmin:0.###} -> {lasXmax:0.###} , {lasYmax:0.###}");
                    }

                    // Cache des couleurs par classification pour éviter les allocations répétées
                    Color[] classColorCache = new Color[256];
                    for (int i = 0; i < 256; i++)
                    {
                        classColorCache[i] = GetClassificationColor((byte)i);
                    }

                    // IMPORT DES POINTS PAR LOTS (CHUNKS)
                    const int batchSize = 50000;
                    int importedCount = 0;
                    long readIndex = 0;
                    int lastPercent = -1;

                    bool previousUndoState = db.UndoRecording;
                    //db.DisableUndoRecording(true); // Évite le freeze dû à l'enregistrement Undo massif

                    try
                    {
                        using (LongOperationProcess op = new LongOperationProcess("Import LAS"))
                        {
                            op.SetTotalOperations(100);

                            bool hasMorePoints = true;
                            using (Transaction tr = db.TransactionManager.StartTransaction())
                            {
                                while (hasMorePoints)
                                {
                                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                                    int batchCount = 0;

                                    while (batchCount < batchSize)
                                    {
                                        if (!las.ReadPoint(out LasPoint pointVal))
                                        {
                                            hasMorePoints = false;
                                            break;
                                        }

                                        readIndex++;

                                        if ((readIndex & 1023) == 0) // Vérification annulation / progression tous les 1024 points
                                        {
                                            if (op.IsCanceled)
                                            {
                                                tr.Abort();
                                                Generic.WriteMessage("\nImport LAS annulé.");
                                                return;
                                            }
                                            UpdateProgressIfNeeded(las, op, ref lastPercent);
                                        }

                                        // Filtrage spatial en 2 étapes : Bounding Box rapide puis test point-dans-polygone
                                        if (!importAll)
                                        {
                                            if (pointVal.X < lasXmin || pointVal.X > lasXmax ||
                                                pointVal.Y < lasYmin || pointVal.Y > lasYmax)
                                            {
                                                continue;
                                            }

                                            if (!(pointVal.X, pointVal.Y).IsPointInsidePolygonMcMartin(lasPolygonPts))
                                            {
                                                continue;
                                            }
                                        }

                                        // Transformation LAS -> dessin
                                        Point2d converted = transform.Transform(pointVal.X, pointVal.Y);
                                        DBPoint acPoint = new DBPoint(new Point3d(converted.X, converted.Y, pointVal.Z))
                                        {
                                            Color = pointVal.HasRGB
                                                ? Color.FromRgb(pointVal.R, pointVal.G, pointVal.B)
                                                : classColorCache[pointVal.Classification]
                                        };

                                        ms.AppendEntity(acPoint);
                                        tr.AddNewlyCreatedDBObject(acPoint, true);

                                        importedCount++;
                                        batchCount++;
                                    }
                                }
                                tr.Commit();
                            }
                        }
                    }
                    finally
                    {
                        db.DisableUndoRecording(previousUndoState);
                    }

                    Generic.WriteMessage($"Import terminé : {importedCount:N0} points importés.");
                }
                finally
                {
                    HideLASBoundingBox(bboxId);
                }
            }
        }

        private static void UpdateProgressIfNeeded(LasReader las, LongOperationProcess op, ref int lastPercent)
        {
            if (las.PointCount == 0) return;

            int currentPercent = (int)((las.PointsProcessed * 100L) / las.PointCount);
            if (currentPercent > lastPercent)
            {
                Generic.WriteMessage($"Progression : {currentPercent}% - {las.PointsProcessed} points traités");

                int steps = currentPercent - lastPercent;
                for (int i = 0; i < steps; i++)
                {
                    op.UpdateProgress(); // Appelle déjà Application.DoEvents()
                }
                lastPercent = currentPercent;
            }
        }

        private static ObjectId ShowLASBoundingBox(LasReader las, CoordinateTransform transform)
        {
            Point2d p1 = transform.Transform(las.MinX, las.MinY);
            Point2d p2 = transform.Transform(las.MaxX, las.MinY);
            Point2d p3 = transform.Transform(las.MaxX, las.MaxY);
            Point2d p4 = transform.Transform(las.MinX, las.MaxY);

            double xmin = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            double xmax = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            double ymin = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            double ymax = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

            Polyline pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(xmin, ymin), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(xmax, ymin), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(xmax, ymax), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(xmin, ymax), 0, 0, 0);
            pl.Closed = true;
            ObjectId id = pl.AddToDrawing();

            Generic.WriteMessage(
                $"Emprise LAS :" +
                $"\nX : {las.MinX:0.###} -> {las.MaxX:0.###}" +
                $"\nY : {las.MinY:0.###} -> {las.MaxY:0.###}" +
                $"\nZ : {las.MinZ:0.###} -> {las.MaxZ:0.###}" +
                $"\nPoints : {las.PointCount:N0}");

            return id;
        }

        private static void HideLASBoundingBox(ObjectId obj)
        {
            if (!obj.IsNull && !obj.IsErased)
            {
                obj.EraseObject();
            }
        }

        public class CoordinateTransform
        {
            private readonly Lambert93 source;
            private readonly Lambert93 target;
            private readonly bool isIdentity;

            public CoordinateTransform(int sourceEPSG, int targetEPSG)
            {
                isIdentity = (sourceEPSG == targetEPSG);
                source = Lambert93.Get(sourceEPSG);
                target = Lambert93.Get(targetEPSG);
            }

            public Point2d Transform(double x, double y)
            {
                if (isIdentity) return new Point2d(x, y);

                var geo = source.Inverse(x, y);
                var result = target.Forward(geo.Lon, geo.Lat);
                return new Point2d(result.X, result.Y);
            }

            public Point2d Inverse(double x, double y)
            {
                if (isIdentity) return new Point2d(x, y);

                var geo = target.Inverse(x, y);
                var result = source.Forward(geo.Lon, geo.Lat);
                return new Point2d(result.X, result.Y);
            }
        }

        private static int GetProjectionOption(Editor ed, int defaultEPSG)
        {
            PromptKeywordOptions opts = new PromptKeywordOptions("\nLes fichiers LAS Lidar en FRANCE sont en Lambert93. Sélectionnez la projection du dessin courant : ");

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
            if (res.Status != PromptStatus.OK) return 0;

            switch (res.StringResult)
            {
                case "Aucune":
                case "Lambert93": return defaultEPSG;
                case "CC42": return 3942;
                case "CC43": return 3943;
                case "CC44": return 3944;
                case "CC45": return 3945;
                case "CC46": return 3946;
                case "CC47": return 3947;
                case "CC48": return 3948;
                case "CC49": return 3949;
                case "CC50": return 3950;
                default: return 0;
            }
        }

        private static Color GetClassificationColor(byte classification)
        {
            switch (classification)
            {
                case 0: return Color.FromRgb(200, 200, 200);
                case 1: return Color.FromRgb(160, 160, 160);
                case 2: return Color.FromRgb(150, 100, 50);
                case 3: return Color.FromRgb(170, 220, 80);
                case 4: return Color.FromRgb(80, 180, 80);
                case 5: return Color.FromRgb(0, 120, 0);
                case 6: return Color.FromRgb(220, 170, 120);
                case 7: return Color.FromRgb(255, 0, 0);
                case 8: return Color.FromRgb(255, 255, 0);
                case 9: return Color.FromRgb(0, 120, 255);
                case 10: return Color.FromRgb(120, 120, 120);
                case 11: return Color.FromRgb(80, 80, 80);
                case 12: return Color.FromRgb(255, 180, 0);
                case 13: return Color.FromRgb(255, 0, 255);
                case 14: return Color.FromRgb(255, 100, 255);
                case 15: return Color.FromRgb(100, 0, 100);
                case 16: return Color.FromRgb(180, 0, 180);
                case 17: return Color.FromRgb(0, 180, 180);
                case 18: return Color.FromRgb(255, 50, 50);
                default: return Color.FromRgb(100, 100, 255);
            }
        }

        private static string GetFile()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Fichiers LAS/LAZ (*.las;*.laz)|*.las;*.laz";
                dlg.Title = "Choisir un fichier LAS";
                if (dlg.ShowDialog() == DialogResult.OK) return dlg.FileName;
            }
            return null;
        }

        private static string ConvertLAZtoLAS(string lazFile)
        {
            const string LastToolDownloadPage = "https://github.com/LAStools/LAStools/releases/latest";

            bool isPathOk = false;
            string laszip;
            do
            {
                string lastoolsPath = Settings.LastoolsPath;

                if (string.IsNullOrWhiteSpace(lastoolsPath) || !Directory.Exists(lastoolsPath))
                {
                    MessageBox.Show(
                        "Le dossier LAStools\\bin n'est pas défini ou manquant.\n\n" +
                        "Veuillez télécharger LAStools et redéfinir le chemin.\n\n" +
                        "Téléchargement :\n" + LastToolDownloadPage,
                        "LAStools", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                    {
                        dlg.Description = "Sélectionnez le dossier \"LAStools\\bin\".\n\nTéléchargement :\n" + LastToolDownloadPage;
                        if (dlg.ShowDialog() != DialogResult.OK) return null;
                        lastoolsPath = dlg.SelectedPath;
                    }
                }

                laszip = Path.Combine(lastoolsPath, "laszip.exe");

                if (!File.Exists(laszip))
                {
                    MessageBox.Show(
                        "laszip.exe introuvable.\n\nTélécharger LAStools :\n" + LastToolDownloadPage,
                        "LAStools manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);

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
                Arguments = $"-i \"{lazFile}\" -o \"{output}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (ProgressDialog dlg = new ProgressDialog("Conversion LAZ → LAS en cours..."))
            {
                dlg.Show();
                Application.DoEvents();

                using (Process p = Process.Start(psi))
                {
                    while (!p.HasExited)
                    {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(50);
                    }

                    dlg.Close();

                    if (p.ExitCode != 0 || !File.Exists(output))
                    {
                        MessageBox.Show("Erreur pendant la conversion LAZ.", "LAZ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
            }

            Generic.WriteMessage("Conversion LAZ -> LAS terminée !");
            return output;
        }

        public struct LasPoint
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

        public class LasReader : IDisposable
        {
            private readonly FileStream fs;
            private readonly BinaryReader br;
            private readonly long pointOffset;
            private readonly ushort pointSize;
            private readonly byte pointFormat;
            private readonly byte[] pointBuffer;
            private readonly int rgbOffsetInPoint;
            private readonly int classOffsetInPoint;

            public uint PointCount { get; }
            private uint pointsRemaining;
            public uint PointsProcessed => PointCount - pointsRemaining;

            private readonly double scaleX;
            private readonly double scaleY;
            private readonly double scaleZ;
            private readonly double offsetX;
            private readonly double offsetY;
            private readonly double offsetZ;

            public double MinX { get; }
            public double MaxX { get; }
            public double MinY { get; }
            public double MaxY { get; }
            public double MinZ { get; }
            public double MaxZ { get; }

            public LasReader(string filename)
            {
                // Tampon de 1 Mo pour une lecture séquentielle quasi instantanée
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 1024, FileOptions.SequentialScan);
                br = new BinaryReader(fs);

                fs.Seek(0, SeekOrigin.Begin);
                string signature = new string(br.ReadChars(4));
                if (signature != "LASF")
                {
                    throw new Exception("Fichier LAS invalide (signature LASF manquante).");
                }

                fs.Seek(24, SeekOrigin.Begin);
                byte versionMajor = br.ReadByte();
                byte versionMinor = br.ReadByte();

                fs.Seek(96, SeekOrigin.Begin);
                pointOffset = br.ReadUInt32();

                fs.Seek(100, SeekOrigin.Begin);
                uint vlrCount = br.ReadUInt32();

                fs.Seek(104, SeekOrigin.Begin);
                pointFormat = br.ReadByte();
                pointSize = br.ReadUInt16();

                if (versionMajor == 1 && versionMinor >= 4)
                {
                    fs.Seek(247, SeekOrigin.Begin);
                    ulong count64 = br.ReadUInt64();
                    if (count64 > uint.MaxValue)
                    {
                        throw new Exception("Trop de points pour ce lecteur (> 4 milliards).");
                    }
                    PointCount = (uint)count64;
                }
                else
                {
                    fs.Seek(107, SeekOrigin.Begin);
                    PointCount = br.ReadUInt32();
                }

                pointsRemaining = PointCount;

                fs.Seek(131, SeekOrigin.Begin);
                scaleX = br.ReadDouble();
                scaleY = br.ReadDouble();
                scaleZ = br.ReadDouble();
                offsetX = br.ReadDouble();
                offsetY = br.ReadDouble();
                offsetZ = br.ReadDouble();

                fs.Seek(179, SeekOrigin.Begin);
                MaxX = br.ReadDouble();
                MinX = br.ReadDouble();
                MaxY = br.ReadDouble();
                MinY = br.ReadDouble();
                MaxZ = br.ReadDouble();
                MinZ = br.ReadDouble();

                // Pré-calcul des offsets fixes dans le buffer de point (zéro branchement dans la boucle)
                pointBuffer = new byte[pointSize];
                rgbOffsetInPoint = -1;

                if (pointFormat <= 5)
                {
                    classOffsetInPoint = 15;
                    if (pointFormat == 2) rgbOffsetInPoint = 20;
                    else if (pointFormat == 3 || pointFormat == 5) rgbOffsetInPoint = 28;
                }
                else
                {
                    classOffsetInPoint = 16;
                    if (pointFormat == 7 || pointFormat == 8 || pointFormat == 10) rgbOffsetInPoint = 30;
                }

                // Un seul Seek initial vers le début des points
                fs.Seek(pointOffset, SeekOrigin.Begin);

                Generic.WriteMessage(
                    $"\nLAS {versionMajor}.{versionMinor} | VLR : {vlrCount}" +
                    $"\nFormat point : {pointFormat} (Taille : {pointSize} octets)" +
                    $"\nNombre points : {PointCount:N0}" +
                    $"\nEmprise : X[{MinX:0.##}..{MaxX:0.##}] Y[{MinY:0.##}..{MaxY:0.##}] Z[{MinZ:0.##}..{MaxZ:0.##}]");
            }

            public bool ReadPoint(out LasPoint point)
            {
                point = default;
                if (pointsRemaining == 0)
                {
                    return false;
                }

                // Lecture séquentielle d'un bloc complet sans aucun Seek()
                int bytesRead = fs.Read(pointBuffer, 0, pointSize);
                if (bytesRead < pointSize)
                {
                    pointsRemaining = 0;
                    return false;
                }

                int rawX = BitConverter.ToInt32(pointBuffer, 0);
                int rawY = BitConverter.ToInt32(pointBuffer, 4);
                int rawZ = BitConverter.ToInt32(pointBuffer, 8);

                point.X = (rawX * scaleX) + offsetX;
                point.Y = (rawY * scaleY) + offsetY;
                point.Z = (rawZ * scaleZ) + offsetZ;
                point.Classification = pointBuffer[classOffsetInPoint];

                if (rgbOffsetInPoint >= 0 && pointSize >= rgbOffsetInPoint + 6)
                {
                    ushort r = BitConverter.ToUInt16(pointBuffer, rgbOffsetInPoint);
                    ushort g = BitConverter.ToUInt16(pointBuffer, rgbOffsetInPoint + 2);
                    ushort b = BitConverter.ToUInt16(pointBuffer, rgbOffsetInPoint + 4);

                    point.R = (byte)(r >> 8);
                    point.G = (byte)(g >> 8);
                    point.B = (byte)(b >> 8);
                    point.HasRGB = true;
                }

                pointsRemaining--;
                return true;
            }

            public void Dispose()
            {
                br?.Dispose();
                fs?.Dispose();
            }
        }
    }
}