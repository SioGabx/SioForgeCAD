
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ViewModel.PointCloudManager;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class SMARTFLATTEN
    {
        public static void FlattenAll()
        {
            var db = Generic.GetDatabase();
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var BlkTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in BlkTable)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr.IsLayout) continue;

                    Debug.WriteLine($"Inspecting block: {btr.Name}");

                    foreach (ObjectId objId in btr)
                    {
                        ids.Add(objId);
                    }
                }

                BlockTableRecord modelSpace = tr.GetObject(BlkTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId objId in modelSpace)
                {
                    ids.Add(objId);
                }
                FlatObjects(ids.ToArray());
                tr.Commit();
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

                    if (entity is Polyline2d polyline2d)
                    {
                        polyline2d.Elevation = 0;
                        using (Polyline pline = polyline2d.ToPolyline())
                        {
                            polyline2d.CopyPropertiesTo(pline);
                            pline.AddToDrawing();
                            polyline2d.Erase();
                        }
                    }
                    if (entity is Polyline3d polyline3d)
                    {
                        using (Polyline pline = polyline3d.ToPolyline())
                        {
                            polyline3d.CopyPropertiesTo(pline);
                            pline.AddToDrawing();
                            polyline3d.Erase();
                        }
                    }
                    else if (entity is Polyline polyline)
                    {
                        polyline.Elevation = 0;
                        polyline.FixNormals();
                        polyline.Flatten();
                    }
                    else if (entity is Ellipse ellipse)
                    {
                        ellipse.Center = ellipse.Center.Flatten();
                    }
                    else if (entity is BlockReference blockreference)
                    {
                        blockreference.Position = blockreference.Position.Flatten();
                    }
                    else if (entity is Circle circle)
                    {
                        circle.Center = circle.Center.Flatten();
                    }
                    else if (entity is Hatch hatch)
                    {
                        hatch.Elevation = 0;
                    }
                    else if (entity is DBText dbtext)
                    {
                        dbtext.Position = dbtext.Position.Flatten();
                    }
                    else if (entity is DBPoint dbpoint)
                    {
                        dbpoint.Position = dbpoint.Position.Flatten();
                    }
                    else if (entity is Leader)
                    {
                        //not implemented
                    }
                    else if (entity is Line line)
                    {
                        line.StartPoint = line.StartPoint.Flatten();
                        line.EndPoint = line.EndPoint.Flatten();
                    }
                    else if (entity is MText mtext)
                    {
                        mtext.Location = mtext.Location.Flatten();
                    }
                    else if (entity is Ray ray)
                    {
                        ray.BasePoint = ray.BasePoint.Flatten();
                        ray.SecondPoint = ray.SecondPoint.Flatten();
                    }
                    else if (entity is Xline xline)
                    {
                        xline.BasePoint = xline.BasePoint.Flatten();
                        xline.SecondPoint = xline.SecondPoint.Flatten();
                    }
                    else if (entity is Helix helix)
                    {
                        helix.StartPoint = helix.StartPoint.Flatten();
                        helix.SetAxisPoint(helix.GetAxisPoint().Flatten(), true);
                    }
                    else if (entity is Arc arc)
                    {
                        arc.Center = arc.Center.Flatten();
                    }
                    else if (entity is Spline spline)
                    {
                        for (int i = 0; i < spline.NumControlPoints; i++)
                        {
                            var point = spline.GetControlPointAt(i);
                            spline.SetControlPointAt(i, point.Flatten());
                        }
                    }
                    else if (entity is Table table)
                    {
                        table.Position = table.Position.Flatten();
                    }
                    else if (entity is Wipeout wipeout)
                    {
                        var orient = wipeout.Orientation;
                        wipeout.Orientation = new CoordinateSystem3d(orient.Origin.Flatten(), orient.Xaxis, orient.Yaxis);
                    }
                    else if (entity is RasterImage rasterimage)
                    {
                        var orient = rasterimage.Orientation;
                        rasterimage.Orientation = new CoordinateSystem3d(orient.Origin.Flatten(), orient.Xaxis, orient.Yaxis);
                    }
                    else if (entity is RotatedDimension rotatedDimension)
                    {
                        rotatedDimension.TextPosition = rotatedDimension.TextPosition.Flatten();
                        rotatedDimension.DimLinePoint = rotatedDimension.DimLinePoint.Flatten();
                        rotatedDimension.XLine1Point = rotatedDimension.XLine1Point.Flatten();
                        rotatedDimension.XLine2Point = rotatedDimension.XLine1Point.Flatten();
                    }
                    else if (entity is AlignedDimension alignedDimension)
                    {
                        alignedDimension.TextPosition = alignedDimension.TextPosition.Flatten();
                        alignedDimension.DimLinePoint = alignedDimension.DimLinePoint.Flatten();
                        alignedDimension.XLine1Point = alignedDimension.XLine1Point.Flatten();
                        alignedDimension.XLine2Point = alignedDimension.XLine1Point.Flatten();
                    }
                    /*

                                        else if (id_platf.ObjectClass.Name == "AcDbRotatedDimension")
                    {//Размер повернутый
                        RotatedDimension dim = (RotatedDimension)ent;

                        //Проверяем, имеют ли задающие точки размера ненулевую координату Z
                        if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.DimLinePoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                            dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                            dim.DimLinePoint = new Point3d(dim.DimLinePoint.X, dim.DimLinePoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: повернутый размер");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbPoint3AngularDimension")
                    {//Угловой размер по 3 точкам
                        Point3AngularDimension dim = (Point3AngularDimension)ent;
                        if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.CenterPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {

                            dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                            dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                            dim.CenterPoint = new Point3d(dim.CenterPoint.X, dim.CenterPoint.Y, 0);

                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Угловой размер по трем точкам");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbLineAngularDimension2")
                    {//Еще угловой размер по точкам
                        LineAngularDimension2 dim = (LineAngularDimension2)ent;

                        if (dim.XLine1Start.Z != 0 || dim.XLine1End.Z != 0 || dim.XLine1Start.Z != 0 || dim.XLine2End.Z != 0 || dim.ArcPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {

                            dim.XLine1Start = new Point3d(dim.XLine1Start.X, dim.XLine1Start.Y, 0);
                            dim.XLine1End = new Point3d(dim.XLine1End.X, dim.XLine1End.Y, 0);
                            dim.XLine2Start = new Point3d(dim.XLine2Start.X, dim.XLine2Start.Y, 0);
                            dim.XLine2End = new Point3d(dim.XLine2End.X, dim.XLine2End.Y, 0);
                            dim.ArcPoint = new Point3d(dim.ArcPoint.X, dim.ArcPoint.Y, 0);

                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Угловой размер по 5 точкам");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbDiametricDimension")
                    {  //Размер диаметра окружности
                        DiametricDimension dim = (DiametricDimension)ent;

                        if (dim.FarChordPoint.Z != 0 || dim.ChordPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.FarChordPoint = new Point3d(dim.FarChordPoint.X, dim.FarChordPoint.Y, 0);
                            dim.ChordPoint = new Point3d(dim.ChordPoint.X, dim.ChordPoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Диаметральный размер");

                            result = true;
                            dimcount++;
                        };
                    }
                    else if (id_platf.ObjectClass.Name == "AcDbArcDimension")
                    {  //Дуговой размер
                        ArcDimension dim = (ArcDimension)ent;

                        if (dim.XLine1Point.Z != 0 || dim.XLine2Point.Z != 0 || dim.ArcPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.XLine1Point = new Point3d(dim.XLine1Point.X, dim.XLine1Point.Y, 0);
                            dim.XLine2Point = new Point3d(dim.XLine2Point.X, dim.XLine2Point.Y, 0);
                            dim.ArcPoint = new Point3d(dim.ArcPoint.X, dim.ArcPoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Дуговой размер");

                            result = true;
                            dimcount++;
                        };

                    }
                    else if (id_platf.ObjectClass.Name == "AcDbRadialDimension")
                    {  //Радиальный размер
                        RadialDimension dim = (RadialDimension)ent;

                        if (dim.Center.Z != 0 || dim.ChordPoint.Z != 0 || dim.TextPosition.Z != 0)
                        {
                            dim.Center = new Point3d(dim.Center.X, dim.Center.Y, 0);
                            dim.ChordPoint = new Point3d(dim.ChordPoint.X, dim.ChordPoint.Y, 0);
                            dim.TextPosition = new Point3d(dim.TextPosition.X, dim.TextPosition.Y, 0);

                            //ed.WriteMessage("DEBUG: Преобразован объект: Радиальный размер");

                            result = true;
                            dimcount++;
                        };

                    }

                    */
                    else if (entity is MLeader mLeader)
                    {
                        //IN PROGRESS DOES NOT WORK ???
                        mLeader.TextLocation = mLeader.TextLocation.Flatten();
                        for (int LeaderLineCount = 0; LeaderLineCount < mLeader.LeaderLineCount; LeaderLineCount++)
                        {
                            mLeader.SetFirstVertex(LeaderLineCount, mLeader.GetFirstVertex(LeaderLineCount).Flatten());
                            mLeader.SetLastVertex(LeaderLineCount, mLeader.GetLastVertex(LeaderLineCount).Flatten());
                            for (int LeaderVerticesCount = 0; LeaderVerticesCount < mLeader.VerticesCount(LeaderLineCount); LeaderVerticesCount++)
                            {
                                Point3d Point = mLeader.GetVertex(LeaderLineCount, LeaderVerticesCount);
                                mLeader.SetVertex(LeaderLineCount, LeaderVerticesCount, Point.Flatten());

                                Debug.WriteLine($"Flatten point {mLeader.GetVertex(LeaderLineCount, LeaderVerticesCount)}");
                            }
                        }
                    }
                    else if (entity is Leader ld)
                    {  //Выноска Autocad

                        if (ld.EndPoint.Z != 0 || ld.StartPoint.Z != 0)
                        {
                            ld.EndPoint = new Point3d(ld.EndPoint.X, ld.EndPoint.Y, 0);
                            ld.StartPoint = new Point3d(ld.StartPoint.X, ld.StartPoint.Y, 0);
                        };

                    }
                    else if (entity is Solid)
                    {
                        //Not supported yet
                    }
                    else if (entity is Solid3d)
                    {
                        //Not supported yet
                    }
                    else
                    {
                        Debug.WriteLine($"Entité non traitée : \"{entity.GetType()}\"");
                    }
                }
                tr.Commit();
            }
        }
    }
}
