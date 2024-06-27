using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class PURGEALL
    {
        public static void Purge()
        {
            var db = Generic.GetDatabase();
            db.PurgeRasterImages();
            db.Purge();
            db.PurgeRegisteredApplication();

            VIEWPORTLOCK.DoLockUnlock(true);
        }
    }
}
