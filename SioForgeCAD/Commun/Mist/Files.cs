using System;
using System.IO;

namespace SioForgeCAD.Commun.Mist
{
    public static class Files
    {

        public static bool IsFileLockedOrReadOnly(FileInfo fi)
        {
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
                if (fs != null) { fs.Close(); }
            }
            return false;
        }
    }
}
