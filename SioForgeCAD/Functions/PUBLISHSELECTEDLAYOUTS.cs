using Autodesk.AutoCAD.ApplicationServices;
using System;

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