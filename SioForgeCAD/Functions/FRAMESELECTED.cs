using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class FRAMESELECTED
    {
        public static void FrameEntityToView()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!ed.GetImpliedSelection(out PromptSelectionResult selResult))
                {
                    selResult = ed.GetSelection();
                }
                if (selResult.Status == PromptStatus.OK)
                {
                    Extents3d Extend = new Extents3d();
                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        if (selObj.ObjectId.GetDBObject() is Entity ent)
                        {
                            Extend.AddExtents(ent.GetExtents());
                            if (ent.Bounds is Extents3d EntBound)
                            {
                                Extend.AddExtents(EntBound);
                            }
                        }

                    }
                    ed.SetImpliedSelection(selResult.Value);

                    Extend.Expand(1.25);
                    Extend.ZoomExtents();

                }
                tr.Commit();
            }
        }
    }
}
