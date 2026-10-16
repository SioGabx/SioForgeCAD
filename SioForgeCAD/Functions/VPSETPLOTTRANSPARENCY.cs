using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Functions
{
    internal class VPSETPLOTTRANSPARENCY
    {
        internal static void Set()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                var PlotTransparency = ed.GetBoolOption("Activer la transparence au tracé ?");
                foreach (Layout layout in ed.GetAllLayout())
                {
                    if (!layout.TabSelected) { continue; }
                    Layout layoutObj = tr.GetObject(layout.ObjectId, OpenMode.ForWrite) as Layout;

                    if (layoutObj != null)
                    {
                        layoutObj.PlotTransparency = PlotTransparency;
                    }
                }

                tr.Commit();
            }
        }
    }
}