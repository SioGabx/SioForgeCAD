using Autodesk.AutoCAD.ApplicationServices;

namespace SioForgeCAD.Functions
{
    public static class PUBLISHSELECTEDLAYOUTS
    {
        public static void ShowPublishDialog()
        {
            Application.Publisher.PublishSelectedLayouts(false);
        }
    }
}