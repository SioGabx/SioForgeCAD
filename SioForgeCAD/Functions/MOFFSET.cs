using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class MOFFSET
    {
        public static async void Offset()
        {
            try
            {
                await Generic.CommandAsync("_offset", .5, Editor.PauseToken, "_m");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
