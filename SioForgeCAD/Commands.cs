using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;

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
        [CommandMethod("CCD")]
        public void CCD()
        {
            new SioForgeCAD.Functions.CCD().Compute();
        }

        [CommandMethod("CCA")]
        public void CCA()
        {
            SioForgeCAD.Functions.CCA.Compute();
        }

        [CommandMethod("CCXREF", CommandFlags.Redraw)]
        public void CCXREF()
        {
            Functions.CCXREF.MoveCotationFromXrefToCurrentDrawing();
        }

        [CommandMethod("trianglecc", CommandFlags.UsePickSet)]
        public void Trianglecc()
        {
            SioForgeCAD.Commun.Triangulate.TriangulateCommand();
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

        [CommandMethod("SSCL")]
        public static void SSCL()
        {
            Functions.SSCL.Select();
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
