using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        [CommandMethod("Test_transient")]
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
        public void DBG_Random_Point()
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

        }










        [CommandMethod("EXP", CommandFlags.UsePickSet)]

        public void ExplodeEntities()

        {

            Document doc =

                Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;



            // Ask user to select entities



            PromptSelectionOptions pso =

              new PromptSelectionOptions();

            pso.MessageForAdding = "\nSelect objects to explode: ";

            pso.AllowDuplicates = false;

            pso.AllowSubSelections = true;

            pso.RejectObjectsFromNonCurrentSpace = true;

            pso.RejectObjectsOnLockedLayers = false;



            PromptSelectionResult psr = ed.GetSelection(pso);

            if (psr.Status != PromptStatus.OK)

                return;



            // Check whether to erase the original(s)



            bool eraseOrig = false;



            if (psr.Value.Count > 0)

            {

                PromptKeywordOptions pko =

                  new PromptKeywordOptions("\nErase original objects?");

                pko.AllowNone = true;

                pko.Keywords.Add("Yes");

                pko.Keywords.Add("No");

                pko.Keywords.Default = "No";



                PromptResult pkr = ed.GetKeywords(pko);

                if (pkr.Status != PromptStatus.OK)

                    return;



                eraseOrig = (pkr.StringResult == "Yes");

            }



            Transaction tr =

              db.TransactionManager.StartTransaction();

            using (tr)

            {

                // Collect our exploded objects in a single collection



                DBObjectCollection objs = new DBObjectCollection();



                // Loop through the selected objects



                foreach (SelectedObject so in psr.Value)

                {

                    // Open one at a time



                    Entity ent =

                      (Entity)tr.GetObject(

                        so.ObjectId,

                        OpenMode.ForRead

                      );



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



                BlockTableRecord btr =

                  (BlockTableRecord)tr.GetObject(

                    db.CurrentSpaceId,

                    OpenMode.ForWrite

                  );



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
