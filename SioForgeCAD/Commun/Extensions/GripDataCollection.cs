using Autodesk.AutoCAD.DatabaseServices;

namespace SioForgeCAD.Commun.Extensions
{
    public static class GripDataCollectionExtensions
    {
        public static GripData[] ToArray(this GripDataCollection grips)
        {
            GripData[] newArray = new GripData[grips.Count];
            int index = 0;
            foreach (GripData item in grips)
            {
                newArray.SetValue(item, index++);
            }
            return newArray;
        }
    }
}
