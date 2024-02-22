
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ViewModel.PointCloudManager;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class SMARTFLATTEN
    {
        public static void Flatten()
        {
            var ed = Generic.GetEditor();
            PromptSelectionOptions PromptBlocSelectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = $"\nSelectionnez des entités",
                RejectObjectsOnLockedLayers = false
            };
            var EntitiesSelection = ed.GetSelection(PromptBlocSelectionOptions);
            if (EntitiesSelection.Status != PromptStatus.OK) { return; }

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId entityObjectId in EntitiesSelection.Value.GetObjectIds())
                {
                    var DbObjEnt = tr.GetObject(entityObjectId, OpenMode.ForWrite, true, true);
                    if (!(DbObjEnt is Entity entity))
                    {
                        DbObjEnt.DowngradeOpen();
                        continue;
                    }
                    if (entity is Polyline polyline)
                    {
                        polyline.Elevation = 0;
                    }
                    if (entity is BlockReference blockreference)
                    {
                        blockreference.Position = blockreference.Position.Flatten();
                    }
                    if (entity is Circle circle)
                    {
                        circle.Center = circle.Center.Flatten();
                    }
                    if (entity is Hatch hatch)
                    {
                        hatch.Elevation = 0;
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.DBText dbtext)
                    {
                        dbtext.Position = dbtext.Position.Flatten();
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.DBPoint dbpoint)
                    {
                        dbpoint.Position = dbpoint.Position.Flatten();
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.Leader leader)
                    {
                        leader.StartPoint = leader.StartPoint.Flatten();
                        leader.EndPoint = leader.EndPoint.Flatten();
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.Line line)
                    {
                        line.StartPoint = line.StartPoint.Flatten();
                        line.EndPoint = line.EndPoint.Flatten();
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.MText mtext)
                    {
                        mtext.Location = mtext.Location.Flatten();
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.Ray ray)
                    {
                        ray.BasePoint = ray.BasePoint.Flatten();
                        ray.EndPoint = ray.EndPoint.Flatten();
                        ray.StartPoint = ray.StartPoint.Flatten();
                    }
                    if (entity is Autodesk.AutoCAD.DatabaseServices.Spline spline)
                    {
                        spline.EndPoint = spline.EndPoint.Flatten();
                        spline.StartPoint = spline.StartPoint.Flatten();
                    }

                    if (entity is Autodesk.AutoCAD.DatabaseServices.Table table)
                    {
                        table.Position = table.Position.Flatten();
                    }
                }
                tr.Commit();
            }


        }

    }
}
