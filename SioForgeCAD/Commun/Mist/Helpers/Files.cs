using System;
using System.IO;
using System.Reflection;

namespace SioForgeCAD.Commun.Mist
{
    public static class Files
    {
        public static string GetATempFolder(string ProcessName)
        {
            string uniqueId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            string tempFolderPath = Path.Combine(Path.GetTempPath(), $"{ProcessName}_{uniqueId}");
            Directory.CreateDirectory(tempFolderPath);
            return tempFolderPath;
        }

        public static string ResolveAbsolutePath(string currentPath, string originalDir)
        {
            if (string.IsNullOrEmpty(currentPath) || Path.IsPathRooted(currentPath))
            {
                return currentPath;
            }

            try
            {
                string cleanRelPath = currentPath.Replace("/", "\\");
                return Path.GetFullPath(Path.Combine(originalDir, cleanRelPath));
            }
            catch
            {
                return currentPath; // Sécurité anti-crash si le chemin est corrompu
            }
        }

        public static string FormatFileSizeFromByte(long ovalue, int odecimalPlaces = 1)
        {
            string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            string SizeSuffix(long value, int decimalPlaces = 1)
            {
                if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }

                int i = 0;
                decimal dValue = value;
                while (Math.Round(dValue, decimalPlaces) >= 1000)
                {
                    dValue /= 1024;
                    i++;
                }

                return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
            }

            return SizeSuffix(ovalue, odecimalPlaces);
        }

        public static bool IsFileLockedOrReadOnly(string path)
        {
            return IsFileLockedOrReadOnly(new FileInfo(path));
        }

        public static bool IsFileLockedOrReadOnly(FileInfo fi)
        {
            if (!fi.Exists)
            {
                return false;
            }
            FileStream fs = null;
            try
            {
                fs = fi.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (System.Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                {
                    return true;
                }
                throw;
            }
            finally
            {
                fs?.Close();
            }
            return false;
        }

        // Determining Build Date
        // https://blog.codinghorror.com/determining-build-date-the-hard-way/
        // Usage : var linkTimeLocal = Assembly.GetExecutingAssembly().GetLinkerTime();

        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        public static bool TryDeleteFile(string FileName)
        {
            if (File.Exists(FileName))
            {
                try
                {
                    File.Delete(FileName);
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool TryDeleteDirectory(string FileName, bool Recursive = true)
        {
            if (Directory.Exists(FileName))
            {
                try
                {
                    Directory.Delete(FileName, Recursive);
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
