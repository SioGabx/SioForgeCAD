using System;
using System.IO;

namespace SioForgeCAD.Commun.Mist
{
    public static class Files
    {
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
