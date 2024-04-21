using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class PURGEALL
    {
        public static void Purge()
        {
            var Database = Generic.GetDatabase();
            Database.PurgeRasterImages();
            Database.Purge();
            Database.PurgeRegisteredApplication();
        }
    }
}
