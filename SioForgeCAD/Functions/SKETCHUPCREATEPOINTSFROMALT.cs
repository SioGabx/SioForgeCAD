using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class SKETCHUPCREATEPOINTSFROMALT
    {
        public static void GeneratePointsFromAlt()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();


            var selRes = ed.GetBlocks(out var ObjIds, "Selectionnez des côtes", false, true);

            if (!selRes) return;



            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                List<ObjectId> createdEntities = new List<ObjectId>();
                foreach (var selObj in ObjIds)
                {
                    if (!(tr.GetObject(selObj, OpenMode.ForRead) is BlockReference blockRef)) continue;

                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (attRef == null) continue;

                        string text = attRef.TextString;

                        // Vérifie format nombre.xx (ex: 123.00)
                        if (Regex.IsMatch(text, @"^\d+\.\d{2,}$"))
                        {
                            double z = (double)Convert.ToDouble(text);

                            // Création du point à l'altitude Z
                            DBPoint point = new DBPoint(new Point3d(blockRef.Position.X, blockRef.Position.Y, z));
                            btr.AppendEntity(point);
                            tr.AddNewlyCreatedDBObject(point, true);
                            createdEntities.Add(point.ObjectId);

                            // Création ligne de 0 à Z
                            Line line = new Line(
                                new Point3d(blockRef.Position.X, blockRef.Position.Y, 0),
                                new Point3d(blockRef.Position.X, blockRef.Position.Y, z)
                            );

                            btr.AppendEntity(line);
                            tr.AddNewlyCreatedDBObject(line, true);
                            createdEntities.Add(line.ObjectId);
                        }
                    }
                }

                if (createdEntities.Count > 0)
                {
                    var BlkDefId = BlockReferences.CreateFromExistingEnts("", $"Terrain généré à partir de {Generic.GetExtensionDLLName()}.", createdEntities.ToObjectIdCollection(), Points.Empty, true, BlockScaling.Uniform, true);
                    
                    if (!BlkDefId.IsValid) { tr.Commit(); return; }
                    var BlkRef = new BlockReference(Points.Empty.SCG, BlkDefId);
                    BlkRef.AddToDrawing();

                    ed.SetImpliedSelection(new ObjectId[1] { BlkRef.ObjectId });
                }


                ed.WriteMessage($"\n{createdEntities.Count} objets créés et sélectionnés.");
                tr.Commit();
            }

        }
    }

}
