using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System.Collections.Generic;
using Viewport = Autodesk.AutoCAD.DatabaseServices.Viewport;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ViewportsExtensions
    {
        public static IntegerCollection GetViewPortsNumbers(this TransientManager _)
        {
            //https://www.keanw.com/2011/03/drawing-transient-graphics-appropriately-in-autocad-within-multiple-paperspace-viewports-using-net.html
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            // Are we in model space outside floating viewports?
            // Then we'll initalize an empty IntegerCollection

            if (db.TileMode)
            {
                return new IntegerCollection();
            }

            List<int> vps = new List<int>();

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                Viewport vp = tr.GetObject(ed.ActiveViewportId, OpenMode.ForRead) as Viewport;

                // Are we in paper space and not inside a floating
                // viewport? Then only the paper space viewport itself
                // is of interest
                if (vp?.Number == 1)
                {
                    vps.Add(1);
                }
                else
                {
                    // Now we're inside a floating viewport and
                    // will display transients in active viewports
                    foreach (ObjectId vpId in db.GetViewports(false))
                    {
                        vp = (Viewport)tr.GetObject(vpId, OpenMode.ForRead);
                        vps.Add(vp.Number);
                    }
                }
                tr.Commit();
            }
            int[] ints = new int[vps.Count];
            vps.CopyTo(ints, 0);
            return ints.ToIntegerCollection();
        }
    }
}
