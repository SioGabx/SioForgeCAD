using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
    public class Commands
    {
        [CommandMethod("CCI")]
        public void CCI()
        {
            new SioForgeCAD.Functions.CCI().Compute();
        }

        [CommandMethod("CCP")]
        public void CCP()
        {
            new SioForgeCAD.Functions.CCP().Compute();
        }
        [CommandMethod("trianglecc", CommandFlags.UsePickSet)]
        public void Trianglecc()
        {
            SioForgeCAD.Commun.Triangulate.TriangulateCommand();
        }


        [CommandMethod("RENBLK", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public void RENBLK()
        {
            Functions.RENBLK.RenameBloc();
        }


        [CommandMethod("BLKMAKEUNIQUE", CommandFlags.Redraw)]
        public void MAKEUNIQUBLK()
        {
            new Functions.BLKMAKEUNIQUEEACH(true).MakeUniqueBlockReferences();
        }

        [CommandMethod("BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        public void BLKMAKEUNIQUEEACH()
        {
            new Functions.BLKMAKEUNIQUEEACH(false).MakeUniqueBlockReferences();
        }

        [CommandMethod("CCMXREF", CommandFlags.Redraw)]
        public void CCMXREF()
        {
            Functions.CCMXREF.MoveCotationFromXrefToCurrentDrawing();
        }


        [CommandMethod("DRAWPERPENDICULARLINEFROMPOINT", CommandFlags.UsePickSet)]
        public void DRAWPERPENDICULARLINEFROMPOINT()
        {
            Functions.DRAWPERPENDICULARLINEFROMPOINT.DrawPerpendicularLineFromPoint();
        }

        [CommandMethod("C2P")]
        public static void ConvertCirclesToPolylines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                PromptSelectionResult selResult = ed.GetSelection();
                if (selResult.Status == PromptStatus.OK)
                {
                    SelectionSet selSet = selResult.Value;

                    foreach (SelectedObject selObj in selSet)
                    {
                        if (selObj.ObjectId.ObjectClass.DxfName == "CIRCLE")
                        {
                            Circle circle = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Circle;

                            // Convert circle to polyline logic
                            using (Polyline pline = new Polyline())
                            {
                                double bulge = 1.0; // Bulge for arc segment
                                double halfWidth = 0.0; // Set width as needed

                                // Add the first vertex
                                pline.AddVertexAt(0, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);

                                // Add the second vertex
                                pline.AddVertexAt(1, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
                                pline.Closed = true;
                                // Set other polyline properties...
                                pline.Layer = circle.Layer;

                                // Add the polyline to the block table record
                                btr.AppendEntity(pline);
                                tr.AddNewlyCreatedDBObject(pline, true);

                                // Remove or comment out the next line to retain the selected Circle
                                circle.Erase();
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }









        //[CommandMethod("OffsetLine")]
        //public void OffsetLine()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;

        //    // Sélection de la ligne à décaler
        //    PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez la ligne à décaler : ");
        //    peo.SetRejectMessage("\nVeuillez sélectionner une ligne.");
        //    peo.AddAllowedClass(typeof(Line), true);

        //    PromptEntityResult per = ed.GetEntity(peo);

        //    if (per.Status != PromptStatus.OK)
        //        return;

        //    // Obtenir la ligne sélectionnée
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        Line selectedLine = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
        //        if (selectedLine == null)
        //            return;

        //        // Obtenir le vecteur de la ligne
        //        Vector3d lineVector = selectedLine.GetVector3d();

        //        // Calculer le vecteur perpendiculaire
        //        Vector3d offsetVector = new Vector3d(-lineVector.Y, lineVector.X, 0);
        //        offsetVector = offsetVector / offsetVector.Length * 1.0;

        //        // Créer une nouvelle ligne décalée
        //        Point3d startPoint = selectedLine.StartPoint + offsetVector;
        //        Point3d endPoint = selectedLine.EndPoint + offsetVector;

        //        Line offsetLine = new Line(startPoint, endPoint);

        //        // Ajouter la nouvelle ligne à la base de données
        //        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        //        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        //        btr.AppendEntity(offsetLine);
        //        tr.AddNewlyCreatedDBObject(offsetLine, true);

        //        tr.Commit();
        //    }

        //    ed.Regen(); // Mettre à jour l'affichage
        //}




        //[CommandMethod("OffsetEntity")]
        //public void OffsetEntity()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;

        //    // Sélection de l'entité à décaler
        //    PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez la ligne ou la polyligne à décaler : ");
        //    peo.SetRejectMessage("\nVeuillez sélectionner une ligne ou une polyligne.");
        //    peo.AddAllowedClass(typeof(Line), true);
        //    peo.AddAllowedClass(typeof(Polyline), true);

        //    PromptEntityResult per = ed.GetEntity(peo);

        //    if (per.Status != PromptStatus.OK)
        //        return;

        //    // Obtenir l'entité sélectionnée
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        Entity selectedEntity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
        //        if (selectedEntity == null)
        //            return;

        //        // Créer une nouvelle entité décalée avec un agrandissement du périmètre
        //        Entity offsetEntity = OffsetEntity(selectedEntity, 1.0);

        //        // Ajouter la nouvelle entité à la base de données
        //        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        //        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        //        btr.AppendEntity(offsetEntity);
        //        tr.AddNewlyCreatedDBObject(offsetEntity, true);

        //        tr.Commit();
        //    }

        //    ed.Regen(); // Mettre à jour l'affichage
        //}

        //private Entity OffsetEntity(Entity originalEntity, double offsetDistance)
        //{
        //    // Créer une copie de l'entité d'origine
        //    Entity offsetEntity = originalEntity.Clone() as Entity;

        //    if (offsetEntity is Line line)
        //    {
        //        // Décalage pour les lignes
        //        Vector3d lineVector = line.GetVector3d();
        //        Vector3d offsetVector = new Vector3d(-lineVector.Y, lineVector.X, 0);
        //        offsetVector = offsetVector.GetNormal() * offsetDistance;
        //        line.StartPoint = line.StartPoint + offsetVector;
        //        line.EndPoint = line.EndPoint + offsetVector;
        //    }
        //    else if (offsetEntity is Polyline polyline)
        //    {
        //        var offsetCurves = polyline.GetOffsetCurves(offsetDistance);

        //        // Créer une nouvelle polyligne à partir des courbes résultantes
        //        Polyline offsetPolyline = new Polyline();
        //        offsetPolyline.JoinEntities(offsetCurves.Cast<Entity>().ToArray());
        //        offsetEntity = offsetPolyline;
        //    }

        //    return offsetEntity;
        //}

        [CommandMethod("OffsetEntity")]
        public void OffsetEntity()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Sélection de l'entité à décaler
            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez la ligne ou la polyligne à décaler : ");
            peo.SetRejectMessage("\nVeuillez sélectionner une ligne ou une polyligne.");
            peo.AddAllowedClass(typeof(Line), true);
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return;

            // Obtenir l'entité sélectionnée
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity selectedEntity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                if (selectedEntity == null)
                    return;

                // Créer une nouvelle entité décalée
                Entity offsetEntity = OffsetEntity(selectedEntity, 1.0);

                // Ajouter la nouvelle entité à la base de données
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                btr.AppendEntity(offsetEntity);
                tr.AddNewlyCreatedDBObject(offsetEntity, true);

                tr.Commit();
            }

            ed.Regen(); // Mettre à jour l'affichage
        }

        private Entity OffsetEntity(Entity originalEntity, double offsetDistance)
        {
            // Créer une copie de l'entité d'origine
            Entity offsetEntity = originalEntity.Clone() as Entity;

            if (offsetEntity is Line line)
            {
                // Décalage pour les lignes
                Vector3d lineVector = line.GetVector3d();
                Vector3d offsetVector = new Vector3d(-lineVector.Y, lineVector.X, 0);
                offsetVector = offsetVector.GetNormal() * offsetDistance;
                line.StartPoint = line.StartPoint + offsetVector;
                line.EndPoint = line.EndPoint + offsetVector;
            }
            else if (offsetEntity is Polyline polyline)
            {
                // Décalage pour les polylignes
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    Point2d vertex = polyline.GetPoint2dAt(i);
                    polyline.SetPointAt(i, vertex + new Vector2d(offsetDistance, 0));
                }
            }

            return offsetEntity;
        }






















        [CommandMethod("Test_transient", CommandFlags.UsePickSet)]
        public void Test_transient()
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            PromptSelectionResult selResult = ed.GetSelection(new PromptSelectionOptions());
            if (selResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("No selection. Exiting...\n");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                SelectionSet selSet = selResult.Value;
                DBObjectCollection sds = new DBObjectCollection();

                foreach (ObjectId id in selSet.GetObjectIds())
                {
                    // Open each selected entity for read
                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                    sds.Add(obj);
                }

                InsertionTransientPoints insertionTransientPoints = new InsertionTransientPoints(sds, (_) => { return null; });
                var InsertionTransientPointsValues = insertionTransientPoints.GetInsertionPoint("Indiquer l'emplacement du bloc pente à ajouter");

            }
        }

        [CommandMethod("debug_random_point")]
        public static void DBG_Random_Point()
        {
            //Document doc = AcAp.DocumentManager.MdiActiveDocument;
            Random _random = new Random();

            // Generates a random number within a range.
            int RandomNumber(int min, int max)
            {
                return _random.Next(min, max);
            }
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < 50; i++)
                {
                    double x = RandomNumber(-50, 50);
                    double y = RandomNumber(-50, 50);
                    double alti = RandomNumber(100, 120) + RandomNumber(0, 99) * 0.01;
                    Point3d point = new Point3d(x, y, 0);
                    CotationElements.InsertBlocFromBlocName("_APUd_COTATIONS_Altimetries", new Points(point), Generic.GetUSCRotation(Generic.AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", alti.ToString("#.00") } });
                }
                tr.Commit();
            }
            ed.Command("_PLAN", "");
        }










        [CommandMethod("EXP", CommandFlags.UsePickSet)]
        public void ExplodeEntities()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            // Ask user to select entities
            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect objects to explode: ",
                AllowDuplicates = false,
                AllowSubSelections = true,
                RejectObjectsFromNonCurrentSpace = true,
                RejectObjectsOnLockedLayers = false
            };

            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK) return;
            // Check whether to erase the original(s)
            bool eraseOrig = false;
            if (psr.Value.Count > 0)
            {
                PromptKeywordOptions pko = new PromptKeywordOptions("\nErase original objects?")
                {
                    AllowNone = true
                };
                pko.Keywords.Add("Yes");
                pko.Keywords.Add("No");
                pko.Keywords.Default = "No";
                PromptResult pkr = ed.GetKeywords(pko);
                if (pkr.Status != PromptStatus.OK) return;
                eraseOrig = (pkr.StringResult == "Yes");
            }

            Transaction tr = db.TransactionManager.StartTransaction();

            using (tr)
            {
                // Collect our exploded objects in a single collection
                DBObjectCollection objs = new DBObjectCollection();
                // Loop through the selected objects
                foreach (SelectedObject so in psr.Value)
                {
                    // Open one at a time
                    Entity ent = (Entity)tr.GetObject(so.ObjectId, OpenMode.ForRead);
                    // Explode the object into our collection
                    ent.Explode(objs);
                    // Erase the original, if requested
                    if (eraseOrig)
                    {
                        ent.UpgradeOpen();
                        ent.Erase();
                    }
                }

                // Now open the current space in order to
                // add our resultant objects

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // Add each one of them to the current space
                // and to the transaction

                foreach (DBObject obj in objs)
                {
                    Entity ent = (Entity)obj;
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }
                // And then we commit
                tr.Commit();
            }
        }



















    }
}
