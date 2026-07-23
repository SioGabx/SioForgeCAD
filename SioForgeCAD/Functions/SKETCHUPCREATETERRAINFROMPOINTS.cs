using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static SioForgeCAD.Commun.DelaunayTriangulate;

namespace SioForgeCAD.Functions
{
    public static class SKETCHUPCREATETERRAINFROMPOINTS
    {
        public static void GeneratePointsFromAlt()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();


            var selRes = ed.GetBlocks(out var ObjIds, "Selectionnez des côtes", false, true);

            if (!selRes)
            {
                return;
            }

            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                //BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                List<Entity> createdEntities = new List<Entity>();
                List<Point3d> PointsSet = new List<Point3d>();
                foreach (var selObj in ObjIds)
                {
                    if (!(tr.GetObject(selObj, OpenMode.ForRead) is BlockReference blockRef))
                    {
                        continue;
                    }

                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        if (!(tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef))
                        {
                            continue;
                        }

                        string text = attRef.TextString;

                        // Vérifie format nombre.xx (ex: 123.00)
                        if (Regex.IsMatch(text, @"^\d+\.\d{2,}$"))
                        {
                            double z = (double)Convert.ToDouble(text);

                            // Création du point à l'altitude Z
                            Point3d point = new Point3d(blockRef.Position.X, blockRef.Position.Y, z);
                            //createdEntities.Add(point.AddToDrawing());

                            PointsSet.Add(point);
                            break;
                        }
                    }
                }

                if (PointsSet.Count < 3)
                {
                    Generic.WriteMessage("Vous devez selectionner au moins 3 points");
                    PointsSet.DeepDispose();
                    tr.Abort();
                    ed.SetImpliedSelection(ObjIds);
                    return;
                }


                foreach (var Triangle in DelaunayTriangulate.Triangulate(PointsSet))
                {
                    Point3dCollection pts = new Point3dCollection(new[] { Triangle.Vertex1, Triangle.Vertex2, Triangle.Vertex3 });
                    using (var tempPoly = new Polyline3d(Poly3dType.SimplePoly, pts, true))
                    using (var Surf = new DBObjectCollection { tempPoly })
                    {
                        try
                        {
                            using (var Poly3DRegions = Region.CreateFromCurves(Surf))
                            {
                                foreach (Region reg in Poly3DRegions)
                                {
                                    createdEntities.Add(reg);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }

                }

                // circle has 32 edges
                if (createdEntities.Count > 0)
                {
                    var BlkDefId = BlockReferences.Create(typeof(SKETCHUPCREATETERRAINFROMPOINTS).Name + "_" + DateTime.Now.Ticks, $"Terrain généré à partir de {Generic.GetExtensionDLLName()} pour SketchUp.", createdEntities.ToDBObjectCollection(), Points.Empty, true, BlockScaling.Uniform);

                    if (!BlkDefId.IsValid) { tr.Commit(); return; }
                    var BlkRef = new BlockReference(Points.Empty.SCG, BlkDefId);
                    BlkRef.AddToDrawingCurrentTransaction();
                    ed.SetImpliedSelection(new ObjectId[1] { BlkRef.ObjectId });
                }


                Generic.WriteMessage($"{createdEntities.Count} objets créés et sélectionnés.");
                tr.Commit();
            }

        }
    }

}
