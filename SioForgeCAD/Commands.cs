using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using SioForgeCAD.Commun.Mist.Helpers;
using SioForgeCAD.Functions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
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
            promptKeywordOptions.Keywords.Add("About");
            promptKeywordOptions.Keywords.Add("Settings");
            if (PluginRegister.IsAlreadyRegister())
            {
                promptKeywordOptions.Keywords.Add("Unregister");
            }
            else
            {
                promptKeywordOptions.Keywords.Add("Register");
            }

            promptKeywordOptions.Keywords.Add("LoadCUIX");

            var result = ed.GetKeywords(promptKeywordOptions);
            switch (result.StringResult)
            {
                case "About":
                    Assembly ExAssembly = Assembly.GetExecutingAssembly();

                    string[] About = new string[]
                    {
                        $"{Settings.CopyrightMessage}",
                        $"Build version : {ExAssembly.GetName().Version}",
                        $"Build date    : {ExAssembly.GetLinkerTime()}",
                        $"Plugin path   : \"{ExAssembly.Location}\""
                    };
                    Generic.WriteMessage("\n");
                    Generic.WriteMessage(string.Join("\n", About));
                    break;
                case "Settings":
                    Settings.CreateAllRegistryKeys();
                    Registries.OpenRegEditAtKey(Settings.RegistryPath);
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
                    cs.AddPermanentKeyboardShortcut("F4", "Cancel F4", "^P'_.osmode $M=$(if,$(and,$(getvar,osmode),16384),$(-,$(getvar,osmode),16384),$(+,$(getvar,osmode),16384))", "ID_Cancel_F4");
                    cs.AddPermanentKeyboardShortcut("CTRL+Q", "Toggle QPMODE", "'_setvar;pickstyle;$M=$(if,$(eq,$(getvar,pickstyle),2),1,2)", "Toggle_QPMODE");
                    cs.LoadCui();
                    Debug.WriteLine(System.IO.File.Exists(cs.CUIFileName));
                    break;
            }
        }

        //Compute an intermediate point between two elevation points.
        [CommandMethod("SIOFORGECAD", "CCI", CommandFlags.Modal)]
        public static void CCI() => Functions.CCI.Compute();

        //Compute slope value between two elevation points.
        [CommandMethod("SIOFORGECAD", "CCP", CommandFlags.Modal)]
        public static void CCP() => Functions.CCP.Compute();

        //Calculate a point from a known elevation point using a slope.
        [CommandMethod("SIOFORGECAD", "CCD", CommandFlags.Modal)]
        public static void CCD() => Functions.CCD.Compute();

        //Add or subtract an elevation value to/from a point.
        [CommandMethod("SIOFORGECAD", "CCA", CommandFlags.Modal)]
        public static void CCA() => Functions.CCA.Compute();

        [CommandMethod("SIOFORGECAD", "CCDIF", CommandFlags.Modal)]
        public static void CCDIF() => Functions.CCDIF.Compute();

        //Create a block from a text-based elevation value
        [CommandMethod("SIOFORGECAD", "CCFROMTEXT", CommandFlags.Modal)]
        public static void CCFROMTEXT() => Functions.CCFROMTEXT.CreateCotationBlocFromText();

        //Create a block from a point-based elevation value
        [CommandMethod("SIOFORGECAD", "CCFROMPOINT", CommandFlags.Redraw)]
        public static void CCFROMPOINT() => Functions.CCFROMPOINT.CreateCotationBlocFromDbPoint();

        //Move a XREF elevation point into the drawing.
        [CommandMethod("SIOFORGECAD", "CCXREF", CommandFlags.Redraw)]
        public static void CCXREF() => Functions.CCXREF.MoveCotationFromXrefToCurrentDrawing();

        //Allow user to rename a block
        [CommandMethod("SIOFORGECAD", "RENBLK", CommandFlags.Redraw)]
        public static void RENBLK() => Functions.RENBLK.RenameBloc();

        //Makes the block instance unique. If several of the selected blocks have the same name, they will then share a new common instance
        [CommandMethod("SIOFORGECAD", "BLKMAKEUNIQUE", CommandFlags.Redraw)]
       public static void BLKMAKEUNIQUE() => new Functions.BLKMAKEUNIQUE(true).MakeUniqueBlockReferences();

        //Makes the block instance unique. If several of the selected blocks have the same name, they will NOT share a new common instance
        [CommandMethod("SIOFORGECAD", "BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        public static void BLKMAKEUNIQUEEACH() => new Functions.BLKMAKEUNIQUE(false).MakeUniqueBlockReferences();

        //Convert all entity values in the block to BYBLOCK
        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCK", CommandFlags.Redraw)]
      public static void BLKSETTOBYBBLOCK() => Functions.BLKSETTOBYBBLOCK.ByBlock(Functions.BLKSETTOBYBBLOCK.HatchSupport.Include);

        //Convert all entity values in the block to BYBLOCK, ignoring HATCH
        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCKIGNOREHATCH", CommandFlags.Redraw)]
        public static void BLKSETTOBYBBLOCKIGNOREHATCH() => Functions.BLKSETTOBYBBLOCK.ByBlock(Functions.BLKSETTOBYBBLOCK.HatchSupport.Ignore);

        //Convert all entity values in the block to BYBLOCK, HATCH to white (rgb 255,255,255)
        [CommandMethod("SIOFORGECAD", "BLKSETTOBYBBLOCKHATCHSETTOWHITE", CommandFlags.Redraw)]
       public static void BLKSETTOBYBBLOCKHATCHSETTOWHITE() => Functions.BLKSETTOBYBBLOCK.ByBlock(Functions.BLKSETTOBYBBLOCK.HatchSupport.SetToWhite);

        //Allow user to redefine the basepoint of a block instance without moving it
        [CommandMethod("SIOFORGECAD", "BLKINSEDIT", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "INSEDIT", CommandFlags.UsePickSet)]
        public static void BLKINSEDIT() => Functions.BLKINSEDIT.MoveBasePoint();

        //Convert dynamic block to static block
        [CommandMethod("SIOFORGECAD", "BLKTOSTATICBLOCK", CommandFlags.UsePickSet)]
        public static void BLKTOSTATICBLOCK() => Functions.BLKTOSTATICBLOCK.Convert();

        //Create  block reference from selection
        [CommandMethod("SIOFORGECAD", "BLKCREATE", CommandFlags.Redraw)]
        public static void BLKCREATE() => Functions.BLKCREATE.Create();

        //Create anonymous block reference from selection
        [CommandMethod("SIOFORGECAD", "BLKCREATEANONYMOUS", CommandFlags.Redraw)]
        public static void BLKCREATEANONYMOUS() => Functions.BLKCREATEANONYMOUS.Create();

        //Convert a block to a XREF
        [CommandMethod("SIOFORGECAD", "BLKTOXREF", CommandFlags.UsePickSet)]
        public static void BLKTOXREF() => Functions.BLKTOXREF.Convert();

        [CommandMethod("SIOFORGECAD", "BLKADDENTITIES", CommandFlags.UsePickSet)]
        public static void BLKADDENTITIES() => Functions.BLKADDENTITIES.Add();

        [CommandMethod("SIOFORGECAD", "BLKREPLACEALL", CommandFlags.UsePickSet)]
        public static void BLKREPLACEALL() => Functions.BLKREPLACE.All();

        [CommandMethod("SIOFORGECAD", "BLKREPLACE", CommandFlags.UsePickSet)]
        public static void BLKREPLACE() => Functions.BLKREPLACE.Selected();

        [CommandMethod("SIOFORGECAD", "BLKSETDEFINITIONTOSCALEUNIFORM", CommandFlags.UsePickSet)]
        public static void BLKSETDEFINITIONTOSCALEUNIFORM() => Functions.BLKSETDEFINITIONTOSCALEUNIFORM.SetBlockScaleToUniform();

        [CommandMethod("SIOFORGECAD", "BLKSETDEFINITIONTOEXPLODABLE", CommandFlags.UsePickSet)]
        public static void BLKSETDEFINITIONTOEXPLODABLE() => Functions.BLKSETDEFINITIONTOEXPLODABLE.SetExplodable();

        [CommandMethod("SIOFORGECAD", "BLKAPPLYSCALE", CommandFlags.UsePickSet)]
        public static void BLKAPPLYSCALE() => Functions.BLKAPPLYSCALE.ApplyBlockScale();

        [CommandMethod("SIOFORGECAD", "BLKMANAGEPOSITION", CommandFlags.UsePickSet)]
        public static void BLKMANAGEPOSITION() => Functions.BLKMANAGEPOSITION.Menu();

        [CommandMethod("SIOFORGECAD", "DRAWPERPENDICULARLINEFROMPOINT", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void DRAWPERPENDICULARLINEFROMPOINT() => Functions.DRAWPERPENDICULARLINEFROMPOINT.DrawPerpendicularLineFromPoint();

        [CommandMethod("SIOFORGECAD", "CIRCLETOPOLYLIGNE", CommandFlags.Redraw)]
        public static void CIRCLETOPOLYLIGNE() => Functions.CIRCLETOPOLYLIGNE.ConvertCirclesToPolylines();
        [CommandMethod("SIOFORGECAD", "ELLIPSETOPOLYLIGNE", CommandFlags.Redraw)]
        public static void ELLIPSETOPOLYLIGNE() => Functions.ELLIPSETOPOLYLIGNE.ConvertEllipseToPolylines();
        [CommandMethod("SIOFORGECAD", "LINETOPOLYLIGNE", CommandFlags.Redraw)]
        public static void LINETOPOLYLIGNE() => Functions.LINETOPOLYLIGNE.ConvertLineToPolylines();

        [CommandMethod("SIOFORGECAD", "POLYLINE3DTOPOLYLIGNE", CommandFlags.Redraw)]
        public static void POLYLINE3DTOPOLYLIGNE() => Functions.POLYLINE3DTOPOLYLIGNE.ConvertPolyline3dToPolylines();

        [CommandMethod("SIOFORGECAD", "POLYLINE2DTOPOLYLIGNE", CommandFlags.Redraw)]
        public static void POLYLINE2DTOPOLYLIGNE() => Functions.POLYLINE2DTOPOLYLIGNE.ConvertPolyline2dToPolylines();

        [CommandMethod("SIOFORGECAD", "CURVETOPOLYGON", CommandFlags.UsePickSet)]
        public static void CURVETOPOLYGON() => Functions.CURVETOPOLYGON.Convert();

        [CommandMethod("SIOFORGECAD", "DRAWCPTERRAIN", CommandFlags.Redraw)]
        public static void DRAWCPTERRAIN() => new Functions.DRAWCPTERRAIN().DrawTerrainFromSelectedPoints();

        [CommandMethod("SIOFORGECAD", "DROPCPOBJECTTOTERRAIN", CommandFlags.UsePickSet)]
        public static void DROPCPOBJECTTOTERRAIN() => Functions.DROPCPOBJECTTOTERRAIN.Project();

        [CommandMethod("SIOFORGECAD", "DRAWCPORDERGRADIENT", CommandFlags.UsePickSet)]
        public static void DRAWCPORDERGRADIENT() => Functions.DRAWCPORDERGRADIENT.Compute();

        //Force layer color to selected entities (changes "BYLAYER" to layer color)
        [CommandMethod("SIOFORGECAD", "FORCELAYERCOLORTOENTITY", CommandFlags.Redraw)]
        public static void FORCELAYERCOLORTOENTITY() => Functions.FORCELAYERCOLORTOENTITY.Convert();

        //
        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESCOLORTOGRAYSCALE", CommandFlags.Redraw)]
        public static void SETSELECTEDENTITIESCOLORTOGRAYSCALE() => Functions.SETSELECTEDENTITIESCOLORTOGRAYSCALE.Set();

        //
        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESBRIGHTNESS", CommandFlags.Redraw)]
        public static void SETSELECTEDENTITIESBRIGHTNESS() => Functions.SETSELECTEDENTITIESBRIGHTNESS.Set();

        //
        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESSATURATION", CommandFlags.Redraw)]
        public static void SETSELECTEDENTITIESSATURATION() => Functions.SETSELECTEDENTITIESBRIGHTNESS.Set();

        //
        [CommandMethod("SIOFORGECAD", "SETSELECTEDENTITIESCONTRAST", CommandFlags.Redraw)]
        public static void SETSELECTEDENTITIESCONTRAST() => Functions.SETSELECTEDENTITIESCONTRAST.Set();

        [CommandMethod("SIOFORGECAD", "OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE", CommandFlags.UsePickSet)]
        public static void OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE() => Functions.OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE.Convert();

        [CommandMethod("SIOFORGECAD", "SSL", CommandFlags.Redraw)]
        public static void SSL() => Functions.SPECIALSSELECTIONS.AllOnSelectedEntitiesLayers();

        [CommandMethod("SIOFORGECAD", "SSC", CommandFlags.Redraw)]
        public static void SSC() => Functions.SPECIALSSELECTIONS.AllWithSelectedEntitiesColors();

        [CommandMethod("SIOFORGECAD", "SST", CommandFlags.Redraw)]
        public static void SST() => Functions.SPECIALSSELECTIONS.AllWithSelectedEntitiesTransparency();

        [CommandMethod("SIOFORGECAD", "SSE", CommandFlags.Redraw)]
        public static void SSE() => Functions.SPECIALSSELECTIONS.AllWithSelectedEntitiesTypes();

        //Select all block instance of selected blocks
        [CommandMethod("SIOFORGECAD", "SSBLK", CommandFlags.Redraw)]
        public static void SSBLK() => Functions.SPECIALSSELECTIONS.AllBlockWithSelectedBlocksNames();

        //Select all entities on current layer
        [CommandMethod("SIOFORGECAD", "SSCL", CommandFlags.Transparent)]
        public static void SSCL() => Functions.SPECIALSSELECTIONS.AllOnCurrentLayer();

        [CommandMethod("SIOFORGECAD", "SSALLINSIDE", CommandFlags.Redraw)]
        public static void SSALLINSIDE() => Functions.SPECIALSSELECTIONS.InsideCrossingPolyline();

        [CommandMethod("SIOFORGECAD", "SSALLSTRICTLYINSIDE", CommandFlags.Redraw)]
        public static void SSALLSTRICTLYINSIDE() => Functions.SPECIALSSELECTIONS.InsideStrictPolyline();

        [CommandMethod("SIOFORGECAD", "RRR", CommandFlags.UsePickSet)]
        public static void RRR() => Functions.RRR.Rotate();

        [CommandMethod("SIOFORGECAD", "RP2", CommandFlags.NoPaperSpace | CommandFlags.Interruptible)]
        public static void RP2() => Functions.RP2.RotateUCS();

        [CommandMethod("SIOFORGECAD", "FRAMESELECTED", CommandFlags.Redraw)]
        public static void FRAMESELECTED() => Functions.FRAMESELECTED.FrameEntitiesToView();

        [CommandMethod("SIOFORGECAD", "VOLUMESTOCKAGEEP", CommandFlags.Redraw)]
        public static void VOLUMESTOCKAGEEP() => Functions.VOLUMESTOCKAGEEP.Compute();

        [CommandMethod("SIOFORGECAD", "AREATOFIELD", CommandFlags.Redraw)]
        public static void AREATOFIELD() => Functions.AREATOFIELD.Compute();

        [CommandMethod("SIOFORGECAD", "TAREA", CommandFlags.Redraw)]
        public static void TAREA() => Functions.TAREA.Compute();

        [CommandMethod("SIOFORGECAD", "TLENS", CommandFlags.Redraw)]
        public static void TLENS() => Functions.TLENS.Compute();

        [CommandMethod("SIOFORGECAD", "TBLK", CommandFlags.Redraw)]
        public static void TBLK() => Functions.TBLK.Compute();

        [CommandMethod("SIOFORGECAD", "TBLKATTR", CommandFlags.Redraw)]
        public static void TBLKATTR() => Functions.TBLK.ComputeCumulativeAttributes();
        [CommandMethod("SIOFORGECAD", "TBLKATTRDETAILED", CommandFlags.Redraw)]
        public static void TBLKATTRDETAILED() => Functions.TBLK.ComputeDetailed();

        [CommandMethod("SIOFORGECAD", "VEGBLOC", CommandFlags.Modal)]
        public static void VEGBLOC() => Functions.VEGBLOC.Create();

        [CommandMethod("SIOFORGECAD", "VEGBLOCEDIT", CommandFlags.UsePickSet)]
        public static void VEGBLOCEDIT() => Functions.VEGBLOCEDIT.Edit();

        [CommandMethod("SIOFORGECAD", "VEGBLOCCOUNTFILL", CommandFlags.Redraw)]
        public static void VEGBLOCCOUNTFILL() => Functions.VEGBLOCCOUNTFILL.CountFill();

        [CommandMethod("SIOFORGECAD", "VEGBLOCCOPYGRIP", CommandFlags.UsePickSet)]
        public static void VEGBLOCCOPYGRIP() => Functions.VEGBLOCCOPYGRIP.ToggleGrip();

        [CommandMethod("SIOFORGECAD", "VEGBLOCLEGEND", CommandFlags.UsePickSet)]
        public static void VEGBLOCLEGEND() => Functions.VEGBLOCLEGEND.Add();

        [CommandMethod("SIOFORGECAD", "VEGBLOCLAYOUT", CommandFlags.UsePickSet)]
        public static void VEGBLOCLAYOUT() => Functions.VEGBLOCLAYOUT.Create();

        [CommandMethod("SIOFORGECAD", "VEGBLOCEXTRACT", CommandFlags.Redraw)]
        public static void VEGBLOCEXTRACT() => Functions.VEGBLOCEXTRACT.Extract();

        [CommandMethod("SIOFORGECAD", "VEGBLOCEXPORTTOILLUSTRATOR", CommandFlags.Redraw)]
        public static void VEGBLOCEXPORTTOILLUSTRATOR() => Functions.VEGBLOCEXPORTTOILLUSTRATOR.ExportToSvg();

        [CommandMethod("SIOFORGECAD", "BATTLEMENTS", CommandFlags.Modal)]
        public static void BATTLEMENTS() => Functions.BATTLEMENTS.Draw();

        [CommandMethod("SIOFORGECAD", "PERSPECTIVETRANSFORM", CommandFlags.Modal)]
        public static void PERSPECTIVETRANSFORM() => Functions.PERSPECTIVETRANSFORM.Create();

        [CommandMethod("SIOFORGECAD", "RANDOMPAVEMENT", CommandFlags.Modal)]
        public static void RANDOMPAVEMENT() => Functions.RANDOMPAVEMENT.Draw();

        [CommandMethod("SIOFORGECAD", "PURGEALL", CommandFlags.Modal | CommandFlags.NoBlockEditor)]
        public static void PURGEALL() => Functions.PURGEALL.Purge();

        [CommandMethod("SIOFORGECAD", "READXDATA", CommandFlags.UsePickSet)]
        public static void READXDATA() => Functions.ENTITIESXDATA.Read();

        [CommandMethod("SIOFORGECAD", "REMOVEENTITIESXDATA", CommandFlags.UsePickSet)]
        public static void REMOVEENTITIESXDATA() => Functions.ENTITIESXDATA.Remove();

        [CommandMethod("SIOFORGECAD", "REMOVEALLENTITIESXDATA", CommandFlags.Modal)]
        public static void REMOVEALLENTITIESXDATA() => Functions.ENTITIESXDATA.RemoveAll();

        //Cut a hatch in half or more (cut with a existing polyline)
        [CommandMethod("SIOFORGECAD", "CUTHATCH", CommandFlags.UsePickSet)]
        public static void CUTHATCH() => Functions.CUTHATCH.CutHoleHatch();

        //Merge two hatches together
        [CommandMethod("SIOFORGECAD", "MERGEHATCH", CommandFlags.UsePickSet)]
        public static void MERGEHATCH() => Functions.MERGEHATCH.Merge();

        //Scale each of the selected objects relative to themselves
        [CommandMethod("SIOFORGECAD", "SCALEBY", CommandFlags.UsePickSet)]
        public static void SCALEBY() => Functions.SCALEBY.ScaleBy();

        //Scale each of the selected objects relative to themselves to fit a specified size
        [CommandMethod("SIOFORGECAD", "SCALEFIT", CommandFlags.UsePickSet)]
        public static void SCALEFIT() => Functions.SCALEFIT.ScaleFit();

        //Scale each of the selected objects relative to themselves to fit a random size between a range
        [CommandMethod("SIOFORGECAD", "SCALERANDOM", CommandFlags.UsePickSet)]
        public static void SCALERANDOM() => Functions.SCALERANDOM.Scale();

        [CommandMethod("SIOFORGECAD", "GETINNERCENTROID", CommandFlags.UsePickSet)]
        //Add a point to the within centroid of a polyline
        public static void GETINNERCENTROID() => Functions.GETINNERCENTROID.Get();

        [CommandMethod("SIOFORGECAD", "MERGEPOLYLIGNES", CommandFlags.UsePickSet)]
        public static void MERGEPOLYLIGNES() => Functions.MERGEPOLYLIGNES.Merge();

        [CommandMethod("SIOFORGECAD", "SUBSTRACTPOLYLIGNES", CommandFlags.UsePickSet)]
        public static void SUBSTRACTPOLYLIGNES() => Functions.SUBSTRACTPOLYLIGNES.Substract();

        [CommandMethod("SIOFORGECAD", "POLYISCLOCKWISE", CommandFlags.UsePickSet)]
        public static void POLYISCLOCKWISE() => Functions.POLYISCLOCKWISE.Check();

        [CommandMethod("SIOFORGECAD", "ADDPOINTSATPOLYLIGNEVERTICES", CommandFlags.Redraw)]
        public static void ADDPOINTSATPOLYLIGNEVERTICES() => Functions.ADDPOINTSATPOLYLIGNEVERTICES.Execute();

        [CommandMethod("SIOFORGECAD", "VIEWGEOMETRYVERTEX", CommandFlags.Redraw)]
        //Overule to draw a circle at each vertex of the selected entities
        public static void VIEWGEOMETRYVERTEX() => Functions.VIEWGEOMETRYVERTEX.ToggleOverrule();

        [CommandMethod("SIOFORGECAD", "LINESAVERAGE", CommandFlags.UsePickSet)]
        public static void LINESAVERAGE() => Functions.LINESAVERAGE.Compute();

        [CommandMethod("SIOFORGECAD", "VPL", CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VPLOCK", CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VPUNLOCK", CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VIEWPORTLOCK", CommandFlags.NoBlockEditor)]
        //ViewPorts lock / unlock all
        public static void VIEWPORTLOCK() => Functions.VIEWPORTLOCK.Menu();

        [CommandMethod("SIOFORGECAD", "POLYCLEAN", CommandFlags.UsePickSet)]
        public static void POLYCLEAN() => Functions.POLYCLEAN.PolyClean();

        [CommandMethod("SIOFORGECAD", "POLYOPTIMIZE", CommandFlags.UsePickSet)]
        public static void POLYOPTIMIZE() => Functions.POLYOPTIMIZE.PolyOptimize();

        [CommandMethod("SIOFORGECAD", "PICKSTYLETRAY", CommandFlags.Transparent)]
        public static void PICKSTYLETRAY() => Functions.PICKSTYLETRAY.AddTray();

        [CommandMethod("SIOFORGECAD", "CONVERTIMAGETOOLE", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "EMBEDIMAGE", CommandFlags.UsePickSet)]
        public static void CONVERTIMAGETOOLE() => Functions.CONVERTIMAGETOOLE.RasterToOle();

        [CommandMethod("SIOFORGECAD", "COPYMODELTOPAPER", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor | CommandFlags.NoPerspective)]
        public static void COPYMODELTOPAPER() => Functions.COPYMODELTOPAPER.ChangeSpace();

        [CommandMethod("SIOFORGECAD", "COPYPAPERTOMODEL", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor | CommandFlags.NoPerspective)]
        public static void COPYPAPERTOMODEL() => Functions.COPYPAPERTOMODEL.ChangeSpace();

        //Outline (in model space) the selected viewport
        [CommandMethod("SIOFORGECAD", "VPO", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VIEWPORTOUTLINE", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        public static void VIEWPORTOUTLINE() => Functions.VIEWPORTOUTLINE.OutlineSelected();

        //Outline (in model space) all viewport in the drawing
        [CommandMethod("SIOFORGECAD", "VPOALL", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        [CommandMethod("SIOFORGECAD", "VIEWPORTOUTLINEALL", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        public static void VIEWPORTOUTLINEALL() => Functions.VIEWPORTOUTLINE.OutlineAll(false);


        //Outline (in model space) all viewport  in current layout tab
        [CommandMethod("SIOFORGECAD", "VPOSELECTEDLAYOUTTAB", CommandFlags.UsePickSet | CommandFlags.NoBlockEditor)]
        public static void VPOSELECTEDLAYOUTTAB() => Functions.VIEWPORTOUTLINE.OutlineAll(true);

        //Delete groups inside of a larger groups
        [CommandMethod("SIOFORGECAD", "DELETESUBGROUP", CommandFlags.Redraw)]
        public static void DELETESUBGROUP() => Functions.DELETESUBGROUP.Delete();

        //Limit the number of selected entities.
        [CommandMethod("SIOFORGECAD", "LIMITNUMBERINSELECTION", CommandFlags.Redraw)]
        public static void LIMITNUMBERINSELECTION() => Functions.LIMITNUMBERINSELECTION.Limit();

        //Rotate selected entities along a single axis (X, Y, Z)
        [CommandMethod("SIOFORGECAD", "ROTATEONSINGLEAXIS", CommandFlags.UsePickSet)]
        public static void ROTATEONSINGLEAXIS() => Functions.ROTATEONSINGLEAXIS.Rotate();

        //Draw the bounding box of selected entities
        [CommandMethod("SIOFORGECAD", "DRAWBOUNDINGBOX", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "DRAWEXTENDS", CommandFlags.UsePickSet)]
        public static void DRAWBOUNDINGBOX() => Functions.DRAWBOUNDINGBOX.Draw();

        //Allow user to import multiples DXF or DWG at once
        [CommandMethod("SIOFORGECAD", "DXFIMPORT", CommandFlags.UsePickSet)]
        public static void DXFIMPORT() => Functions.DXFIMPORT.Import();

        //Recreate the polyline arround each selected hatch
        [CommandMethod("SIOFORGECAD", "RECREATEASSOCIATIVEHATCHBOUNDARY", CommandFlags.UsePickSet)]
        [CommandMethod("SIOFORGECAD", "HATCHRECREATEMISSINGBOUNDARIES", CommandFlags.UsePickSet)]
        public static void HATCHRECREATEMISSINGBOUNDARIES() => Functions.HATCHRECREATEMISSINGBOUNDARIES.Recreate();

        [CommandMethod("SIOFORGECAD", "HATCHSELECTWITHINVALIDAREA", CommandFlags.Redraw)]
        [CommandMethod("SIOFORGECAD", "FINDHATCHWITHOUTVALIDAREA", CommandFlags.Redraw)]
        public static void FINDHATCHWITHOUTVALIDAREA() => Functions.FINDHATCHWITHOUTVALIDAREA.Search();

        [CommandMethod("SIOFORGECAD", "HATCHSELECTWITHOUTASSOCIATIVEBOUNDARY", CommandFlags.Redraw)]
        [CommandMethod("SIOFORGECAD", "FINDHATCHWITHOUTASSOCIATIVEBOUNDARY", CommandFlags.Redraw)]
        public static void FINDHATCHWITHOUTASSOCIATIVEBOUNDARY() => Functions.FINDHATCHWITHOUTASSOCIATIVEBOUNDARY.Search();

        [CommandMethod("SIOFORGECAD", "HATCHSELECTASSOCIATIVEBOUNDARYNOTSAMELAYER", CommandFlags.Redraw)]
        [CommandMethod("SIOFORGECAD", "FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER", CommandFlags.Redraw)]
        public static void FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER() => Functions.FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER.Search();

        //Flatten Each Entity
        [CommandMethod("SIOFORGECAD", "SMARTFLATTEN", CommandFlags.UsePickSet)]
        public static void SMARTFLATTEN() => Functions.SMARTFLATTEN.Flatten();

        //Flatten Each Entity
        [CommandMethod("SIOFORGECAD", "SMARTFLATTENEVERYTHINGS", CommandFlags.UsePickSet)]
        public static void SMARTFLATTENEVERYTHINGS() => Functions.SMARTFLATTEN.FlattenAll();

        //
        [CommandMethod("SIOFORGECAD", "STRIPTEXTFORMATING", CommandFlags.UsePickSet)]
        public static void STRIPTEXTFORMATING() => Functions.STRIPTEXTFORMATING.Strip();

        //Fix possible issues in drawing
        [CommandMethod("SIOFORGECAD", "FIXDRAWING", CommandFlags.Modal)]
        public static void FIXDRAWING() => Functions.FIXDRAWING.Fix();

        [CommandMethod("SIOFORGECAD", "WIPEOUTGRIP", CommandFlags.UsePickSet)]
        public static void WIPEOUTGRIP() => Functions.WIPEOUTGRIP.ToggleGrip();

        [CommandMethod("SIOFORGECAD", "SAVEFILEATCLOSE", CommandFlags.Redraw)]
        public static void SAVEFILEATCLOSE() => Functions.SAVEFILEATCLOSE.Toggle();

        [CommandMethod("SIOFORGECAD", "REMOVEALLPROXIES", CommandFlags.Redraw)]
        public static void REMOVEALLPROXIES() => Functions.REMOVEALLPROXIES.SearchAndEraseProxy();

        [CommandMethod("SIOFORGECAD", "SKETCHUPCREATEREGIONFROMPOLY", CommandFlags.Redraw)]
        public static void SKETCHUPCREATEREGIONFROMPOLY() => Functions.SKETCHUPCREATEREGIONFROMPOLY.GenerateRegionFromBoundaries();

        [CommandMethod("SIOFORGECAD", "SKETCHUPCREATETERRAINFROMPOINTS", CommandFlags.Redraw)]
        public static void SKETCHUPCREATETERRAINFROMPOINTS() => Functions.SKETCHUPCREATETERRAINFROMPOINTS.GeneratePointsFromAlt();

        [CommandMethod("SIOFORGECAD", "OFFSETMULTIPLE", CommandFlags.Redraw)]
        public static void OFFSETMULTIPLE() => Functions.OFFSETMULTIPLE.Execute();

        [CommandMethod("SIOFORGECAD", "EXTENDPOLY", CommandFlags.Redraw)]
        public static void EXTENDPOLY() => Functions.EXTENDPOLY.Execute();

        [CommandMethod("SIOFORGECAD", "COPYGEOMETRYTOCLIPBOARDFORINDESIGN", CommandFlags.Redraw)]
        public static void COPYGEOMETRYTOCLIPBOARDFORINDESIGN() => Functions.COPYGEOMETRYTOCLIPBOARDFORINDESIGN.Copy();

        [CommandMethod("SIOFORGECAD", "RENAMELAYOUT", CommandFlags.UsePickSet)]
        public static void RENAMELAYOUT() => Functions.RENAMELAYOUT.Rename();

        [CommandMethod("SIOFORGECAD", "RENAMELAYERS", CommandFlags.UsePickSet)]
        public static void RENAMELAYERS() => Functions.RENAMELAYERS.Rename();

        [CommandMethod("SIOFORGECAD", "MANAGEDRAWINGCUSTOMPROPERTIES", CommandFlags.UsePickSet)]
        public static void MANAGEDRAWINGCUSTOMPROPERTIES() => Functions.MANAGEDRAWINGCUSTOMPROPERTIES.Menu();

        [CommandMethod("SIOFORGECAD", "MANAGESCU", CommandFlags.UsePickSet)]
        public static void MANAGESCU() => Functions.MANAGESCU.Menu();

        [CommandMethod("SIOFORGECAD", "IMPORT3DGEOMETRYFROMOBJFILE", CommandFlags.UsePickSet)]
        public static void IMPORT3DGEOMETRYFROMOBJFILE() => Functions.IMPORT3DGEOMETRYFROMOBJFILE.Menu();

        [CommandMethod("SIOFORGECAD", "UPDATEXREFS", CommandFlags.UsePickSet)]
        public static void UPDATEXREFS() => Functions.UPDATEXREFS.Update();

        [CommandMethod("SIOFORGECAD", "ARRAYCOPY", CommandFlags.Redraw)]
        public static void ARRAYCOPY() => Functions.ARRAYCOPY.Execute();

        [CommandMethod("SIOFORGECAD", "POLYOUTLINE", CommandFlags.Redraw)]
        public static void POLYOUTLINE() => Functions.POLYOUTLINE.CreatePolyOutline();

        [CommandMethod("SIOFORGECAD", "LAYOUTFROMRECTANGLE", CommandFlags.Redraw)]
        public static void LAYOUTFROMRECTANGLE() => Functions.LAYOUTFROMRECTANGLE.Execute();

        [CommandMethod("SIOFORGECAD", "CUSTOMLAYOUTBAR", CommandFlags.Redraw)]
        public static void CUSTOMLAYOUTBAR() => Functions.CUSTOMLAYOUTBAR.Toggle();

        [CommandMethod("SIOFORGECAD", "DRAWPAPERFRAME", CommandFlags.Redraw)]
        public static void DRAWPAPERFRAME() => Functions.DRAWPAPERFRAME.Draw();

        [CommandMethod("SIOFORGECAD", "PUBLISHSELECTEDLAYOUTS", CommandFlags.Redraw)]
        public static void PUBLISHSELECTEDLAYOUTS() => Functions.PUBLISHSELECTEDLAYOUTS.ShowPublishDialog();

        [CommandMethod("SIOFORGECAD", "CREATECONTOURSLINESFROMPOINTS", CommandFlags.Redraw)]
        public static void CREATECONTOURSLINESFROMPOINTS()
        {
            Functions.CREATECONTOURSLINESFROMPOINTS.GeneratePointsFromAlt();
        }

        [CommandMethod("SIOFORGECAD", "TOGGLELAYERCOMPARE", CommandFlags.Redraw)]
        public static void TOGGLELAYERCOMPARE()
        {
            Functions.TOGGLELAYERCOMPARE.Execute();
        }

        [CommandMethod("SIOFORGECAD", "LASIMPORT", CommandFlags.Redraw)]
        public static void LASIMPORT()
        {
            Functions.LASIMPORT.ImportLAS();
        }



#if DEBUG
        //https://www.keanw.com/2007/04/rendering_autoc.html
        [CommandMethod("DEBUG", "FIELDEDITOR", CommandFlags.Redraw)]
        public static void FIELDEDITOR() => Functions.FIELDEDITOR.Test();


        [CommandMethod("DEBUG", "SMARTLEGEND", CommandFlags.Redraw)]
        public static void SMARTLEGEND()
        {
            Functions.SMARTLEGEND.Test();
        }

        [CommandMethod("DEBUG", "TRYCONVERTACADPROXYENTALTIMETRYTOBLK", CommandFlags.Redraw)]
        public static void TRYCONVERTACADPROXYENTALTIMETRYTOBLK()
        {
            Functions.TRYCONVERTACADPROXYENTALTIMETRYTOBLK.Convert();
        }

        [CommandMethod("DEBUG", "TEST", CommandFlags.Redraw)]
        public static void TEST()
        {
            //Functions.LAZPOINTSIMPORT.Import();
            //Functions.TEST.OffsetSegmentsCustom();
        }

        [CommandMethod("DEBUG", "TEST1", CommandFlags.Redraw)]
        public static void TEST1()
        {
            Functions.TESTEXPORTLAYOUT.ExportLayoutComplet();
        }

        [CommandMethod("DEBUG", "TEST2", CommandFlags.Redraw)]
        public static void TEST2()
        {
            var Paths = Directory.GetFiles(@"C:\Users\AMPLITUDE PAYSAGE\AppData\Roaming\Autodesk\AutoCAD 2021\R24.0\fra\Plotters");
            foreach (var item in Paths)
            {
                SioForgeCAD.Commun.Mist.Helpers.TextParsers.PC3.Files.Decode(item);
            }
        }

        [CommandMethod("DEBUG", "TEST3", CommandFlags.Redraw)]
        public static void TEST3()
        {


            DEBUG.EXPORTPOLYLINEDATA();

            //const string patha = "PATH.pc3";
            //SioForgeCAD.Commun.Mist.Helpers.TextParsers.PC3.Files.Decode(patha);
            //const string pathb = "PATH.txt";
            //SioForgeCAD.Commun.Mist.Helpers.TextParsers.PC3.Files.Encode(pathb, pathb + "_edited.pc3");
        }

        //CMLContentSearchPreviews.GetBlockTRThumbnail(); https://keanw.com/2013/11/generating-larger-preview-images-for-all-blocks-in-an-autocad-drawing-using-net.html
        //Autodesk.AutoCAD.Internal.Utils.GetBlockImage() https://drive-cad-with-code.blogspot.com/2020/12/obtaining-blocks-image.html


        //%USERPROFILE%\AppData\Roaming\Autodesk\AutoCAD 2021\R24.0\fra\Plotters\PMP Files

        //SioForgeCAD.Commun.Mist.Helpers.TextParsers.PC3.Files.Decode(@"%USERPROFILE%\Source\Repos\unzip_pc3\unzip_pc3\bin\Debug\DWG To PDF_AMPLITUDE.pmp");
        //SioForgeCAD.Commun.Mist.Helpers.TextParsers.PC3.Files.Encode(@"%USERPROFILE%\Source\Repos\unzip_pc3\unzip_pc3\bin\Debug\Converted\DWG To PDF_AMPLITUDE_modified.pmp.txt", @"C:\Users\AMPLITUDE PAYSAGE\Source\Repos\unzip_pc3\unzip_pc3\bin\Debug\Converted\DWG To PDF_AMPLITUDE.pmp");


        //TODO : BLKSETTOBYLAYER
        //TODO : Overrule for XCLIP
        //TOTO : Transform perspective
        //TODO : Edit field formula

        //https://aps.autodesk.com/blog/explode-dbtext-geometry-using-design-automation-api-6

        //https://keanw.com/2014/06/iterating-autocad-system-variables-using-net-part-2.html

        //https://github.com/huypham0808/Autopublish_AutoCAD_Plug-in/blob/f2f059f795bb26f7b654a05e64de68557f34f950/Command.cs#L165


        [CommandMethod("DEBUG", "GETOBJECTBYTESIZE", CommandFlags.UsePickSet)]
        public static void GETOBJECTBYTESIZE() => Functions.DEBUG.GETOBJECTBYTESIZE();


        [CommandMethod("DEBUG", "RANDOM_POINTS", CommandFlags.Transparent)]
        public static void DEBUG_RANDOM_POINTS() => Functions.DEBUG.DEBUG_RANDOM_POINTS();

        [CommandMethod("DEBUG", "DRAWRAINBOWLIGNES", CommandFlags.Transparent)]
        public static void DRAWRAINBOWLIGNES() => Functions.DEBUG.DRAWRAINBOWLIGNES();

#endif

    }
}
