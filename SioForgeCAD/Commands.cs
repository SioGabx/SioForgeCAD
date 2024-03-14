using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Attach();
            Functions.ELLIPSETOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE2DTOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE3DTOPOLYLIGNE.ContextMenu.Attach();
        }

        public void Terminate()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Detach();
            Functions.ELLIPSETOPOLYLIGNE.ContextMenu.Detach();
            Functions.POLYLINE2DTOPOLYLIGNE.ContextMenu.Detach();
            Functions.POLYLINE3DTOPOLYLIGNE.ContextMenu.Detach();
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

        [CommandMethod("RENBLK", CommandFlags.Redraw)]
        public void RENBLK()
        {
            Functions.RENBLK.RenameBloc();
        }

        [CommandMethod("BLKMAKEUNIQUE", CommandFlags.Redraw)]
        public void BLKMAKEUNIQUE()
        {
            new Functions.BLKMAKEUNIQUE(true).MakeUniqueBlockReferences();
        }

        [CommandMethod("BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        public void BLKMAKEUNIQUEEACH()
        {
            new Functions.BLKMAKEUNIQUE(false).MakeUniqueBlockReferences();
        }

        [CommandMethod("BLKSETTOBYBBLOCK", CommandFlags.Redraw)]
        public void BLKSETTOBYBBLOCK()
        {
            Functions.BLKSETTOBYBBLOCK.ByBlock();
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
        [CommandMethod("ELLIPSETOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void ELLIPSETOPOLYLIGNE()
        {
            Functions.ELLIPSETOPOLYLIGNE.ConvertEllipseToPolylines();
        }

        [CommandMethod("POLYLINE3DTOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void POLYLINE3DTOPOLYLIGNE()
        {
            Functions.POLYLINE3DTOPOLYLIGNE.ConvertPolyline3dToPolylines();
        }

        [CommandMethod("POLYLINE2DTOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void POLYLINE2DTOPOLYLIGNE()
        {
            Functions.POLYLINE2DTOPOLYLIGNE.ConvertPolyline2dToPolylines();
        }

        [CommandMethod("DRAWCPTERRAIN", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void DRAWCPTERRAIN()
        {
            new Functions.DRAWCPTERRAIN().DrawTerrainFromSelectedPoints();
        }

        [CommandMethod("DROPCPOBJECTTOTERRAIN", CommandFlags.UsePickSet)]
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
            Functions.SPECIALSSELECTIONS.AllOnCurrentLayer();
        }

        [CommandMethod("SSOC", CommandFlags.Transparent)]
        public static void SSOC()
        {
            Functions.SPECIALSSELECTIONS.InsideCrossingPolyline();
        }

        [CommandMethod("SSOF", CommandFlags.Transparent)]
        public static void SSOF()
        {
            Functions.SPECIALSSELECTIONS.InsideStrictPolyline();
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

        //[CommandMethod("TLEN")]
        public void TLEN()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("VEGBLOC", CommandFlags.Modal)]
        public void VEGBLOC()
        {
            Functions.VEGBLOC.Create();
        }

        [CommandMethod("VEGBLOCEDIT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void VEGBLOCEDIT()
        {
            Functions.VEGBLOCEDIT.Edit();
        }

        [CommandMethod("VEGBLOCCOPYGRIP", CommandFlags.UsePickSet)]
        public void VEGBLOCCOPYGRIP()
        {
            Functions.VEGBLOCCOPYGRIP.AddGrip();
        }

        [CommandMethod("BLKTOSTATICBLOCK", CommandFlags.UsePickSet)]
        public static void BLKTOSTATICBLOCK()
        {
            Functions.BLKTOSTATICBLOCK.Convert();
        }

        [CommandMethod("BATTLEMENTS")]
        public static void BATTLEMENTS()
        {
            Functions.BATTLEMENTS.Draw();
        }

        [CommandMethod("RANDOMPAVEMENT")]
        public static void RANDOMPAVEMENT()
        {
            Functions.RANDOMPAVEMENT.Draw();
        }

        [CommandMethod("PURGEALL")]
        public static void PURGEALL()
        {
            Functions.PURGEALL.Purge();
        }


        [CommandMethod("CUTHATCH", CommandFlags.UsePickSet)]
        public static void CUTHATCH()
        {
            Functions.CUTHATCH.CutHatch();
        }
        
        [CommandMethod("TESTCUTHATCH", CommandFlags.UsePickSet)]
        public static void TESTCUTHATCH()
        {
            Functions.CUTHATCH.CutHoleHatch();
        }
        [CommandMethod("TESTMERGE", CommandFlags.UsePickSet)]
        public static void TESTMERGE()
        {
            Editor ed = Generic.GetEditor();

            // ed.TraceBoundary(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0), false);
            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
                return;

            SelectionSet sel = selRes.Value;
            List<Curve> Curves = new List<Curve>();

            using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
            {
                foreach (ObjectId selectedObjectId in sel.GetObjectIds())
                {
                    DBObject ent = selectedObjectId.GetDBObject();
                    if (ent is Curve)
                    {
                        Curve curv = ent.Clone() as Curve;
                        Curves.Add(curv);
                    }
                }
                Curves.Join().AddToDrawing(2);
                tr.Commit();
            }
            
        }





        [CommandMethod("MERGEHATCH", CommandFlags.UsePickSet)]
        public static void MERGEHATCH()
        {
            Functions.MERGEHATCH.Merge();
        }

        [CommandMethod("MERGEPOLYLIGNES", CommandFlags.UsePickSet)]
        public static void MERGEPOLYLIGNES()
        {
            Functions.MERGEPOLYLIGNES.Merge();
        }



        [CommandMethod("VPLOCK")]
        public static void VPLOCK()
        {
            Functions.VPLOCK.DoLockUnlock(true);
        }

        [CommandMethod("VPUNLOCK")]
        public static void VPUNLOCK()
        {
            Functions.VPLOCK.DoLockUnlock(false);
        }












































   
























        [CommandMethod("TESTtraytooltip")]
        public static void TESTtraytooltip()
        {
            //https://forums.autodesk.com/t5/net/statusbar-contextmenu-position/td-p/7249697/page/2
            //https://forums.autodesk.com/t5/net/statusbar-contextmenu-position/td-p/7249697/page/2
            //https://forums.autodesk.com/t5/net/statusbar-contextmenu-position/td-p/7249697/page/2


            TrayItem ti = new TrayItem();
            ti.ToolTipText = "My tray item tooltip";
            var bitmap = new Bitmap("C:\\Users\\AMPLITUDE PAYSAGE\\Downloads\\testico\\ico.png"); // or get it from resource
            var iconHandle = bitmap.GetHicon();
            ti.Icon = System.Drawing.Icon.FromHandle(iconHandle);

            Autodesk.AutoCAD.ApplicationServices.Application.StatusBar.TrayItems.Add(ti);

            Autodesk.AutoCAD.Windows.Pane pane = new Autodesk.AutoCAD.Windows.Pane();
            // pane.Icon = ti.Icon;
            pane.ToolTipText = "My Pane tooltip";
            pane.Style = Autodesk.AutoCAD.Windows.PaneStyles.Normal;
            //pane.Ba
            pane.MouseDown += new Autodesk.AutoCAD.Windows.StatusBarMouseDownEventHandler(HelloWorld);
            Autodesk.AutoCAD.ApplicationServices.Application.StatusBar.Panes.Add(pane);
        }

        public static void HelloWorld(object sender, Autodesk.AutoCAD.Windows.StatusBarMouseDownEventArgs e)
        {
            MessageBox.Show("hello");
        }

























        [CommandMethod("DEBUG", "POLYCLEAN", CommandFlags.UsePickSet)]
        public void POLYCLEAN()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var poly = ed.GetPolyline("Selectionnez une polyligne");
                int NumberOfVerticesBefore = poly.NumberOfVertices;
                poly.UpgradeOpen();
                poly.Cleanup();
                int NumberOfVerticesAfter = poly.NumberOfVertices;
                Generic.WriteMessage("La polyline à été simplifiée en supprimant " + (NumberOfVerticesBefore - NumberOfVerticesAfter));
                tr.Commit();
            }
        }


        [CommandMethod("DEBUG", "TRIANGLECC", CommandFlags.UsePickSet)]
        public void TRIANGLECC()
        {
            Commun.Triangulate.TriangulateCommand();
        }

        [CommandMethod("DEBUG", "READXDATA", CommandFlags.UsePickSet)]
        public void READXDATA()
        {

            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            var result = ed.GetEntity("Selectionnez un object");
            if (result.Status != PromptStatus.OK)
            {
                return;
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (result.ObjectId.GetDBObject() is Entity ent)
                {
                    foreach (var item in ent.ReadXData())
                    {
                        Generic.WriteMessage(item.ToString());
                    }

                }
            }
        }

        [CommandMethod("DEBUG", "ISCLOCKWISE", CommandFlags.UsePickSet)]
        public void ISCLOCKWISE()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            var result = ed.GetEntity("Selectionnez une polyligne");
            if (result.Status != PromptStatus.OK)
            {
                return;
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (result.ObjectId.GetDBObject() is Polyline poly)
                {
                    Generic.WriteMessage("Polyligne is clockwise : " + poly.IsClockwise());
                }
            }
        }


#if DEBUG
        [CommandMethod("DEBUG", "RANDOM_POINTS", CommandFlags.Transparent)]
        public static void DEBUG_RANDOM_POINTS()
        {
            Random _random = new Random();

            // Generates a random number within a range.
            int RandomNumber(int min, int max)
            {
                return _random.Next(min, max);
            }
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < 150; i++)
                {
                    double x = RandomNumber(-200, 200);
                    double y = RandomNumber(-50, 50);
                    double alti = RandomNumber(100, 120) + RandomNumber(0, 99) * 0.01;
                    Point3d point = new Point3d(x, y, alti);
                    Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist("_APUd_COTATIONS_Altimetries", new Points(point), ed.GetUSCRotation(AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", alti.ToString("#.00") } });
                }
                tr.Commit();
            }
            Generic.Command("_PLAN", "");
        }

#endif
    }
}
