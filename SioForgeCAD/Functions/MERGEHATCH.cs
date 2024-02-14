using Autodesk.AutoCAD.ApplicationServices;
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
using System.Windows.Controls;

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

        


            ed.GetPoint("Indiquez un point");
            var CurrentViewSave = ed.GetCurrentView();

            Document doc = Generic.GetDocument();
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId selectedObjectId in sel.GetObjectIds())
                {
                    if (selectedObjectId.GetDBObject() is Entity entity)
                    {
                        entities.Add(entity);
                    }
                }
                Extents3d box = entities.GetExtents();
                box.Expand(1.2);
                box.GetGeometry().AddToDrawing();


                var reg = Region.CreateFromCurves(entities.ToDBObjectCollection());
                Region RegionZero = reg[0] as Region;
                for (int i = 1; i < reg.Count; i++)
                {
                    RegionZero.BooleanOperation(BooleanOperationType.BoolUnite, reg[i] as Region);
                    //https://www.theswamp.org/index.php?action=post;quote=607106;topic=31865.30;last_msg=617675
                    //https://www.theswamp.org/index.php?topic=31865.30

                }
                tr.Commit();
            }
        }




    }
}
