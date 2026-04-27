namespace SioForgeCAD.Commun.Extensions
{
    public static class LayerTableRecordExtensions
    {
        public static bool IsXref(this Autodesk.AutoCAD.DatabaseServices.LayerTableRecord ltr)
        {
            return ltr.IsDependent;
        }
    }
}
