namespace SioForgeCAD
{
    public static class Settings
    {
        public static int MultithreadingMaxNumberOfThread = 1; // -1 to disable limits

        public static string CADLayerPrefix = "_APUd_";
        public static string VegblocLayerPrefix = CADLayerPrefix + "VEG_";
        public static string InfoLayerPrefix = CADLayerPrefix + "INFO_";
        public static string BlocNameAltimetrie = CADLayerPrefix + "COTATIONS_Altimetries";
        public static string BlocNameAltimetrieCoupes = CADLayerPrefix + "CP_Altimetries";
        public static string BlocNamePente = CADLayerPrefix + "COTATIONS_Pentes";

        public static int TransientPrimaryColorIndex = 252;
        public static int TransientSecondaryColorIndex = 255;
        public static string VegblocLayerHeightName = InfoLayerPrefix + "Hauteurs_végétaux";
        public static bool VegblocCopyGripDeselectAfterCopy = true;
        public static bool VegblocGeneratePeripheryCircles = false;
    }
}
