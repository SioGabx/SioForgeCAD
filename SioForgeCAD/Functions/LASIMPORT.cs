using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using System.Diagnostics;
using System.Net;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;

using System;
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


            // Conversion LAZ automatique
            if (Path.GetExtension(file).Equals(".laz", StringComparison.OrdinalIgnoreCase))
            {
                file = ConvertLAZtoLAS(file);

                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
            }

            PromptPointResult p1 = ed.GetPoint("\nPremier coin de la zone : ");
            if (p1.Status != PromptStatus.OK)
            {
                return;
            }
            PromptCornerOptions opts = new PromptCornerOptions("\nDeuxième coin : ", p1.Value);
            PromptPointResult p2 = ed.GetCorner(opts);

            if (p2.Status != PromptStatus.OK)
            {
                return;
            }

            double xmin = Math.Min(p1.Value.X, p2.Value.X);
            double xmax = Math.Max(p1.Value.X, p2.Value.X);

            double ymin = Math.Min(p1.Value.Y, p2.Value.Y);
            double ymax = Math.Max(p1.Value.Y, p2.Value.Y);

            int count = 0;

            using (LongOperationProcess op = new LongOperationProcess("Import LAS"))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    using (LasReader las = new LasReader(file))
                    {
                        while (las.ReadPoint(out LasPoint pt))
                        {
                            if (op.IsCanceled)
                            {
                                return;
                            }

                            if (pt.X < xmin || pt.X > xmax || pt.Y < ymin || pt.Y > ymax)
                            {
                                continue;
                            }

                            DBPoint acPoint = new DBPoint(new Point3d(pt.X, pt.Y, pt.Z));

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


                            if (count % 5000 == 0)
                            {
                                Generic.WriteMessage($"\n{count} points");
                                System.Windows.Forms.Application.DoEvents();
                            }
                        }
                    }


                    tr.Commit();
                }
            }


            Generic.WriteMessage($"\nImport terminé : {count} points");
        }

        private static Color GetClassificationColor(byte classification)
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
                    return Color.FromRgb(
                        100,
                        100,
                        255);
            }
        }

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

        private static string ConvertLAZtoLAS(string lazFile)
        {
            string lastoolsPath = Settings.LastoolsPath;


            if (string.IsNullOrWhiteSpace(lastoolsPath) || !Directory.Exists(lastoolsPath))
            {
                FolderBrowserDialog dlg = new FolderBrowserDialog();

                dlg.Description = "Sélectionnez le dossier LAStools";


                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return null;
                }
                lastoolsPath = dlg.SelectedPath;


                Settings.LastoolsPath = lastoolsPath;
            }

            string laszip = Path.Combine(lastoolsPath, "laszip.exe");


            if (!File.Exists(laszip))
            {
                MessageBox.Show("laszip.exe introuvable.\n\nTélécharger LAStools :\nhttps://github.com/LAStools/LAStools/releases/latest", "LAStools manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Settings.LastoolsPath = "";
                return null;
            }

            string output = Path.Combine(Path.GetDirectoryName(lazFile), Path.GetFileNameWithoutExtension(lazFile) + ".las");

            Generic.WriteMessage("\nConversion LAZ -> LAS...");

            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = laszip;
            psi.Arguments = "-i \"" + lazFile + "\" -o \"" + output + "\"";


            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            using (Process p = Process.Start(psi))
            {
                while (!p.HasExited)
                {
                    System.Windows.Forms.Application.DoEvents();
                }

                if (p.ExitCode != 0)
                {
                    MessageBox.Show("Erreur pendant la conversion LAZ.");
                    return null;
                }
            }

            return output;
        }




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


        /*
            Le format LAS 1.4 point 6 est :
                X                    4
                Y                    4
                Z                    4
                Intensity            2
                Return Flags         1
                Classification Flags 1
                Classification       1
                User Data            1
                Scan Angle           2
                Point Source ID      2
                GPS Time             8

        */
        public class LasReader : IDisposable
        {
            private BinaryReader br;
            private long pointOffset;
            private ushort pointSize;
            private byte pointFormat;
            private uint pointCount;
            private double scaleX;
            private double scaleY;
            private double scaleZ;
            private double offsetX;
            private double offsetY;
            private double offsetZ;



            public LasReader(string filename)
            {
                br = new BinaryReader(File.OpenRead(filename));
                // Vérification signature LAS
                br.BaseStream.Seek(0, SeekOrigin.Begin);

                string signature =
                    new string(br.ReadChars(4));

                if (signature != "LASF")
                {
                    throw new System.Exception("Fichier LAS invalide");
                }
                // Version LAS
                br.BaseStream.Seek(24, SeekOrigin.Begin);

                byte versionMajor = br.ReadByte();
                byte versionMinor = br.ReadByte();

                // Offset début des points
                br.BaseStream.Seek(96, SeekOrigin.Begin);
                pointOffset = br.ReadUInt32();



                // Nombre de VLR
                br.BaseStream.Seek(100, SeekOrigin.Begin);

                uint vlrCount = br.ReadUInt32();

                Generic.WriteMessage($"\nNombre VLR : {vlrCount}");

                // Format du point
                br.BaseStream.Seek(104, SeekOrigin.Begin);
                pointFormat = br.ReadByte();

                // Taille point
                pointSize = br.ReadUInt16();



                // Nombre de points
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

                // Echelles
                br.BaseStream.Seek(131, SeekOrigin.Begin);

                scaleX = br.ReadDouble();
                scaleY = br.ReadDouble();
                scaleZ = br.ReadDouble();

                // Offsets
                offsetX = br.ReadDouble();
                offsetY = br.ReadDouble();
                offsetZ = br.ReadDouble();

                // Aller au premier point
                br.BaseStream.Seek(pointOffset, SeekOrigin.Begin);


                Generic.WriteMessage(
                    $"\nLAS {versionMajor}.{versionMinor}" +
                    $"\nFormat point : {pointFormat}" +
                    $"\nTaille point : {pointSize}" +
                    $"\nNombre points : {pointCount}");
            }




            public bool ReadPoint(out LasPoint point)
            {
                point = null;


                if (pointCount == 0)
                {
                    return false;
                }

                long start = br.BaseStream.Position;


                LasPoint p = new LasPoint();
                int rawX = br.ReadInt32();
                int rawY = br.ReadInt32();
                int rawZ = br.ReadInt32();

                p.X = rawX * scaleX + offsetX;
                p.Y = rawY * scaleY + offsetY;
                p.Z = rawZ * scaleZ + offsetZ;

                br.ReadUInt16(); // Intensité
                br.ReadByte();   // Return Number / Flags
                br.ReadByte();   // Classification Flags

                // Classification LAS
                p.Classification = br.ReadByte();

                // User Data
                br.ReadByte();
                // Scan angle
                br.ReadInt16();
                // Point source ID
                br.ReadUInt16();
                // GPS Time
                br.ReadDouble();

                // Lecture complète du point restant
                long afterBasic = br.BaseStream.Position;
                int remaining = pointSize - (int)(afterBasic - start);
                byte[] buffer = br.ReadBytes(remaining);

                // RGB formats 2 et 3
                if (pointFormat == 2 || pointFormat == 3)
                {

                    if (buffer.Length >= 6)
                    {
                        int index = buffer.Length - 6;

                        ushort r = BitConverter.ToUInt16(buffer, index);
                        ushort g = BitConverter.ToUInt16(buffer, index + 2);
                        ushort b = BitConverter.ToUInt16(buffer, index + 4);

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


            public void Dispose()
            {
                br?.Dispose();
            }
        }
    }