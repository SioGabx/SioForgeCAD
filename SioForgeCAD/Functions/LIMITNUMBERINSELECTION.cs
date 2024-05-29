using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class LIMITNUMBERINSELECTION
    {
        public static void LimitToOne()
        {
            Editor ed = Generic.GetEditor();
            var Select = ed.GetSelectionRedraw();
            if (Select.Status == PromptStatus.OK)
            {
                var selectObjIds = Select.Value.GetObjectIds();
                if (selectObjIds.Length > 0)
                {
                    ed.SetImpliedSelection(new ObjectId[1] { selectObjIds[0] });
                }
            }
        }
    }
}
