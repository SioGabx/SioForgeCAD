using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1102:Make class static")]
    public class Commands
    {
        //https://forums.autodesk.com/t5/net/net-ribbon-persistance/td-p/12803033
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
            promptKeywordOptions.Keywords.Add("LoadCUIX");
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
                case "LoadCUIX":
                    var doc = Generic.GetDocument();
                    var cs = doc.CreatePartialCui("SIOFORGECAD");
                    cs.AddMacro("Mesurer", "^C^C_MEASUREGEOM _distance", "SFC_Dist", "Mesure la distance entre deux point ou le long d'une polyligne.", "RCDATA_32_QUICKMEASURE", "_MEASUREGEOM", true);
                    cs.AddMacro("Précédente sélection", "(if (wcmatch (getvar 'cmdnames) \"\") (sssetfirst nil (ssget \"_P\")) (ssget \"_P\"))", "SFC_PSelectLast", "Récupère la dernière sélection", "RCDATA_16_SELWIN", "_PSELECT", true);
                    cs.AddPermanentKeyboardShortcut("F1", "Cancel F1", "^C^C", "ID_Cancel_F1");
                    cs.AddPermanentKeyboardShortcut("F4", "Cancel F4", "^C^C", "ID_Cancel_F4");
                    cs.AddPermanentKeyboardShortcut("CTRL+Q", "Toggle QPMODE", "'_setvar;pickstyle;$M=$(if,$(eq,$(getvar,pickstyle),2),1,2)", "Toggle_QPMODE");
                    cs.LoadCui();
                    AcApp.ReloadAllMenus();
                    break;
            }
        }

        [CommandMethod("SIOFORGECAD", "CCI", CommandFlags.Modal)]
        //Compute a intermedian point between two points
        public static void CCI()
        {
            new Functions.CCI().Compute();
        }

        [CommandMethod("SIOFORGECAD", "CCP", CommandFlags.Modal)]
        //Compute the slope value between two points
        public static void CCP()
        {
            new Functions.CCP().Compute();
        }
        [CommandMethod("SIOFORGECAD", "CCD", CommandFlags.Modal)]
        //Compute a new point from existing one using a slope value
        public static void CCD()
        {
            new Functions.CCD().Compute();
        }

        [CommandMethod("SIOFORGECAD", "CCA", CommandFlags.Modal)]
        //Take a altitude point and add or substract a user defined value to it
        public static void CCA()
        {
            Functions.CCA.Compute();
        }

        [CommandMethod("SIOFORGECAD", "CCFROMTEXT", CommandFlags.Modal)]
        //Take a altitude from a text and add a block
        public static void CCFROMTEXT()
        {
            Functions.CCFROMTEXT.CreateCotationBlocFromText();
        }

        [CommandMethod("SIOFORGECAD", "CCXREF", CommandFlags.Redraw)]
        //Move a XREF point into the drawing
        public static void CCXREF()
        {
            Functions.CCXREF.MoveCotationFromXrefToCurrentDrawing();
        }

        [CommandMethod("SIOFORGECAD", "RENBLK", CommandFlags.Redraw)]
        //Allow user to rename a block
        public static void RENBLK()
        {
            Functions.RENBLK.RenameBloc();
        }

        [CommandMethod("SIOFORGECAD", "BLKMAKEUNIQUE", CommandFlags.Redraw)]
        //Makes the block instance unique. If several of the selected blocks have the same name, they will then share a new common instance
        public static void BLKMAKEUNIQUE()
        {
            new Functions.BLKMAKEUNIQUE(true).MakeUniqueBlockReferences();
        }

        [CommandMethod("SIOFORGECAD", "BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        //Makes the block instance unique. If several of the selected blocks have the same name, they will NOT share a new common instance
        public static void BLKMAKEUNIQUEEACH()
        {
            new Functions.BLKMAKEUNIQUE(false).MakeUniqueBlockReferences();
        }

        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCK", CommandFlags.Redraw)]
        //Convert all entity values in the block to BYBLOCK
        public static void BLKSETTOBYBBLOCK()
        {
            Functions.BLKSETTOBYBBLOCK.ByBlock(Functions.BLKSETTOBYBBLOCK.HatchSupport.Include);
        }

        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCKIGNOREHATCH", CommandFlags.Redraw)]
        //Convert all entity values in the block to BYBLOCK, ignoring HATCH
        public static void BLKSETTOBYBBLOCKIGNOREHATCH()
        {
            Functions.BLKSETTOBYBBLOCK.ByBlock(Functions.BLKSETTOBYBBLOCK.HatchSupport.Ignore);
        }

        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCKHATCHSETTOWHITE", CommandFlags.Redraw)]
        //Convert all entity values in the block to BYBLOCK, HATCH to white (rgb 255,255,255)
        public static void BLKSETTOBYBBLOCKHATCHSETTOWHITE()
        {
            Functions.BLKSETTOBYBBLOCK.ByBlock(Functions.BLKSETTOBYBBLOCK.HatchSupport.SetToWhite);
        }

        [CommandMethod("SIOFORGECAD", "BLKINSEDIT", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "INSEDIT", CommandFlags.UsePickSet)]
        //Allow user to redefine the basepoint of a block instance without moving it
        public static void BLKINSEDIT()
        {
            Functions.BLKINSEDIT.MoveBasePoint();
        }

        [CommandMethod("SIOFORGECAD", "BLKTOSTATICBLOCK", CommandFlags.UsePickSet)]
        //Convert dynamic block to static block
        public static void BLKTOSTATICBLOCK()
        {
            Functions.BLKTOSTATICBLOCK.Convert();
        }

        [CommandMethod("SIOFORGECAD", "BLKTOXREF", CommandFlags.UsePickSet)]
        //Convert a block to a XREF
        public static void BLKTOXREF()
        {
            Functions.BLKTOXREF.Convert();
        }

        [CommandMethod("SIOFORGECAD", "BLKADDENTITIES", CommandFlags.UsePickSet)]
        public static void BLKADDENTITIES()
        {
            Functions.BLKADDENTITIES.Add();
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

        [CommandMethod("SIOFORGECAD", "CURVETOPOLYGON", CommandFlags.UsePickSet)]
        public static void CURVETOPOLYGON()
        {
            Functions.CURVETOPOLYGON.Convert();
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
        //Force layer color to selected entities (changes "BYLAYER" to layer color)
        public static void FORCELAYERCOLORTOENTITY()
        {
            Functions.FORCELAYERCOLORTOENTITY.Convert();
        }

        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESCOLORTOGRAYSCALE", CommandFlags.UsePickSet)]
        //Force layer color to selected entities (changes "BYLAYER" to layer color)
        public static void SETSELECTEDENTITIESCOLORTOGRAYSCALE()
        {
            Functions.SETSELECTEDENTITIESCOLORTOGRAYSCALE.Convert();
        }

        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESBRIGHTNESS", CommandFlags.UsePickSet)]
        //Force layer color to selected entities (changes "BYLAYER" to layer color)
        public static void SETSELECTEDENTITIESBRIGHTNESS()
        {
            Functions.SETSELECTEDENTITIESBRIGHTNESS.Set();
        }

        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESCONTRAST", CommandFlags.UsePickSet)]
        //Force layer color to selected entities (changes "BYLAYER" to layer color)
        public static void SETSELECTEDENTITIESCONTRAST()
        {
            Functions.SETSELECTEDENTITIESCONTRAST.Set();
        }

        [CommandMethod("SIOFORGECAD", "OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE", CommandFlags.UsePickSet)]
        public static void OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE()
        {
            Functions.OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE.Convert();
        }

        [CommandMethod("SIOFORGECAD", "SSL", CommandFlags.Redraw)]
        public static void SSL()
        {
            Functions.SPECIALSSELECTIONS.AllOnSelectedEntitiesLayers();
        }

        [CommandMethod("SIOFORGECAD", "SSC", CommandFlags.Redraw)]
        public static void SSC()
        {
            Functions.SPECIALSSELECTIONS.AllWithSelectedEntitiesColors();
        }

        [CommandMethod("SIOFORGECAD", "SST", CommandFlags.Redraw)]
        public static void SST()
        {
            Functions.SPECIALSSELECTIONS.AllWithSelectedEntitiesTransparency();
        }

        [CommandMethod("SIOFORGECAD", "SSE", CommandFlags.Redraw)]
        public static void SSE()
        {
            Functions.SPECIALSSELECTIONS.AllWithSelectedEntitiesTypes();
        }

        [CommandMethod("SIOFORGECAD", "SSBLK", CommandFlags.Redraw)]
        //Select all block instance of selected blocks
        public static void SSBLK()
        {
            Functions.SPECIALSSELECTIONS.AllBlockWithSelectedBlocksNames();
        }

        [CommandMethod("SIOFORGECAD", "SSCL", CommandFlags.Transparent)]
        //Select all entities on current layer
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

        [CommandMethod("SIOFORGECAD", "RP2", CommandFlags.NoPaperSpace | CommandFlags.Interruptible)]
        public static void RP2()
        {
            Functions.RP2.RotateUCS();
        }

        [CommandMethod("SIOFORGECAD", "FRAMESELECTED", CommandFlags.Redraw)]
        public static void FRAMESELECTED()
        {
            Functions.FRAMESELECTED.FrameEntitiesToView();
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
            Functions.VEGBLOCCOPYGRIP.ToggleGrip();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOCLEGEND", CommandFlags.UsePickSet)]
        public static void VEGBLOCLEGEND()
        {
            Functions.VEGBLOCLEGEND.Add();
        }

        [CommandMethod("SIOFORGECAD", "VEGBLOCEXTRACT", CommandFlags.Redraw)]
        public static void VEGBLOCEXTRACT()
        {
            Functions.VEGBLOCEXTRACT.Extract();
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

        [CommandMethod("SIOFORGECAD", "READXDATA", CommandFlags.UsePickSet)]
        public static void READXDATA()
        {
            Functions.ENTITIESXDATA.Read();
        }

        [CommandMethod("SIOFORGECAD", "REMOVEENTITIESXDATA", CommandFlags.UsePickSet)]
        public static void REMOVEENTITIESXDATA()
        {
            Functions.ENTITIESXDATA.Remove();
        }

        [CommandMethod("SIOFORGECAD", "REMOVEALLENTITIESXDATA", CommandFlags.Modal)]
        public static void REMOVEALLENTITIESXDATA()
        {
            Functions.ENTITIESXDATA.RemoveAll();
        }

        [CommandMethod("SIOFORGECAD", "CUTHATCH", CommandFlags.UsePickSet)]
        //Cut a hatch in half or more (cut with a existing polyline)
        public static void CUTHATCH()
        {
            Functions.CUTHATCH.CutHoleHatch();
        }

        [CommandMethod("SIOFORGECAD", "MERGEHATCH", CommandFlags.UsePickSet)]
        //Merge two hatches together
        public static void MERGEHATCH()
        {
            Functions.MERGEHATCH.Merge();
        }

        [CommandMethod("SIOFORGECAD", "SCALEBY", CommandFlags.UsePickSet)]
        //Scale each of the selected objects relative to themselves
        public static void SCALEBY()
        {
            Functions.SCALEBY.ScaleBy();
        }

        [CommandMethod("SIOFORGECAD", "SCALEFIT", CommandFlags.UsePickSet)]
        //Scale each of the selected objects relative to themselves to fit a specified size
        public static void SCALEFIT()
        {
            Functions.SCALEFIT.ScaleFit();
        }

        [CommandMethod("SIOFORGECAD", "SCALERANDOM", CommandFlags.UsePickSet)]
        //Scale each of the selected objects relative to themselves to fit a random size between a range
        public static void SCALERANDOM()
        {
            Functions.SCALERANDOM.Scale();
        }

        [CommandMethod("SIOFORGECAD", "GETINNERCENTROID", CommandFlags.UsePickSet)]
        //Add a point to the within centroid of a polyline
        public static void GETINNERCENTROID()
        {
            Functions.GETINNERCENTROID.Get();
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

        [CommandMethod("SIOFORGECAD", "LINESAVERAGE", CommandFlags.UsePickSet)]
        public static void LINESAVERAGE()
        {
            Functions.LINESAVERAGE.Compute();
        }

        [CommandMethod("SIOFORGECAD", "VPL", CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VPLOCK", CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VPUNLOCK", CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VIEWPORTLOCK", CommandFlags.NoBlockEditor)]
        //ViewPorts lock / unlock all
        public static void VIEWPORTLOCK()
        {
            Functions.VIEWPORTLOCK.Menu();
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

        [CommandMethod("SIOFORGECAD", "COPYMODELTOPAPER", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor | CommandFlags.NoPerspective)]
        public static void COPYMODELTOPAPER()
        {
            Functions.COPYMODELTOPAPER.ChangeSpace();
        }

        [CommandMethod("SIOFORGECAD", "VPO", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VIEWPORTOUTLINE", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        //Outline (in model space) the selected viewport
        public static void VIEWPORTOUTLINE()
        {
            Functions.VIEWPORTOUTLINE.OutlineSelected();
        }

        [CommandMethod("SIOFORGECAD", "VPOALL", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VIEWPORTOUTLINEALL", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        //Outline (in model space) all viewport in the drawing
        public static void VIEWPORTOUTLINEALL()
        {
            Functions.VIEWPORTOUTLINE.OutlineAll(false);
        }
        [CommandMethod("SIOFORGECAD", "VPOSELECTEDLAYOUTTAB", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        //Outline (in model space) all viewport  in current layout tab
        public static void VPOSELECTEDLAYOUTTAB()
        {
            Functions.VIEWPORTOUTLINE.OutlineAll(true);
        }

        [CommandMethod("SIOFORGECAD", "DELETESUBGROUP", CommandFlags.Redraw)]
        //Delete groups inside of a larger groups
        public static void DELETESUBGROUP()
        {
            Functions.DELETESUBGROUP.Delete();
        }

        [CommandMethod("SIOFORGECAD", "LIMITNUMBERINSELECTION", CommandFlags.Redraw)]
        //Limit the number of selected entities.
        public static void LIMITNUMBERINSELECTION()
        {
            Functions.LIMITNUMBERINSELECTION.Limit();
        }

        [CommandMethod("ROTATEONSINGLEAXIS", CommandFlags.UsePickSet)]
        //Rotate selected entities along a single axis (X, Y, Z)
        public static void ROTATEONSINGLEAXIS()
        {
            Functions.ROTATEONSINGLEAXIS.Rotate();
        }

        [CommandMethod("SIOFORGECAD", "DRAWBOUNDINGBOX", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "DRAWEXTENDS", CommandFlags.UsePickSet)]
        //Draw the bounding box of selected entities
        public static void DRAWBOUNDINGBOX()
        {
            Functions.DRAWBOUNDINGBOX.Draw();
        }

        [CommandMethod("SIOFORGECAD", "DXFIMPORT", CommandFlags.UsePickSet)]
        //Allow user to import multiples DXF or DWG at once
        public static void DXFIMPORT()
        {
            Functions.DXFIMPORT.Import();
        }

        [CommandMethod("SIOFORGECAD", "RECREATEASSOCIATIVEHATCHBOUNDARY", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "HATCHRECREATEMISSINGBOUNDARIES", CommandFlags.UsePickSet)]
        //Recreate the polyline arround each selected hatch
        public static void HATCHRECREATEMISSINGBOUNDARIES()
        {
            Functions.HATCHRECREATEMISSINGBOUNDARIES.Recreate();
        }

        [CommandMethod("SIOFORGECAD", "HATCHSELECTWITHINVALIDAREA", CommandFlags.Redraw)]
        [CommandMethod("SIOFORGECAD", "FINDHATCHWITHOUTVALIDAREA", CommandFlags.Redraw)]
        public static void FINDHATCHWITHOUTVALIDAREA()
        {
            Functions.FINDHATCHWITHOUTVALIDAREA.Search();
        }

        [CommandMethod("SIOFORGECAD", "HATCHSELECTWITHOUTASSOCIATIVEBOUNDARY", CommandFlags.Redraw)]
        [CommandMethod("SIOFORGECAD", "FINDHATCHWITHOUTASSOCIATIVEBOUNDARY", CommandFlags.Redraw)]
        public static void FINDHATCHWITHOUTASSOCIATIVEBOUNDARY()
        {
            Functions.FINDHATCHWITHOUTASSOCIATIVEBOUNDARY.Search();
        }

        [CommandMethod("SIOFORGECAD", "HATCHSELECTASSOCIATIVEBOUNDARYNOTSAMELAYER", CommandFlags.Redraw)]
        [CommandMethod("SIOFORGECAD", "FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER", CommandFlags.Redraw)]
        public static void FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER()
        {
            Functions.FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER.Search();
        }

        [CommandMethod("SIOFORGECAD", "SMARTFLATTEN", CommandFlags.UsePickSet)]
        //Flatten Each Entity
        public static void SMARTFLATTEN()
        {
            Functions.SMARTFLATTEN.Flatten();
        }

        [CommandMethod("SIOFORGECAD", "SMARTFLATTENEVERYTHINGS", CommandFlags.UsePickSet)]
        //Flatten Each Entity
        public static void SMARTFLATTENEVERYTHINGS()
        {
            Functions.SMARTFLATTEN.FlattenAll();
        }

        [CommandMethod("SIOFORGECAD", "STRIPTEXTFORMATING", CommandFlags.UsePickSet)]
        //
        public static void STRIPTEXTFORMATING()
        {
            Functions.STRIPTEXTFORMATING.Strip();
        }

        [CommandMethod("SIOFORGECAD", "FIXDRAWING", CommandFlags.Modal)]
        //Fix possible issues in drawing
        public static void FIXDRAWING()
        {
            Functions.FIXDRAWING.Fix();
        }

        [CommandMethod("SIOFORGECAD", "PREVIEWPRINT", CommandFlags.Modal)]
        public static void PREVIEWPRINT()
        {
            var ed = Generic.GetEditor();
            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("Veuillez selectionner une option")
            {
                AppendKeywordsToMessage = true
            };

            const string ACTIVE = "Activé";
            const string DESACTIVE = "Désactiver";
            promptKeywordOptions.Keywords.Add(ACTIVE);
            promptKeywordOptions.Keywords.Add(DESACTIVE);
            var result = ed.GetKeywords(promptKeywordOptions);
            switch (result.StringResult)
            {
                case ACTIVE:
                    AcApp.SetSystemVariable("ROLLOVERTIPS", 0);
                    AcApp.SetSystemVariable("XDWGFADECTL", 0);
                    break;
                case DESACTIVE:
                    AcApp.SetSystemVariable("ROLLOVERTIPS", 0);
                    AcApp.SetSystemVariable("XDWGFADECTL", 50);
                    break;
            }
        }

        [CommandMethod("SIOFORGECAD", "WIPEOUTGRIP", CommandFlags.UsePickSet)]
        public static void WIPEOUTGRIP()
        {
            Functions.WIPEOUTGRIP.ToggleGrip();
        }

        [CommandMethod("SIOFORGECAD", "SAVEFILEATCLOSE", CommandFlags.Redraw)]
        public static void SAVEFILEATCLOSE()
        {
            Functions.SAVEFILEATCLOSE.Toggle();
        }

        [CommandMethod("DEBUG", "TEST", CommandFlags.Redraw)]
        public static void TEST()
        {
            var db = Generic.GetDatabase();
            var doc = Generic.GetDocument();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                //Functions.DRAWBOUNDINGBOX.DrawExplodedExtends();
                Autodesk.AutoCAD.ApplicationServices.InplaceTextEditor x = Autodesk.AutoCAD.ApplicationServices.InplaceTextEditor.Current;

                string fc2 = "%<\\AcVar Date \\f \"dd/MM/yyyy\">%";
                Field fld = new Field(fc2);
                fld.Evaluate();


                var settings = new InplaceTextEditorSettings();
                settings.Type = InplaceTextEditorSettings.EntityType.Default;
                settings.Flags = InplaceTextEditorSettings.EditFlags.SelectAll;
                settings.SimpleMText = true;
                var t = new MText();

                t.SetField(fld);
                t.AddToDrawing();
                
                //AcApp.Idle += AcApp_Idle;
                Task.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(3000);
                    }
                    catch (System.Exception ex)
                    {
                        // Log exception or inform user
                        Console.WriteLine(ex.Message);
                    }
                }).ContinueWith(g =>
                {

                    SetForegroundWindow(Application.MainWindow.Handle);
                    //start while send key, stop when event raised
                    SendKeys.SendWait("^h");
                    //AcApp.Idle -= AcApp_Idle;

                }, TaskScheduler.FromCurrentSynchronizationContext());
                AcApp.EnterModal += AcApp_EnterModal;
                AcApp.LeaveModal += AcApp_LeaveModal;
                InplaceTextEditor.Invoke(t, settings);
                AcApp.EnterModal -= AcApp_EnterModal;
                AcApp.LeaveModal -= AcApp_LeaveModal;
                
                Generic.WriteMessage("Done");
                acTrans.Commit();
            }
        }

        private static void AcApp_LeaveModal(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(3000);
                }
                catch (System.Exception ex)
                {
                    // Log exception or inform user
                    Console.WriteLine(ex.Message);
                }
            }).ContinueWith(g =>
            {
                var z = InplaceTextEditor.Current;
                SetForegroundWindow(Application.MainWindow.Handle);
                InplaceTextEditor.Current.Close(TextEditor.ExitStatus.ExitSave);

            }, TaskScheduler.FromCurrentSynchronizationContext());
            Thread.Sleep(300);
           
            Debug.WriteLine("AcApp_LeaveModal");
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);


        private static void AcApp_EnterModal(object sender, EventArgs e)
        {
            Debug.WriteLine("AcApp_EnterModal");
        }

        [CommandMethod("DEBUG", "TEST2", CommandFlags.Redraw)]
        public static void TEST2()
        {
            //https://www.keanw.com/2007/07/accessing-the-a.html
            //https://www.keanw.com/2007/06/embedding_field.html
            //https://www.cadforum.cz/en/qaID.asp?tip=6381
            //%<\AcExpr (%<\AcObjProp Object(%<\_ObjId 2621431877296>%).Area>%+10) \f "%lu6">%
            var ed = Generic.GetEditor();
            var res = ed.GetEntity("tte");
            if (res.Status == PromptStatus.OK)
            {
                var ent = res.ObjectId.GetNoTransactionDBObject();
                Generic.WriteMessage("Ent");
            }
        }

        [CommandMethod("DEBUG", "TESTMERGE", CommandFlags.UsePickSet)]
        public static void TESTMERGE()
        {
            Editor ed = Generic.GetEditor();
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

        [CommandMethod("DEBUG", "TRIANGLECC", CommandFlags.UsePickSet)]
        public static void TRIANGLECC()
        {
            DelaunayTriangulate.TriangulateCommand();
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
