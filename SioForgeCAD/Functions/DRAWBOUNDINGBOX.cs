using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class DRAWBOUNDINGBOX
    {
        public static void Draw()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            var result = ed.GetSelectionRedraw();
            if (result.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId SelectedEntityObjId in result.Value.GetObjectIds())
                {
                    var ent = SelectedEntityObjId.GetEntity();
                    ent.GetExtents().GetGeometry().AddToDrawingCurrentTransaction();
                    var geo = ent.GetVisualExtents(out _).GetGeometry();
                    geo.ColorIndex = 5;
                        geo.AddToDrawingCurrentTransaction();
                }
                tr.Commit();
            }
        }
    }
}
