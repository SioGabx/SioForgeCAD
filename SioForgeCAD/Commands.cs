using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
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
            Functions.ELLIPSETOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE2DTOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE3DTOPOLYLIGNE.ContextMenu.Attach();

            Functions.PICKSTYLETRAY.AddTray();
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
                    PluginRegister.Register();
                    break;
                case "Unregister":
                    PluginRegister.Unregister();
                    break;
            }
        }

        [CommandMethod("SIOFORGECAD", "CCI", CommandFlags.Modal)]
        public static void CCI()
        {
            new Functions.CCI().Compute();
        }

        [CommandMethod("SIOFORGECAD", "CCP", CommandFlags.Modal)]
        public static void CCP()
        {
            new Functions.CCP().Compute();
        }
        [CommandMethod("SIOFORGECAD", "CCD", CommandFlags.Modal)]
        public static void CCD()
        {
            new Functions.CCD().Compute();
        }

        [CommandMethod("SIOFORGECAD", "CCA", CommandFlags.Modal)]
        public static void CCA()
        {
            Functions.CCA.Compute();
        }

        [CommandMethod("SIOFORGECAD", "CCXREF", CommandFlags.Redraw)]
        public static void CCXREF()
        {
            Functions.CCXREF.MoveCotationFromXrefToCurrentDrawing();
        }

        [CommandMethod("SIOFORGECAD", "RENBLK", CommandFlags.Redraw)]
        public static void RENBLK()
        {
            Functions.RENBLK.RenameBloc();
        }

        [CommandMethod("SIOFORGECAD", "BLKMAKEUNIQUE", CommandFlags.Redraw)]
        public static void BLKMAKEUNIQUE()
        {
            new Functions.BLKMAKEUNIQUE(true).MakeUniqueBlockReferences();
        }

        [CommandMethod("SIOFORGECAD", "BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        public static void BLKMAKEUNIQUEEACH()
        {
            new Functions.BLKMAKEUNIQUE(false).MakeUniqueBlockReferences();
        }

        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCK", CommandFlags.Redraw)]
        public static void BLKSETTOBYBBLOCK()
        {
            Functions.BLKSETTOBYBBLOCK.ByBlock();
        }

        [CommandMethod("SIOFORGECAD", "DRAWPERPENDICULARLINEFROMPOINT", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void DRAWPERPENDICULARLINEFROMPOINT()
        {
            Functions.DRAWPERPENDICULARLINEFROMPOINT.DrawPerpendicularLineFromPoint();
        }

        [CommandMethod("SIOFORGECAD", "CIRCLETOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void CIRCLETOPOLYLIGNE()
        {
            Functions.CIRCLETOPOLYLIGNE.ConvertCirclesToPolylines();
        }
        [CommandMethod("SIOFORGECAD", "ELLIPSETOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void ELLIPSETOPOLYLIGNE()
        {
            Functions.ELLIPSETOPOLYLIGNE.ConvertEllipseToPolylines();
        }

        [CommandMethod("SIOFORGECAD", "POLYLINE3DTOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void POLYLINE3DTOPOLYLIGNE()
        {
            Functions.POLYLINE3DTOPOLYLIGNE.ConvertPolyline3dToPolylines();
        }

        [CommandMethod("SIOFORGECAD", "POLYLINE2DTOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void POLYLINE2DTOPOLYLIGNE()
        {
            Functions.POLYLINE2DTOPOLYLIGNE.ConvertPolyline2dToPolylines();
        }

        [CommandMethod("SIOFORGECAD", "DRAWCPTERRAIN", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void DRAWCPTERRAIN()
        {
            new Functions.DRAWCPTERRAIN().DrawTerrainFromSelectedPoints();
        }

        [CommandMethod("SIOFORGECAD", "DROPCPOBJECTTOTERRAIN", CommandFlags.UsePickSet)]
        public static void DROPCPOBJECTTOTERRAIN()
        {
            Functions.DROPCPOBJECTTOTERRAIN.Project();
        }

        [CommandMethod("SIOFORGECAD", "FORCELAYERCOLORTOENTITY", CommandFlags.UsePickSet)]
        public static void FORCELAYERCOLORTOENTITY()
        {
            Functions.FORCELAYERCOLORTOENTITY.Convert();
        }

        [CommandMethod("SIOFORGECAD", "SSCL", CommandFlags.Transparent)]
        public static void SSCL()
        {
            Functions.SPECIALSSELECTIONS.AllOnCurrentLayer();
        }

        [CommandMethod("SIOFORGECAD", "SSOC", CommandFlags.Redraw)]
        public static void SSOC()
        {
            Functions.SPECIALSSELECTIONS.InsideCrossingPolyline();
        }

        [CommandMethod("SIOFORGECAD", "SSOF", CommandFlags.Redraw)]
        public static void SSOF()
        {
            Functions.SPECIALSSELECTIONS.InsideStrictPolyline();
        }

        [CommandMethod("SIOFORGECAD", "RRR", CommandFlags.UsePickSet)]
        public static void RRR()
        {
            Functions.RRR.Rotate();
        }

        [CommandMethod("SIOFORGECAD", "BLKINSEDIT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        [CommandMethod("SIOFORGECAD", "INSEDIT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void BLKINSEDIT()
        {
            Functions.BLKINSEDIT.MoveBasePoint();
        }

        [CommandMethod("SIOFORGECAD", "RP2", CommandFlags.Transparent)]
        public static void RP2()
        {
            Functions.RP2.RotateUCS();
        }

        [CommandMethod("SIOFORGECAD", "TAREA", CommandFlags.Modal)]
        public void TAREA()
        {
            throw new NotImplementedException();
        }

        //[CommandMethod("TLEN")]
        public void TLEN()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOC", CommandFlags.Modal)]
        public static void VEGBLOC()
        {
            Functions.VEGBLOC.Create();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOCEDIT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public static void VEGBLOCEDIT()
        {
            Functions.VEGBLOCEDIT.Edit();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOCCOPYGRIP", CommandFlags.UsePickSet)]
        public static void VEGBLOCCOPYGRIP()
        {
            Functions.VEGBLOCCOPYGRIP.AddGrip();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOCLEGEND", CommandFlags.UsePickSet)]
        public static void VEGBLOCLEGEND()
        {
            Functions.VEGBLOCLEGEND.Add();
        }

        [CommandMethod("SIOFORGECAD", "BLKTOSTATICBLOCK", CommandFlags.UsePickSet)]
        public static void BLKTOSTATICBLOCK()
        {
            Functions.BLKTOSTATICBLOCK.Convert();
        }

        [CommandMethod("SIOFORGECAD", "BATTLEMENTS", CommandFlags.Modal)]
        public static void BATTLEMENTS()
        {
            Functions.BATTLEMENTS.Draw();
        }

        [CommandMethod("SIOFORGECAD", "RANDOMPAVEMENT", CommandFlags.Modal)]
        public static void RANDOMPAVEMENT()
        {
            Functions.RANDOMPAVEMENT.Draw();
        }

        [CommandMethod("SIOFORGECAD", "PURGEALL", CommandFlags.Modal)]
        public static void PURGEALL()
        {
            Functions.PURGEALL.Purge();
        }

        [CommandMethod("SIOFORGECAD", "CUTHATCH", CommandFlags.UsePickSet)]
        public static void CUTHATCH()
        {
            Functions.CUTHATCH.CutHoleHatch();
        }

        [CommandMethod("SIOFORGECAD", "SCALEBY", CommandFlags.UsePickSet)]
        public static void SCALEBY()
        {
            Functions.SCALEBY.ScaleBy();
        }

        [CommandMethod("SIOFORGECAD", "INNERCENTROID", CommandFlags.UsePickSet)]
        public static void INNERCENTROID()
        {
            Editor ed = Generic.GetEditor();
            var poly = ed.GetPolyline("Select poly", false);
            var polygon = poly.ToPolygon(10);
            polygon.AddToDrawing();
            var PtnsCollection = polygon.GetPoints().ToPoint3dCollection();
            PtnsCollection.Add(PtnsCollection[0]);
            var pnts = PolygonOperation.GetInnerCentroid(polygon, 5);
            pnts.AddToDrawing();
        }

        [CommandMethod("SIOFORGECAD", "MERGEHATCH", CommandFlags.UsePickSet)]
        public static void MERGEHATCH()
        {
            Functions.MERGEHATCH.Merge();
        }

        [CommandMethod("SIOFORGECAD", "MERGEPOLYLIGNES", CommandFlags.UsePickSet)]
        public static void MERGEPOLYLIGNES()
        {
            Functions.MERGEPOLYLIGNES.Merge();
        }

        //[CommandMethod("MERGEPOLYLIGNESAU", CommandFlags.UsePickSet)]
        //public static void MERGEPOLYLIGNESAU()
        //{
        //    Functions.MERGEPOLYLIGNES.MergeUsingRegion();
        //}
        [CommandMethod("SIOFORGECAD", "POLYLINEISCLOCKWISE", CommandFlags.UsePickSet)]
        public static void ISCLOCKWISE()
        {
            var ed = Generic.GetEditor();
            var result = ed.GetPolyline("Selectionnez une polyligne");
            if (result == null)
            {
                return;
            }
            Generic.WriteMessage($"L'orientation de la polyline est {(result.IsClockwise() ? "CLOCKWISE" : "ANTICLOCKWISE")}");
        }

        [CommandMethod("SIOFORGECAD", "VPLOCK", CommandFlags.Modal)]
        public static void VPLOCK()
        {
            Functions.VPLOCK.DoLockUnlock(true);
        }

        [CommandMethod("SIOFORGECAD", "VPUNLOCK", CommandFlags.Modal)]
        public static void VPUNLOCK()
        {
            Functions.VPLOCK.DoLockUnlock(false);
        }

        [CommandMethod("SIOFORGECAD", "POLYCLEAN", CommandFlags.UsePickSet)]
        public static void POLYCLEAN()
        {
            Functions.POLYCLEAN.PolyClean();
        }

        [CommandMethod("SIOFORGECAD", "PICKSTYLETRAY", CommandFlags.Transparent)]
        public static void PICKSTYLETRAY()
        {
            Functions.PICKSTYLETRAY.AddTray();
        }

        [CommandMethod("SIOFORGECAD", "EMBEDIMAGEASOLE", CommandFlags.UsePickSet)]
        public static void EMBEDIMAGEASOLE()
        {
            Functions.EMBEDIMAGEASOLE.EmbedToOle();
        }



            [CommandMethod("SIOFORGECAD", "CURVEPOLYTOPOLYGON", CommandFlags.UsePickSet)]
        public static void CURVEPOLYTOPOLYGON()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var poly = ed.GetPolyline("Selectionnez une polyligne");
                int NumberOfVerticesBefore = poly.NumberOfVertices;
                poly.UpgradeOpen();
                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Number Of Segment per Arc")
                {
                    DefaultValue = 3
                };
                var value = ed.GetDouble(promptDoubleOptions);
                if (value.Status != PromptStatus.OK) { return; }
                var Polygon = poly.ToPolygon((uint)value.Value);
                poly.CopyPropertiesTo(Polygon);
                Polygon.AddToDrawing(5);
                poly.EraseObject();
                tr.Commit();
            }
        }

        [CommandMethod("DEBUG", "TESTSHRINKOFFSET", CommandFlags.UsePickSet)]
        public static void TESTSHRINKOFFSET()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var poly = ed.GetPolyline("Selectionnez une polyligne");
                int NumberOfVerticesBefore = poly.NumberOfVertices;
                poly.UpgradeOpen();
                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Distance")
                {
                    DefaultValue = -0.01
                };
                var value = ed.GetDouble(promptDoubleOptions);
                if (value.Status != PromptStatus.OK) { return; }
                var curve = poly.SmartOffset(value.Value);
                curve.AddToDrawing(5);
                tr.Commit();
            }
        }

        [CommandMethod("DEBUG", "TESTMERGE", CommandFlags.UsePickSet)]
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
                Curves.JoinMerge().AddToDrawing(2);
                tr.Commit();
            }
        }

        [CommandMethod("SIOFORGECAD", "GENERATEBOUNDINGBOX", CommandFlags.UsePickSet)]
        public static void GENERATEBOUNDINGBOX()
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
        public static void TRIANGLECC()
        {
            Triangulate.TriangulateCommand();
        }

        [CommandMethod("DEBUG", "READXDATA", CommandFlags.UsePickSet)]
        public static void READXDATA()
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
                    double alti = RandomNumber(100, 120) + (RandomNumber(0, 99) * 0.01);
                    Point3d point = new Point3d(x, y, alti);
                    BlockReferences.InsertFromNameImportIfNotExist("_APUd_COTATIONS_Altimetries", new Points(point), ed.GetUSCRotation(AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", alti.ToString("#.00") } });
                }
                tr.Commit();
            }
            Generic.Command("_PLAN", "");
        }

#endif
    }
}
