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
using System.Diagnostics;
using System.Windows;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1102:Make class static")]
    public class Commands
    {
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

        [CommandMethod("SIOFORGECAD", "CIRCLETOPOLYLIGNE", CommandFlags.Redraw)]
        public static void CIRCLETOPOLYLIGNE()
        {
            Functions.CIRCLETOPOLYLIGNE.ConvertCirclesToPolylines();
        }
        [CommandMethod("SIOFORGECAD", "ELLIPSETOPOLYLIGNE", CommandFlags.Redraw)]
        public static void ELLIPSETOPOLYLIGNE()
        {
            Functions.ELLIPSETOPOLYLIGNE.ConvertEllipseToPolylines();
        }

        [CommandMethod("SIOFORGECAD", "POLYLINE3DTOPOLYLIGNE", CommandFlags.Redraw)]
        public static void POLYLINE3DTOPOLYLIGNE()
        {
            Functions.POLYLINE3DTOPOLYLIGNE.ConvertPolyline3dToPolylines();
        }

        [CommandMethod("SIOFORGECAD", "POLYLINE2DTOPOLYLIGNE", CommandFlags.Redraw)]
        public static void POLYLINE2DTOPOLYLIGNE()
        {
            Functions.POLYLINE2DTOPOLYLIGNE.ConvertPolyline2dToPolylines();
        }

        [CommandMethod("SIOFORGECAD", "DRAWCPTERRAIN", CommandFlags.Redraw)]
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

        [CommandMethod("SIOFORGECAD", "BLKINSEDIT", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "INSEDIT", CommandFlags.UsePickSet)]
        public static void BLKINSEDIT()
        {
            Functions.BLKINSEDIT.MoveBasePoint();
        }

        [CommandMethod("SIOFORGECAD", "RP2", CommandFlags.Transparent)]
        public static void RP2()
        {
            Functions.RP2.RotateUCS();
        }

        [CommandMethod("SIOFORGECAD", "TAREA", CommandFlags.Redraw)]
        public static void TAREA()
        {
            Functions.TAREA.Compute();
        }

        [CommandMethod("SIOFORGECAD", "TLENS", CommandFlags.Redraw)]
        public static void TLEN()
        {
            Functions.TLEN.Compute();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOC", CommandFlags.Modal)]
        public static void VEGBLOC()
        {
            Functions.VEGBLOC.Create();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOCEDIT", CommandFlags.UsePickSet)]
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

        [CommandMethod("SIOFORGECAD", "SCALEFIT", CommandFlags.UsePickSet)]
        public static void SCALEFIT()
        {
            Functions.SCALEFIT.ScaleFit();
        }

        [CommandMethod("SIOFORGECAD", "GETINNERCENTROID", CommandFlags.UsePickSet)]
        public static void GETINNERCENTROID()
        {
            Functions.GETINNERCENTROID.Get();
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

        [CommandMethod("SIOFORGECAD", "SUBSTRACTPOLYLIGNES", CommandFlags.UsePickSet)]
        public static void SUBSTRACTPOLYLIGNES()
        {
            Functions.SUBSTRACTPOLYLIGNES.Substract();
        }

        [CommandMethod("SIOFORGECAD", "POLYISCLOCKWISE", CommandFlags.UsePickSet)]
        public static void POLYISCLOCKWISE()
        {
            Functions.POLYISCLOCKWISE.Check();
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

        [CommandMethod("SIOFORGECAD", "CONVERTIMAGETOOLE", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "EMBEDIMAGE", CommandFlags.UsePickSet)]
        public static void CONVERTIMAGETOOLE()
        {
            Functions.CONVERTIMAGETOOLE.RasterToOle();
        }

        [CommandMethod("SIOFORGECAD", "CURVETOPOLYGON", CommandFlags.UsePickSet)]
        public static void CURVETOPOLYGON()
        {
            Functions.CURVETOPOLYGON.Convert();
        }

        [CommandMethod("SIOFORGECAD", "CHANGESPACECOPY", CommandFlags.UsePickSet)]
        public static void CHANGESPACECOPY()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("SIOFORGECAD", "DELETESUBGROUP", CommandFlags.Redraw)]
        public static void DELETESUBGROUP()
        {
            Functions.DELETESUBGROUP.Delete();
        }

        [CommandMethod("SIOFORGECAD", "EXECUTECOMMANDONEACHSELECTED", CommandFlags.UsePickSet)]
        public static void EXECUTECOMMANDONEACHSELECTED()
        {
            Functions.EXECUTECOMMANDONEACHSELECTED.Execute();
        }

        [CommandMethod("SIOFORGECAD", "LIMITNUMBERINSELECTION", CommandFlags.Redraw)]
        public static void LIMITNUMBERINSELECTION()
        {
            Functions.LIMITNUMBERINSELECTION.LimitToOne();
        }
        [CommandMethod("RotateBlockContent", CommandFlags.UsePickSet)]
        public void RotateBlockContent()
        {
            // Récupérer le document actuel et la base de données
            Document doc = Generic.GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Sélectionner un bloc
            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez un bloc :");
            peo.SetRejectMessage("\nVeuillez sélectionner un bloc valide.");
            peo.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            // Ouvrir le bloc sélectionné pour lecture
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                if (blockRef != null)
                {
                    // Récupérer la matrice de transformation du bloc
                    Matrix3d transform = blockRef.BlockTransform;

                    // Créer une matrice de rotation de 90 degrés autour de l'axe X
                    Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI / 2.0, Vector3d.XAxis, blockRef.Position);

                    // Combiner les matrices de transformation du bloc et de rotation
                    Matrix3d newTransform = transform.PreMultiplyBy(rotationMatrix);

                    // Modifier la position et la rotation du bloc
                    blockRef.UpgradeOpen();
                    blockRef.BlockTransform = newTransform;

                    // Valider la transaction
                    tr.Commit();

                    ed.WriteMessage("\nContenu du bloc tourné de 90 degrés autour de l'axe X avec succès.");
                }
                else
                {
                    ed.WriteMessage("\nL'entité sélectionnée n'est pas un bloc.");
                }
            }
        }

        [CommandMethod("RotateEntityOnZAxis", CommandFlags.UsePickSet)]
        public void RotateEntityOnZAxis()
        {
            // Get the current document and database
            Document doc = Generic.GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Select an entity to rotate
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an entity to rotate:");
            peo.SetRejectMessage("\nPlease select a valid entity.");
            var per = ed.GetSelectionRedraw();
            if (per.Status != PromptStatus.OK) return;


            // Parse the clipboard content as a double
            double defaultRotationAngle = 0.0;
            if (double.TryParse(Clipboard.GetText(), out double clipboardValue))
            {
                defaultRotationAngle = clipboardValue;
            }


            // Get the rotation angle
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEnter rotation angle in degrees:");
            pdo.AllowNegative = true;
            pdo.AllowZero = false;
            pdo.DefaultValue = defaultRotationAngle;
            pdo.UseDefaultValue = true;
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;

            double angleInDegrees = pdr.Value;

            // Open the selected entity for write
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var item in per.Value.GetObjectIds())
                {
                    Entity entity = tr.GetObject(item, OpenMode.ForWrite) as Entity;
                    if (entity != null)
                    {
                        // Get the current bounding box
                        Extents3d extents = entity.GeometricExtents;

                        // Calculate the center of the bounding box
                        Point3d center = new Point3d(
                            (extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                            (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0,
                            (extents.MinPoint.Z + extents.MaxPoint.Z) / 2.0
                        );

                        // Get the current transformation matrix
                        Matrix3d transform = Matrix3d.Identity;

                        // Rotate the entity around the center of the bounding box
                        Matrix3d rotationMatrix = Matrix3d.Rotation(
                            angleInDegrees * (Math.PI / 180),  // Convert degrees to radians
                            Vector3d.ZAxis,
                            center
                        );

                        // Apply the rotation to the transformation matrix
                        transform = transform.PreMultiplyBy(rotationMatrix);

                        // Transform the entity
                        entity.TransformBy(transform);

                        // Commit the transaction
                        tr.Commit();

                        // Update the display
                        doc.Editor.Regen();
                        ed.WriteMessage($"\nEntity rotated by {angleInDegrees} degrees around the center of its bounding box.");
                    }
                    else
                    {
                        ed.WriteMessage("\nThe selected entity is invalid.");
                    }
                }
            }
        }

        [CommandMethod("DEBUG", "TESTSHRINKOFFSET", CommandFlags.UsePickSet)]
        public static void TESTSHRINKOFFSET()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (var poly = ed.GetPolyline("Selectionnez une polyligne"))
            {
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
