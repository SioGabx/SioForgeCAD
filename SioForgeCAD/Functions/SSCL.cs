using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;

namespace SioForgeCAD.Functions
{
    public static class SSCL
    {
        public static void Select()
        {
            Editor ed = Generic.GetEditor();
            TypedValue[] tvs = new TypedValue[] {
                new TypedValue((int)DxfCode.LayerName,Layers.GetCurrentLayerName()),
               // new TypedValue((int)DxfCode.Start,"LINE"),
            };
            SelectionFilter sf = new SelectionFilter(tvs);
            PromptSelectionResult psr = ed.SelectAll(sf);
            ed.SetImpliedSelection(psr.Value);
        }
    }
}

