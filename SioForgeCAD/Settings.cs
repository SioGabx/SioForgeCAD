namespace SioForgeCAD
{
    public static class Settings
    {
        public static int MultithreadingMaxNumberOfThread = 1; // -1 to disable limits

        public static string BlocNameAltimetrie = "_APUd_COTATIONS_Altimetries";
        public static string BlocNameAltimetrieCoupes = "_APUd_CP_Altimetries";
        public static string BlocNamePente = "_APUd_COTATIONS_Pentes";

        public static int TransientPrimaryColorIndex = 252;
        public static int TransientSecondaryColorIndex  = 255;

        public static string VegblocLayerHeightName  = "-APUd_VEG_HAUTEURS";
        public static string VegblocLayerPrefix= "_APUd_VEG_";
        public static bool VegblocCopyGripDeselectAfterCopy = true;
        public static bool VegblocGeneratePeripheryCircles = false;
    }
}
