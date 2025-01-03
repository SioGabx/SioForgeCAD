﻿
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ViewModel.PointCloudManager;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class SMARTFLATTEN
    {
        public static void FlattenAll()
        {
            var doc = Generic.GetDocument();
            var db = Generic.GetDatabase();
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                var BlkTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in BlkTable)
                {
                    BlockTableRecord btr = trans.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr.IsLayout) continue;

                    Debug.WriteLine($"\nInspecting block: {btr.Name}");

                    foreach (ObjectId objId in btr)
                    {
                        ids.Add(objId);
                    }
                }

                BlockTableRecord modelSpace = trans.GetObject(BlkTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId objId in modelSpace)
                {
                    ids.Add(objId);
                }
                FlatObjects(ids.ToArray());
                trans.Commit();

            }
        }

        public static void Flatten()
        {
            var ed = Generic.GetEditor();
            var EntitiesSelection = ed.GetSelectionRedraw();
            if (EntitiesSelection.Status != PromptStatus.OK) { return; }

            FlatObjects(EntitiesSelection.Value.GetObjectIds());
        }

        private static void FlatObjects(ObjectId[] objectIds)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId entityObjectId in objectIds)
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
                    if (entity is Ellipse ellipse)
                    {
                        ellipse.Center = ellipse.Center.Flatten();
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
                    if (entity is DBText dbtext)
                    {
                        dbtext.Position = dbtext.Position.Flatten();
                    }
                    if (entity is DBPoint dbpoint)
                    {
                        dbpoint.Position = dbpoint.Position.Flatten();
                    }
                    if (entity is Leader leader)
                    {
                        leader.StartPoint = leader.StartPoint.Flatten();
                        leader.EndPoint = leader.EndPoint.Flatten();
                    }
                    if (entity is Line line)
                    {
                        line.StartPoint = line.StartPoint.Flatten();
                        line.EndPoint = line.EndPoint.Flatten();
                    }
                    if (entity is MText mtext)
                    {
                        mtext.Location = mtext.Location.Flatten();
                    }
                    if (entity is Ray ray)
                    {
                        ray.BasePoint = ray.BasePoint.Flatten();
                        ray.SecondPoint = ray.SecondPoint.Flatten();
                    }
                    if (entity is Xline xline)
                    {
                        xline.BasePoint = xline.BasePoint.Flatten();
                        xline.SecondPoint = xline.SecondPoint.Flatten();
                    }
                    if (entity is Helix helix)
                    {
                        helix.StartPoint = helix.StartPoint.Flatten();
                        helix.SetAxisPoint(helix.GetAxisPoint().Flatten(), true);
                    }
                    if (entity is Spline spline)
                    {
                        for (int i = 0; i < spline.NumControlPoints; i++)
                        {
                            var point = spline.GetControlPointAt(i);
                            spline.SetControlPointAt(i, point.Flatten());
                        }
                    }

                    if (entity is Table table)
                    {
                        table.Position = table.Position.Flatten();
                    }
                }
                tr.Commit();
            }
        }

    }
}
