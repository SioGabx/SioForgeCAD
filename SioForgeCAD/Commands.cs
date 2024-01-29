using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Attach();
        }

        public void Terminate()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Detach();
        }

        [CommandMethod("SIOFORGECAD")]
        public static void SIOFORGECAD()
        {
            var ed = Generic.GetEditor();
            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("Veuillez selectionner une option")
            {
                AppendKeywordsToMessage = true
            };
            promptKeywordOptions.Keywords.Add("Settings");
            promptKeywordOptions.Keywords.Add("Register");
            promptKeywordOptions.Keywords.Add("Unregister");
            var result = ed.GetKeywords(promptKeywordOptions);
            switch (result.StringResult)
            {
                case "Settings":
                    break;
                case "Register":
                    Commun.PluginRegister.Register();
                    break;
                case "Unregister":
                    Commun.PluginRegister.Unregister();
                    break;
            }
        }


        [CommandMethod("CCI")]
        public void CCI()
        {
            new Functions.CCI().Compute();
        }

        [CommandMethod("CCP")]
        public void CCP()
        {
            new Functions.CCP().Compute();
        }
        [CommandMethod("CCD")]
        public void CCD()
        {
            new Functions.CCD().Compute();
        }

        [CommandMethod("CCA")]
        public void CCA()
        {
            Functions.CCA.Compute();
        }

        [CommandMethod("CCXREF", CommandFlags.Redraw)]
        public void CCXREF()
        {
            Functions.CCXREF.MoveCotationFromXrefToCurrentDrawing();
        }

        [CommandMethod("trianglecc", CommandFlags.UsePickSet)]
        public void Trianglecc()
        {
            Commun.Triangulate.TriangulateCommand();
        }

        [CommandMethod("RENBLK", CommandFlags.Redraw)]
        public void RENBLK()
        {
            Functions.RENBLK.RenameBloc();
        }

        [CommandMethod("BLKMAKEUNIQUE", CommandFlags.Redraw)]
        public void MAKEUNIQUBLK()
        {
            new Functions.BLKMAKEUNIQUE(true).MakeUniqueBlockReferences();
        }

        [CommandMethod("BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        public void BLKMAKEUNIQUEEACH()
        {
            new Functions.BLKMAKEUNIQUE(false).MakeUniqueBlockReferences();
        }

        [CommandMethod("DRAWPERPENDICULARLINEFROMPOINT", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void DRAWPERPENDICULARLINEFROMPOINT()
        {
            Functions.DRAWPERPENDICULARLINEFROMPOINT.DrawPerpendicularLineFromPoint();
        }

        [CommandMethod("CIRCLETOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void CIRCLETOPOLYLIGNE()
        {
            Functions.CIRCLETOPOLYLIGNE.ConvertCirclesToPolylines();
        }

        [CommandMethod("DRAWCPTERRAIN", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void DRAWCPTERRAIN()
        {
            new Functions.DRAWCPTERRAIN().DrawTerrainFromSelectedPoints();
        }

        [CommandMethod("DROPCPOBJECTTOTERRAIN")]
        public static void DROPCPOBJECTTOTERRAIN()
        {
            Functions.DROPCPOBJECTTOTERRAIN.Project();
        }

        [CommandMethod("FORCELAYERCOLORTOENTITY", CommandFlags.UsePickSet)]
        public static void FORCELAYERCOLORTOENTITY()
        {
            Functions.FORCELAYERCOLORTOENTITY.Convert();
        }

        [CommandMethod("SSCL", CommandFlags.Transparent)]
        public static void SSCL()
        {
            Functions.SSCL.Select();
        }

        [CommandMethod("RRR", CommandFlags.UsePickSet)]
        public static void RRR()
        {
            Functions.RRR.Rotate();
        }

        [CommandMethod("BLKINSEDIT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        [CommandMethod("INSEDIT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public void BLKINSEDIT()
        {
            Functions.BLKINSEDIT.MoveBasePoint();
        }

        [CommandMethod("RP2")]
        public void RP2()
        {
            Functions.RP2.RotateUCS();
        }

        [CommandMethod("TAREA")]
        public void TAREA()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("TLEN")]
        public void TLEN()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("VEGBLOC", CommandFlags.Modal)]
        public void VEGBLOC()
        {
            Functions.VEGBLOC.Create();
        }






        [CommandMethod("EraseBasePointParameter", CommandFlags.UsePickSet)]
        public void EraseBasePointParameter()
        {
            Database db = Generic.GetDatabase();
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            //var PromptSelectEntitiesOptions = new PromptSelectionOptions()
            //{
            //    MessageForAdding = "Selectionnez les entités"
            //};

            //var AllSelectedObject = ed.GetSelection(PromptSelectEntitiesOptions);

            //if (AllSelectedObject.Status != PromptStatus.OK)
            //{
            //    return;
            //}

            SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "BASEPOINTPARAMETERENTITY") });
            PromptSelectionResult AllSelectedObject = ed.SelectAll(filter);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var x = AllSelectedObject.Value;
                var y = x.GetObjectIds();
                foreach (ObjectId obj in y)
                {
                    obj.EraseObject();
                }
                Debug.WriteLine(y.Length);
                tr.Commit();
            }
        }


        [CommandMethod("AnalyseSelectionInDebbuger", CommandFlags.UsePickSet)]
        public void AnalyseSelectionInDebbuger()
        {
            Database db = Generic.GetDatabase();
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();

            PromptSelectionResult AllSelectedObject = ed.GetSelection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var x = AllSelectedObject.Value;
                var y = x.GetObjectIds();
                foreach (ObjectId obj in y)
                {
                    DBObject dbobj = obj.GetDBObject();
                    Debug.WriteLine(dbobj.GetType());
                    Debugger.Break();
                }
                Debug.WriteLine(y.Length);
                tr.Commit();
            }
        }

        [CommandMethod("DrawExtend", CommandFlags.UsePickSet)]
        public void DrawExtend()
        {
            Database db = Generic.GetDatabase();
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    var entity = ed.GetSelection();
                    foreach (var item in entity.Value.GetObjectIds())
                    {
                        var obj = item.GetDBObject();
                        if (obj is Entity ent)
                        {
                            var GeometricExtents = ent.GeometricExtents;
                            var TopLeft = GeometricExtents.TopLeft();
                            var TopRight = GeometricExtents.TopRight();
                            var BottomLeft = GeometricExtents.BottomLeft();
                            var BottomRight = GeometricExtents.BottomRight();
                            Lines.Draw(TopRight, TopLeft, 125);
                            Lines.Draw(TopLeft, BottomLeft, 55);
                            Lines.Draw(BottomLeft, BottomRight, 24);
                            Lines.Draw(BottomRight, TopRight, 48);
                        }
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }
        }


        [CommandMethod("TOSTATICBLOCK")]
        public static void TOSTATICBLOCK()
        {
            Editor editor = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            PromptSelectionResult promptResult;

            while (true)
            {
                promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count > 0)
                    {
                        break;
                    }
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ed = Generic.GetEditor();
                ObjectId blockRefId = promptResult.Value.GetObjectIds().First();
                if (!(tr.GetObject(blockRefId, OpenMode.ForWrite) is BlockReference blockRef))
                {
                    return;
                }
                blockRef.ConvertToStaticBlock("STATIC_" + blockRef.GetBlockReferenceName());
                tr.Commit();
            }

        }



        [CommandMethod("GETBOUND")]
        public static void GETBOUND()
        {
            Editor editor = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            Document doc = Generic.GetDocument();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            PromptSelectionResult promptResult;

            while (true)
            {
                promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count > 0)
                    {
                        break;
                    }
                }
            }



            ObjectId blockRefId = promptResult.Value.GetObjectIds().First();
            var point = Functions.BLKINSEDIT.GetOriginalBasePointInDynamicBlockWithBasePoint(blockRefId);
            using (var tr2 = doc.TransactionManager.StartTransaction())
            {
                Circles.Draw(new Points(point), 0.2, 5);
                Circles.Draw(new Points(point), 20, 5);
                tr2.Commit();
            }

        }




        [CommandMethod("MB")]
        public static void MergeBlocks()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }
            var db = doc.Database;
            var ed = doc.Editor;
            string first = "AAA";
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(first))
                {
                    string second = "BBB";
                    if (bt.Has(second))
                    {
                        string merged = "CCC" + DateTime.Now.Ticks;
                        if (!bt.Has(merged))
                        {

                            var ids = new ObjectIdCollection();
                            var btr1 = tr.GetObject(bt[first], OpenMode.ForRead) as BlockTableRecord;
                            var btr2 = tr.GetObject(bt[second], OpenMode.ForRead) as BlockTableRecord;
                            var en1 = btr1.Cast<ObjectId>();
                            var en2 = btr2.Cast<ObjectId>();
                            ids.Add(en1.ToArray<ObjectId>());
                            ids.Add(en2.ToArray<ObjectId>());
                            var btr = new BlockTableRecord();
                            btr.Name = merged;
                            bt.UpgradeOpen();
                            var btrId = bt.Add(btr);
                            tr.AddNewlyCreatedDBObject(btr, true);
                            var idMap = new IdMapping();
                            db.DeepCloneObjects(ids, btrId, idMap, false);
                            ed.WriteMessage("\nBlock \"{0}\" created.", merged);
                            BlockReferences.InsertFromName(merged, Points.Empty, 0, null, null);
                        }
                        else
                        {
                            ed.WriteMessage("\nDrawing already contains a block named \"{0}\".", merged);
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\nBlock \"{0}\" not found.", second);
                    }

                }
                else
                {
                    ed.WriteMessage("\nBlock \"{0}\" not found.", first);
                }
                tr.Commit();
            }

        }



































        [CommandMethod("debug_random_point")]
        public static void DBG_Random_Point()
        {
            Random _random = new Random();

            // Generates a random number within a range.
            int RandomNumber(int min, int max)
            {
                return _random.Next(min, max);
            }
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < 150; i++)
                {
                    double x = RandomNumber(-200, 200);
                    double y = RandomNumber(-50, 50);
                    double alti = RandomNumber(100, 120) + RandomNumber(0, 99) * 0.01;
                    Point3d point = new Point3d(x, y, 0);
                    Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist("_APUd_COTATIONS_Altimetries", new Points(point), Generic.GetUSCRotation(Generic.AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", alti.ToString("#.00") } });
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
