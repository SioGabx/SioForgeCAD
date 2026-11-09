# SioForgeCAD

**SioForgeCAD** is a powerful collection of custom AutoCAD commands designed to boost your productivity and streamline daily drafting tasks. It includes tools for geometry manipulation, block management, visual formatting, terrain modeling, hatch operations, and much more.

## 🛠️ Installation

1. Build the project as a `.dll`.
2. If you load this DLL from a network drive, add this to `"C:\Program Files\Autodesk\AutoCAD XXXX\acad.exe.config"` : `<runtime><loadFromRemoteSources enabled="true"/></runtime>`
3. Load the extension into AutoCAD using the `NETLOAD` command.
4. Select the compiled `.dll` file.
5. Once loaded, use the command prefix `SIOFORGECAD` followed by one of the available commands listed below.

ONLY FOR AUTOCAD < 2025
<!--
> https://gilecad.azurewebsites.net/Resources/Migration_NET_Core.pdf
-->
---


<!-- 
Regex to get command name      : \[(?:[^\]]*\s*,\s*)?"([^"]+)"   
Regex to get command name (V2) : \[(?:[^\]]*\s*,\s*)?"\K[^"]+(?=")
Regex to get README command name : /`([^`]+?)`/g
https://regex101.com/?regex=%5C%5B%28%3F%3A%5B%5E%5C%5D%5D*%5Cs*%2C%5Cs*%29%3F%22%5CK%5B%5E%22%5D%2B%28%3F%3D%22%29&testString=&flags=g&flavor=pcre2&delimiter=%2F


Compléter mon fichier MD, génére moi le fichier en me mettant uniquement le lien de téléchargement.

Toutes les commandes :  


Commandes existantes fichier MD (à completer par les commandes manquantes, propose des description que tu met en commentaire)


-->
## 🧭 Command Reference

| Command | Description |
|---|---|
| `SIOFORGECAD` | Display the SioForgeCAD main menu and execute the selected option. |
| `CCI` | Compute an intermediate point between two elevation points. |
| `CCP` | Compute slope value between two elevation points. |
| `CCD` | Calculate a point from a known elevation point using a slope. |
| `CCA` | Add or subtract an elevation value to/from a point. |
| `CCDIF` | Compute the elevation difference between two points. |
| `CCFROMTEXT` | Create a block from a text-based elevation value. |
| `CCFROMPOINT` | Create a block from a point-based elevation value. |
| `CCFROMBLOCKZ` | Create a block from a block Z elevation value. |
| `CCXREF` | Move a XREF elevation point into the drawing. |
| `RENBLK` | Allow user to rename a block. |
| `BLKMAKEUNIQUE` | Make selected blocks share a new common unique instance. |
| `BLKMAKEUNIQUEEACH` | Make each selected block a unique instance (not sharing a common definition). |
| `BLKSETTOBYBBLOCK` | Set all entity values in the block to 'BYBLOCK'. |
| `BLKSETTOBYBBLOCKIGNOREHATCH` | Set all entity values in the block to 'BYBLOCK', ignoring hatch entities. |
| `BLKSETTOBYBBLOCKHATCHSETTOWHITE` | Set all entity values in the block to 'BYBLOCK', setting hatches to white (RGB 255,255,255). |
| `BLKINSEDIT` / `INSEDIT` | Allow user to redefine the basepoint of a block instance without moving it. |
| `BLKTOSTATICBLOCK` | Convert dynamic block to static block. |
| `BLKCLIP` | Clip block references using a polyline. |
| `BLKCLIPFRAME` | Toggle the display of the block clipping frame. |
| `BLKCREATE` | Create block reference from selection. |
| `BLKCREATEANONYMOUS` | Create anonymous block reference from selection. |
| `BLKTOXREF` | Convert a block to an external reference (XREF). |
| `BLKADDENTITIES` | Add entities to a block definition. |
| `BLKREPLACEALL` | Replace all occurrences of one block with another block, keeping insertion points, scale, and rotation. |
| `BLKREPLACE` | Replace selected instances of a block with another block, keeping insertion points, scale, and rotation. |
| `BLKSETDEFINITIONTOSCALEUNIFORM` | Set the block definition to use uniform scaling (same scale on X, Y, and Z). |
| `BLKSETDEFINITIONTOEXPLODABLE` | Set block definition to allow exploding. |
| `BLKAPPLYSCALE` | Apply selected references' scale to their internal block definition and reset scale factor. |
| `BLKMANAGEPOSITION` | Open the block position management menu. |
| `BLKREGEN` / `BLKRECORDMODIFIEDGRAPHICS` | Regenerate the modified graphics of block references. |
| `DRAWPERPENDICULARLINEFROMPOINT` | Draw a perpendicular line from a given point to a polyline. |
| `CIRCLETOPOLYLIGNE` | Convert circles to polylines. |
| `ELLIPSETOPOLYLIGNE` | Convert ellipses to polylines. |
| `LINETOPOLYLIGNE` | Convert lines to polylines. |
| `POLYLINE3DTOPOLYLIGNE` | Convert 3D polylines to 2D polylines. |
| `POLYLINE2DTOPOLYLIGNE` | Convert 2D polylines to polylines. |
| `CURVETOPOLYGON` | Convert selected curves into polyline polygons. |
| `DRAWCPTERRAIN` | Draw terrain surface from selected elevation points. |
| `DROPCPOBJECTTOTERRAIN` | Project object onto terrain surface (polyline). |
| `DRAWCPORDERGRADIENT` | Sort selected entities by projection along a user-defined direction vector and update draw order to create a gradient stacking. |
| `FORCELAYERCOLORTOENTITY` | Force layer color to selected entities (change 'BYLAYER' to explicit color). |
| `SETSELECTEDENTITIESCOLORTOGRAYSCALE` | Convert color of selected entities to grayscale. |
| `SETSELECTEDENTITIESBRIGHTNESS` | Adjust brightness of selected entities. |
| `SETSELECTEDENTITIESSATURATION` | Adjust saturation of selected entities. |
| `SETSELECTEDENTITIESCONTRAST` | Adjust contrast of selected entities. |
| `OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE` | Override external reference layers color to grayscale. |
| `SSL` | Select all entities on the selected layers. |
| `SSC` | Select all entities with the same colors as selection. |
| `SST` | Select all entities with the same transparency as selection. |
| `SSE` | Select all entities with the same types as selection. |
| `SSBLK` | Select all block instances with the selected block names. |
| `SSCL` | Select all entities on the current layer. |
| `SSALLINSIDE` | Select entities inside or crossing a polyline boundary. |
| `SSALLSTRICTLYINSIDE` | Select entities strictly inside a polyline boundary. |
| `SSELEV` | Select entities by elevation value. |
| `RRR` | Rotate selected entities around a reference base point. |
| `RP2` | Orient user coordinate system (UCS) according to current view. |
| `FRAMESELECTED` | Frame selected entities in current view. |
| `VOLUMESTOCKAGEEP` | Calculate storage volume between surfaces or points (Rainwater Storage Volume). |
| `AREATOFIELD` | Convert selected area (polyline, region, hatch) into a dynamic field that updates automatically. |
| `TAREA` | Compute total area of selected objects. |
| `TLENS` | Compute total length of selected curves/objects. |
| `TBLK` | Count occurrences of selected block names and export tab-separated results to clipboard. |
| `TBLKATTR` | Compute cumulative sum of selected block attribute and dynamic property values. |
| `TBLKATTRDETAILED` | Analyze selected blocks, count attribute values per block, and copy detailed summary to clipboard. |
| `VEGBLOC` | Create vegetation block. |
| `VEGBLOCEDIT` | Edit selected vegetation block. |
| `VEGBLOCCOUNTFILL` | Count fills in vegetation blocks. |
| `VEGBLOCCOPYGRIP` | Toggle grip copying for vegetation blocks. |
| `VEGBLOCLEGEND` | Add legend for vegetation blocks. |
| `VEGBLOCLAYOUT` | Create layout from vegetation blocks. |
| `VEGBLOCEXTRACT` | Extract data from vegetation blocks. |
| `VEGBLOCEXPORTTOILLUSTRATOR` | Export vegetation blocks to SVG format for Adobe Illustrator. |
| `BATTLEMENTS` | Draw battlements or battlement-like elements. |
| `PERSPECTIVETRANSFORM` | Apply perspective transformation to selected objects (BETA). |
| `RANDOMPAVEMENT` | Draw pavement pattern with random layout. |
| `PURGEALL` | Purge all unused elements from drawing. |
| `READXDATA` | Read XData from selected entities. |
| `REMOVEENTITIESXDATA` | Remove XData from selected entities. |
| `REMOVEALLENTITIESXDATA` | Remove all XData from all entities in drawing. |
| `CUTHATCH` | Cut a hatch using an existing polyline boundary. |
| `MERGEHATCH` | Merge two hatches together. |
| `SCALEBY` | Scale each selected object relative to its own origin point. |
| `SCALEFIT` | Scale objects relative to themselves to fit a specified target size. |
| `SCALERANDOM` | Randomly scale objects relative to themselves within a range. |
| `GETINNERCENTROID` | Add a point to the inner centroid of a polyline. |
| `MERGEPOLYLIGNES` | Merge multiple polylines into one. |
| `SUBSTRACTPOLYLIGNES` | Subtract one polyline from another. |
| `POLYISCLOCKWISE` | Check if polyline orientation is clockwise. |
| `ADDPOINTSATPOLYLIGNEVERTICES` | Add point entities at each vertex of selected polylines. |
| `VIEWGEOMETRYVERTEX` | Toggle overrule to display visual indicators at each vertex of drawing geometry. |
| `LINESAVERAGE` | Compute average length of selected lines. |
| `VPL` / `VPLOCK` / `VPUNLOCK` / `VIEWPORTLOCK` | Display viewport lock/unlock menu. |
| `POLYCLEAN` | Clean unnecessary vertices in polylines. |
| `POLYOPTIMIZE` | Optimize selected polylines. |
| `PICKSTYLETRAY` | Add or update selection style settings tray panel. |
| `CONVERTIMAGETOOLE` / `EMBEDIMAGE` | Convert raster images to embedded OLE objects. |
| `COPYMODELTOPAPER` | Copy selected objects from model space to paper space. |
| `COPYPAPERTOMODEL` | Copy selected objects from paper space to model space. |
| `VPO` / `VIEWPORTOUTLINE` | Outline selected viewport in model space. |
| `VPOALL` / `VIEWPORTOUTLINEALL` | Outline all viewports in drawing in model space. |
| `VPOSELECTEDLAYOUTTAB` | Outline all viewports in current layout tab in model space. |
| `VPSETPLOTTRANSPARENCY` | Configure plot transparency settings for selected viewports. |
| `DELETESUBGROUP` | Delete subgroups inside larger groups. |
| `LIMITNUMBERINSELECTION` | Limit maximum number of selected entities. |
| `ROTATEONSINGLEAXIS` | Rotate selected entities along a single axis (X, Y, or Z). |
| `DRAWBOUNDINGBOX` / `DRAWEXTENDS` | Draw bounding box around selected entities. |
| `DXFIMPORT` | Import multiple DXF or DWG files at once. |
| `RECREATEASSOCIATIVEHATCHBOUNDARY` / `HATCHRECREATEMISSINGBOUNDARIES` | Recreate polyline associative hatch boundaries. |
| `HATCHSELECTWITHINVALIDAREA` / `FINDHATCHWITHOUTVALIDAREA` | Search for hatches with invalid surface areas. |
| `HATCHSELECTWITHOUTASSOCIATIVEBOUNDARY` / `FINDHATCHWITHOUTASSOCIATIVEBOUNDARY` | Find non-associative hatches. |
| `HATCHSELECTASSOCIATIVEBOUNDARYNOTSAMELAYER` / `FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER` | Find hatches with boundaries located on a different layer. |
| `SMARTFLATTEN` | Flatten Z coordinates for selected entities. |
| `SMARTFLATTENEVERYTHINGS` | Flatten Z coordinates for all entities in the drawing. |
| `STRIPTEXTFORMATING` | Remove text formatting from selected text objects. |
| `FIXDRAWING` | Repair common drawing issues. |
| `WIPEOUTGRIP` | Toggle grip visibility on wipeout entities. |
| `SAVEFILEATCLOSE` | Toggle automatic file saving upon closing. |
| `REMOVEALLPROXIES` | Search and purge all proxy entities from drawing. |
| `SKETCHUPCREATEREGIONFROMPOLY` | Create closed regions from polylines for SketchUp import. |
| `SKETCHUPCREATETERRAINFROMPOINTS` | Create 3D terrain mesh from elevation points for SketchUp. |
| `OFFSETMULTIPLE` | Create multiple parallel offsets of selected geometry in one operation. |
| `EXTENDPOLY` | Extend polyline extremities by a specified distance. |
| `COPYGEOMETRYTOCLIPBOARDFORINDESIGN` | Copy selected vector geometry optimized for pasting into Adobe InDesign. |
| `RENAMELAYOUT` | Rename layout tabs by replacing matching text strings. |
| `RENAMELAYERS` | Batch rename layers based on user-defined replacement rules. |
| `MANAGEDRAWINGCUSTOMPROPERTIES` | Manage drawing custom properties (copy/paste via clipboard). |
| `MANAGESCU` | Manage User Coordinate Systems (UCS/SCU) between drawings. |
| `IMPORT3DGEOMETRYFROMOBJFILE` | Import 3D geometry from Wavefront .OBJ file as 3D geometry or altimetry point symbols. |
| `UPDATEXREFS` | Reload and update all external references in drawing (BETA). |
| `ARRAYCOPY` | Copy objects into customized array patterns. |
| `POLYOUTLINE` | Generate outline boundary contour around a wide polyline. |
| `LAYOUTFROMRECTANGLE` | Create viewport matching a selected model space rectangle. |
| `CUSTOMLAYOUTBAR` | Toggle custom layout bar visibility. |
| `DRAWPAPERFRAME` | Draw printable area boundary frame in layout. |
| `PUBLISHSELECTEDLAYOUTS` | Show publish dialog for selected layouts. |
| `CREATECONTOURSLINESFROMPOINTS` | Generate contour lines from elevation points. |
| `CREATELABELSONCONTOURELEVATIONS` | Create elevation text labels along contour lines. |
| `TOGGLELAYERCOMPARE` | Toggle visual layer comparison. |
| `SELECTSPREADPOINTS` | Select spread points based on elevation/spatial criteria. |
| `SOLIDTOHATCH` | Convert 2D solids to hatch entities. |
| `LAZIMPORT` / `LASIMPORT` | Import LiDAR point clouds from .LAS or .LAZ files. |
| `DIVIDEPOLYLIGNINEQUALAREA` | Divide polyline area into equal polygon segments. |
| `EXECUTEONEACHOBJECTSINSELECTION` | Run a specified command sequentially on every object in selection. |
| `FIELDEDITOR` | Debug only - Open advanced editor for dynamic fields. |
| `SMARTLEGEND` | Debug only - Test smart legend generation. |
| `TRYCONVERTACADPROXYENTALTIMETRYTOBLK` | Debug only - Attempt to convert AutoCAD altimetry proxies to blocks. |
| `TEST` | Debug only - General development/test method. |
| `TEST1` | Debug only - Development test method 1. |
| `TEST2` | Debug only - Development test method 2 (3D/2D polyline trimming). |
| `TEST3` | Debug only - Development test method 3. |
| `GETOBJECTBYTESIZE` | Debug only - Display memory footprint (in bytes) of selected objects. |
| `RANDOM_POINTS` | Debug only - Generate random point entities for testing. |
| `DRAWRAINBOWLIGNES` | Debug only - Draw test lines with rainbow color gradient. |