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
using System.Linq;
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

        [CommandMethod("SSOC", CommandFlags.Redraw)]
        public static void SSOC()
        {
            Functions.SPECIALSSELECTIONS.InsideCrossingPolyline();
        }

        [CommandMethod("SSOF", CommandFlags.Redraw)]
        public static void SSOF()
        {
            Functions.SPECIALSSELECTIONS.InsideStrictPolyline();
        }


        [CommandMethod("SelectInPolyline")]
        public void SelectInPolylineCmd()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline: ");
            peo.SetRejectMessage("Only polylines accepted");
            peo.AddAllowedClass(typeof(Polyline), false);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;
            ObjectId plId = per.ObjectId;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (Point3dCollection vtcs = new Point3dCollection())
            {
                Polyline pl = (Polyline)tr.GetObject(plId, OpenMode.ForRead);
                (pl.GeometricExtents).ZoomExtents();
                for (int i = 0; i < pl.NumberOfVertices; i++)
                    vtcs.Add(pl.GetPoint3dAt(i));
                tr.Commit();
                PromptSelectionResult psr = ed.SelectCrossingPolygon(vtcs);
                if (psr.Status != PromptStatus.OK) return;
                ObjectId[] ids = psr.Value.GetObjectIds();
                ed.SetImpliedSelection(ids.Where(id => id != plId).ToArray());
            }
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

        [CommandMethod("RP2", CommandFlags.Transparent)]
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

        [CommandMethod("VEGBLOCLEGEND", CommandFlags.UsePickSet)]
        public void VEGBLOCLEGEND()
        {
            Functions.VEGBLOCLEGEND.Add();
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
            Functions.CUTHATCH.CutHoleHatch();
        }


        [CommandMethod("SCALEBY", CommandFlags.UsePickSet)]
        public void SCALEBY()
        {
            Functions.SCALEBY.ScaleBy();
        }

        [CommandMethod("FORCEPOINTTOBEONSCREENBOUNDS_ON", CommandFlags.Transparent)]
        public void FORCEPOINTTOBEONSCREENBOUNDS_ON()
        {
            Functions.FORCEPOINTTOBEONSCREENBOUNDS.Enable();
        }
        [CommandMethod("FORCEPOINTTOBEONSCREENBOUNDS_OFF", CommandFlags.Transparent)]
        public void FORCEPOINTTOBEONSCREENBOUNDS_OFF()
        {
            Functions.FORCEPOINTTOBEONSCREENBOUNDS.Disable();
        }




        [CommandMethod("INNERCENTROID", CommandFlags.UsePickSet)]
        public static void INNERCENTROID()
        {
            Editor ed = Generic.GetEditor();
            var poly = ed.GetPolyline("Select poly", false);
            var polygon = poly.ToPolygon(10, false);
            var PtnsCollection = polygon.GetPoints().ToPoint3dCollection();
            PtnsCollection.Add(PtnsCollection[0]);
            var pnts = PolygonOperation.GetInnerCentroid(polygon, 5);
            pnts.AddToDrawing();
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

        [CommandMethod("MERGEPOLYLIGNESAU", CommandFlags.UsePickSet)]
        public static void MERGEPOLYLIGNESAU()
        {
            Functions.MERGEPOLYLIGNES.MergeUsingRegion();
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


            TrayItem ti = new TrayItem
            {
                ToolTipText = "My tray item tooltip"
            };
            var bitmap = new Bitmap("C:\\Users\\AMPLITUDE PAYSAGE\\Downloads\\testico\\ico.png"); // or get it from resource
            var iconHandle = bitmap.GetHicon();
            ti.Icon = System.Drawing.Icon.FromHandle(iconHandle);

            Autodesk.AutoCAD.ApplicationServices.Application.StatusBar.TrayItems.Add(ti);

            Autodesk.AutoCAD.Windows.Pane pane = new Autodesk.AutoCAD.Windows.Pane
            {
                // pane.Icon = ti.Icon;
                ToolTipText = "My Pane tooltip",
                Style = Autodesk.AutoCAD.Windows.PaneStyles.Normal
            };
            //pane.Ba
            pane.MouseDown += new Autodesk.AutoCAD.Windows.StatusBarMouseDownEventHandler(HelloWorld);
            Autodesk.AutoCAD.ApplicationServices.Application.StatusBar.Panes.Add(pane);
        }

        public static void HelloWorld(object sender, Autodesk.AutoCAD.Windows.StatusBarMouseDownEventArgs e)
        {
            MessageBox.Show("hello");
        }




















        [CommandMethod("DEBUG", "TESTSHRINKOFFSET", CommandFlags.UsePickSet)]
        public void TESTSHRINKOFFSET()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var poly = ed.GetPolyline("Selectionnez une polyligne");
                int NumberOfVerticesBefore = poly.NumberOfVertices;
                poly.UpgradeOpen();
                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Distance");
                promptDoubleOptions.DefaultValue = -0.01;
                var value = ed.GetDouble(promptDoubleOptions);
                if (value.Status != PromptStatus.OK) { return; }
                var curve = poly.SmartShrinkOffset(value.Value);
                curve.AddToDrawing(5);
                tr.Commit();
            }
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

        [CommandMethod("DEBUG", "GENERATEBOUNDINGBOX", CommandFlags.UsePickSet)]
        public void GENERATEBOUNDINGBOX()
        {

            Editor ed = Generic.GetEditor();
            var result = ed.GetEntity("select");
            if (result.Status != PromptStatus.OK) { return; }
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ent = result.ObjectId.GetEntity();
                ent.GetExtents().GetGeometry().AddToDrawingCurrentTransaction();
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
