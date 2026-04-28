using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SioForgeCAD.Commun.Mist.Helpers.TextParsers.PC3
{
    //https://github.com/znlgis/lightcad1/blob/master/src/LightCAD.UI.WinForm/Printer/PiaSerializer.cs
    public static class Files
    {

        public static List<string> GetPaperFormatsFromPc3(string pc3FileName)
        {
            List<string> formats = new List<string>();

            try
            {
                using (PlotSettings ps = new PlotSettings(true))
                {
                    PlotSettingsValidator psv = PlotSettingsValidator.Current;
                    psv.RefreshLists(ps);

                    // Applique le nom du PC3 au PlotSettings
                    psv.SetPlotConfigurationName(ps, pc3FileName, null);

                    //var mediaNames = psv.GetCanonicalMediaNameList(ps);
                    PlotConfigManager.SetCurrentConfig(pc3FileName);
                    PlotConfig plotConfig = PlotConfigManager.CurrentConfig;
                    var CanonicalMediaNames = plotConfig.CanonicalMediaNames;
                    foreach (string CanonicalName in CanonicalMediaNames)
                    {
                        // Convertit le nom système en nom lisible (Local)
                        string localName = psv.GetLocaleMediaName(ps, CanonicalName);
                        formats.Add(localName);
                    }
                }
                formats.Sort();
            }
            catch
            {
            }

            return formats;
        }

        public static void Decode(string filePath)
        {
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                // On se place à l'offset 60 comme avant
                fs.Seek(60L, SeekOrigin.Begin);

                // LIRE LES 2 OCTETS ZLIB (78 DA) pour les ignorer
                fs.ReadByte();
                fs.ReadByte();

                // On utilise DeflateStream au lieu de GZipStream
                using (DeflateStream ds = new DeflateStream(fs, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(ds, Encoding.UTF8))
                    {
                        string s = sr.ReadToEnd();
                        File.WriteAllText(filePath + ".txt", s, Encoding.Default);
                    }
                }
            }
        }


        /// <summary>
        /// Encode un fichier texte modifié vers un fichier PC3/PMP valide.
        /// </summary>
        public static void Encode(string textFilePath, string outputFilePath)
        {
            string newText = File.ReadAllText(textFilePath, Encoding.Default);
            byte[] uncompressedBytes = Encoding.Default.GetBytes(newText);

            byte[] header48 = Encoding.Default.GetBytes("PIAFILEVERSION_2.0,PC3VER1,compress\r\npmzlibcodec");
            //Calculer l'Adler32 (utilisé à deux endroits !)
            uint adler = CalculateAdler32(uncompressedBytes);

            //Compresser les données en mémoire avec le DeflateStream natif
            byte[] rawDeflateBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true))
                {
                    ds.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                }
                rawDeflateBytes = ms.ToArray();
            }

            //Construire le flux Zlib complet
            //Taille = 2 octets (En-tête Zlib) + X octets (Données) + 4 octets (Trailer Adler32)
            byte[] zlibStreamBytes = new byte[2 + rawDeflateBytes.Length + 4];

            zlibStreamBytes[0] = 0x78; // En-tête Zlib
            zlibStreamBytes[1] = 0xDA; // Niveau de compression par défaut

            // Copie des données compressées au milieu
            Buffer.BlockCopy(rawDeflateBytes, 0, zlibStreamBytes, 2, rawDeflateBytes.Length);

            // Ajout du Trailer Zlib à la fin (Adler32 en Big-Endian obligatoire pour Zlib)
            zlibStreamBytes[zlibStreamBytes.Length - 4] = (byte)(adler >> 24);
            zlibStreamBytes[zlibStreamBytes.Length - 3] = (byte)(adler >> 16);
            zlibStreamBytes[zlibStreamBytes.Length - 2] = (byte)(adler >> 8);
            zlibStreamBytes[zlibStreamBytes.Length - 1] = (byte)adler;

            using (FileStream fsOut = File.Create(outputFilePath))
            {
                //Écrire l'en-tête texte (48 octets)
                fsOut.Write(header48, 0, 48);

                //Générer et écrire le bloc de validation AutoCAD (12 octets)
                byte[] checkSumBlock = new byte[12];
                //AutoCAD lit ces valeurs en Little-Endian (format Windows standard)
                BitConverter.GetBytes((int)adler).CopyTo(checkSumBlock, 0);
                BitConverter.GetBytes(uncompressedBytes.Length).CopyTo(checkSumBlock, 4);
                BitConverter.GetBytes(zlibStreamBytes.Length).CopyTo(checkSumBlock, 8);

                fsOut.Write(checkSumBlock, 0, 12);

                //Écrire le flux Zlib complet
                fsOut.Write(zlibStreamBytes, 0, zlibStreamBytes.Length);

                //Caractère de fin de fichier (0x00)
                fsOut.WriteByte(0);
            }
        }

        /// <summary>
        /// Calcule la somme de contrôle Adler-32 requise par le format Zlib.
        /// From https://github.com/icsharpcode/SharpZipLib/blob/master/src/ICSharpCode.SharpZipLib/Checksum/Adler32.cs
        /// </summary>
        public static uint CalculateAdler32(byte[] data)
        {
            const uint BASE = 65521; //largest prime smaller than 65536
            uint s1 = 1;
            uint s2 = 0;

            int length = data.Length;
            int offset = 0;

            while (length > 0)
            {
                // Traitement par lots de 3800 octets pour optimiser les performances (retarde le modulo)
                int n = length < 3800 ? length : 3800;
                length -= n;

                while (--n >= 0)
                {
                    s1 += data[offset++];
                    s2 += s1;
                }

                s1 %= BASE;
                s2 %= BASE;
            }

            return (s2 << 16) | s1;
        }


    }
}
