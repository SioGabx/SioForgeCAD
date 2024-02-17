namespace SioForgeCAD
{
    public static class Settings
    {
        public static string BlocNameAltimetrie = "_APUd_COTATIONS_Altimetries";
        public static string BlocNameAltimetrieCoupes = "_APUd_CP_Altimetries";
        public static string BlocNamePente = "_APUd_COTATIONS_Pentes";

        public static int TransientPrimaryColorIndex { get; } = 252;
        public static int TransientSecondaryColorIndex { get; } = 255;

        public static string VegblocLayerHeightName { get; } = "-APUd_VEG_HAUTEURS";
        public static string VegblocLayerPrefix { get; } = "_APUd_VEG_";
        public static bool VegblocCopyGripDeselectAfterCopy { get; } = true;
        public static bool VegblocGeneratePeripheryCircles { get; } = false;
    }
}
