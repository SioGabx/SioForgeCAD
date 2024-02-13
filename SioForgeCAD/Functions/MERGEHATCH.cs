using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class MERGEHATCH
    {
        public static void Merge()
        {
            Editor ed = Generic.GetEditor();

            // ed.TraceBoundary(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0), false);
            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
                return;

            SelectionSet sel = selRes.Value;
            List<Entity> entities = new List<Entity>();

            foreach (ObjectId selectedObjectId in sel.GetObjectIds())
            {
                if (selectedObjectId.GetDBObject() is Entity entity)
                {
                    entities.Add(entity);
                }
            }


            ed.GetPoint("Indiquez un point");
            var CurrentViewSave = ed.GetCurrentView();

            Extents3d box = entities.GetExtents();
            box.Expand(1.2);
            box.GetGeometry().AddToDrawing();


        }




    }
}
