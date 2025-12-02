using System;
using System.IO;

namespace SioForgeCAD.Commun.Mist
{
    public static class Files
    {

        public static string FormatFileSizeFromByte(Int64 ovalue, int odecimalPlaces = 1)
        {
            string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            string SizeSuffix(Int64 value, int decimalPlaces = 1)
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
    }
}
