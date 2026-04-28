# SioForgeCAD

**SioForgeCAD** is a powerful collection of custom AutoCAD commands designed to boost your productivity and streamline daily drafting tasks. It includes tools for geometry manipulation, block management, visual formatting, terrain modeling, hatch operations, and much more.

## 🛠️ Installation

1. Build the project as a `.dll`.
2. In add this to `"C:\Program Files\Autodesk\AutoCAD XXXX\acad.exe.config"`:   `<runtime><loadFromRemoteSources enabled="true"/></runtime>`
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
| `CCI` | Compute an intermediate point between two elevation points. |
| `CCP` | Compute slope value between two elevation points. |
| `CCD` | Calculate a point from a known elevation point using a slope. |
| `CCA` | Add or subtract an elevation value to/from a point. |
| `CCFROMTEXT` | Create a block from a text-based elevation value |
| `CCXREF` | Move a XREF elevation point into the drawing. |
| `RENBLK` | Rename selected block(s). |
| `BLKMAKEUNIQUE` | Make selected blocks share a new unique instance. |
| `BLKMAKEUNIQUEEACH` | Make each selected block a unique instance. |
| `BLKSETTOBYBBLOCK` | Set all block entities to 'BYBLOCK'. |
| `BLKSETTOBYBBLOCKIGNOREHATCH` | Set to 'BYBLOCK', ignore hatches. |
| `BLKSETTOBYBBLOCKHATCHSETTOWHITE` | Set to 'BYBLOCK', make hatches white. |
| `BLKINSEDIT` / `INSEDIT` | Move block basepoint without altering its position. |
| `BLKTOSTATICBLOCK` | Convert dynamic block to static block. |
| `BLKTOXREF` | Convert block to external reference (XREF). |
| `BLKADDENTITIES` | Add entities to an existing block or XREF. |
| `BLKCREATEANONYMOUS` | Creates an anonymous block (a block without a fixed name) from selected entities. |
| `BLKREPLACE` | Replaces selected instances of a block in the drawing with another block, keeping insertion points, scale, rotation. |
| `BLKREPLACEALL` | Replaces all instances of a block in the drawing with another block, keeping insertion points, scale, rotation. |
| `BLKSETDEFINITIONTOSCALEUNIFORM` | Set the block definition to use uniform scaling (same scale on X, Y, and Z). |
| `DRAWPERPENDICULARLINEFROMPOINT` | Draw a perpendicular line from a given point to a polyline. |
| `CIRCLETOPOLYLIGNE` | Convert a circle to a polyline. |
| `LINETOPOLYLIGNE` | Convert an line to a polyline. |
| `ELLIPSETOPOLYLIGNE` | Convert an ellipse to a polyline. |
| `POLYLINE3DTOPOLYLIGNE` | Convert 3D polyline to a 2D polyline. |
| `POLYLINE2DTOPOLYLIGNE` | Convert 2D polyline to a 3D polyline. |
| `CURVETOPOLYGON` | Convert somes curves into polygons polyline. |
| `COPYGEOMETRYTOCLIPBOARDFORINDESIGN` | Copies selected vector geometry into the clipboard in a format optimized for pasting into Adobe InDesign. |
| `DRAWCPTERRAIN` | Draw terrain from selected elevation points. |
| `DRAWCPORDERGRADIENT` | This command sorts the selected entities according to their projection along a user-defined direction vector, and updates their draw order accordingly. The result is a “gradient-like” visual stacking, where objects are displayed from back to front based on their projected distance.  |
| `DROPCPOBJECTTOTERRAIN` | Project object to terrain surface (polyline). |
| `FORCELAYERCOLORTOENTITY` | Force entity color to match its layer. |
| `SETSELECTEDENTITIESCOLORTOGRAYSCALE` | Convert selected entities to grayscale. |
| `SETSELECTEDENTITIESBRIGHTNESS` | Adjust brightness of selected entities. |
| `SETSELECTEDENTITIESCONTRAST` | Adjust contrast of selected entities. |
| `OVERRIDEXREFLAYERSCOLORSTOGRAYSCALE` | Override external reference layers color to grayscale. |
| `SSL` | Select all entities on the same layer. |
| `SSC` | Select all entities with the same color. |
| `SST` | Select all entities with the same transparency. |
| `SSE` | Select all entities with the same type. |
| `SSBLK` | Select all instances of selected blocks. |
| `SSCL` | Select all entities on the current layer. |
| `RRR` | Rotate entities around a base point. |
| `RP2` | Rotate view to current UCS system. |
| `FRAMESELECTED` | View : Frame selected entity. |
| `AREATOFIELD` | Converts a selected area (from a polyline, region, hatch, etc.) into a dynamic field that automatically updates when the geometry changes. |
| `TAREA` | Calculate area of selected objects. |
| `TLENS` | Compute total length of selected curves. |
| `TBLK` | Analyzes the selected block references in the drawing, counts how many instances exist for each block name, and exports the results to the clipboard in a tab-separated list. |
| `TBLKATTR` | Computes the cumulative sum of all selected block attribute and dynamic property values and reports totals per property. |
| `TBLKATTRDETAILED` | Analyzes the selected block references in the drawing, counts all attribute values for each block, and copies the detailed attribute summary to the clipboard |
| `VEGBLOC` | Create VEGBLOC. |
| `VEGBLOCEDIT` | Edit VEGBLOC. |
| `VEGBLOCCOPYGRIP` | Enable VEGBLOC grip. |
| `VEGBLOCLEGEND` | Create VEGBLOC legend. |
| `VEGBLOCEXTRACT` | Extract VEGBLOC. |
| `VEGBLOCLAYOUT` | Create layout from existing template. |
| `BATTLEMENTS` | Create battlement geometry. |
| `RANDOMPAVEMENT` | Create random pavement patterns. |
| `PURGEALL` | Purge all unused elements from the drawing. |
| `READXDATA` | Read XData from selected entities. |
| `REMOVEENTITIESXDATA` | Remove XData from selected entities. |
| `REMOVEALLENTITIESXDATA` | Remove XData from all entities in the drawing. |
| `CUTHATCH` | Cut a hatch using a polyline. |
| `MERGEHATCH` | Merge two hatches together. |
| `SCALEBY` | Scale each object relative to its own origin. |
| `SCALEFIT` | Scale objects to fit a target size. |
| `SCALERANDOM` | Randomly scale objects with a range. |
| `GETINNERCENTROID` | Add point to inner centroid of polyline. |
| `MERGEPOLYLIGNES` | Merge selected polylines. |
| `SUBSTRACTPOLYLIGNES` | Subtract selected polylines. |
| `OFFSETMULTIPLE` | Create multiple copies of a geometry at a specified offset distance, in one operation.. |
| `POLYISCLOCKWISE` | Check if polyline is clockwise. |
| `LINESAVERAGE` | Calculate the average polyline between two polyline. |
| `VPL` /`VPLOCK` / `VPUNLOCK` /`VIEWPORTLOCK` | Lock / Unlock viewports. |
| `POLYCLEAN` | Clean up any unnecessary vertices in the polyline. |
| `PICKSTYLETRAY` | Display the pickstyle tray for faster selection. |
| `CONVERTIMAGETOOLE` / `EMBEDIMAGE` | Convert images to OLE objects for embedding directly into the drawing file. |
| `COPYMODELTOPAPER` | Copy selected model space objects to paper space. |
| `VPO` / `VIEWPORTOUTLINE` | Outline (in model space) the selected viewport |
| `VPOALL` / `VIEWPORTOUTLINEALL` | Outline (in model space) all viewport in the drawing. |
| `VPOSELECTEDLAYOUTTAB` | Outline (in model space) all viewport  in current layout tab |
| `DELETESUBGROUP` | Delete groups inside of a larger groups |
| `LIMITNUMBERINSELECTION` | Limit the number of selected entities. |
| `ROTATEONSINGLEAXIS` | Rotate selected entities along a single axis (X, Y, Z) |
| `DRAWBOUNDINGBOX` / `DRAWEXTENDS` | Draw bounding box around selected objects. |
| `DXFIMPORT` | Import multiple DXF or DWG files at once. |
| `IMPORT3DGEOMETRYFROMOBJFILE` | Import data from a Wavefront .OBJ file and converts it into AutoCAD entities. It provides two different import modes: full 3D geometry or altitude point symbols (vertex altimetry). |
| `DIMDISASSOCIATE` | Remove the associative link between a dimension and the geometry it references, so that the dimension no longer updates automatically when the object changes or is deleted. |
| `RECREATEASSOCIATIVEHATCHBOUNDARY` / `HATCHRECREATEMISSINGBOUNDARIES` | Recreate polyline associative hatch boundaries. |
| `HATCHSELECTWITHINVALIDAREA` / `FINDHATCHWITHOUTVALIDAREA` | Find hatches with invalid area. |
| `HATCHSELECTWITHOUTASSOCIATIVEBOUNDARY` / `FINDHATCHWITHOUTASSOCIATIVEBOUNDARY` | Find hatches without associative boundaries. |
| `HATCHSELECTASSOCIATIVEBOUNDARYNOTSAMELAYER` / `FINDHATCHASSOCIATIVEBOUNDARYNOTSAMELAYER` | Find hatches with associative boundaries on different layers. |
| `SMARTFLATTEN` | Flatten selected entities. |
| `SMARTFLATTENEVERYTHINGS` | Flatten all entities in the drawing. |
| `STRIPTEXTFORMATING` | Strip formatting from selected text. |
| `FIXDRAWING` | Fix common issues in the drawing. |
| `PREVIEWPRINT` | Toggle print preview settings. |
| `WIPEOUTGRIP` | Toggle grip visibility on wipeouts. |
| `SAVEFILEATCLOSE` | Save file automatically upon closing. |
| `MANAGEDRAWINGCUSTOMPROPERTIES` | Manages the custom properties of the current drawing by allowing the user to copy or paste these properties through the clipboard. |
| `REGIONFORSKETCHUP` | This command converts closed AutoCAD entities into Regions and optionally exports them into a new drawing, specifically to prepare geometry for SketchUp import or other 3D applications that require clean, planar region outlines. |
| `REMOVEALLPROXIES` | Purges all proxy objects from the drawing |
| `RENAMELAYOUT` | Renames all paper space layouts in the current drawing by replacing a specific substring in their names with a new one provided by the user. |
| `BLKCREATE` | Create a new block from selected entities. |
| `BLKSETDEFINITIONTOEXPLODABLE` | <!-- Set block definition to allow exploding. --> Set block definition to allow exploding. |
| `BLKAPPLYSCALE` | Apply current scale to block internal entities and reset scale factor. |
| `SSALLINSIDE` | Select all entities entirely inside a boundary. |
| `SSALLSTRICTLYINSIDE` | Select all entities strictly inside a boundary. |
| `VOLUMESTOCKAGEEP` | Calculate rainwater storage volume (Volume Stockage EP). |
| `VEGBLOCCOUNTFILL` | Count and fill areas with vegetation blocks. |
| `VEGBLOCEXPORTTOILLUSTRATOR` | Export vegetation blocks to Adobe Illustrator. |
| `PERSPECTIVETRANSFORM` | BETA - Apply a perspective transformation to selected objects. |
| `ADDPOINTSATPOLYLIGNEVERTICES` |Add a point entity at each vertex of selected polylines. |
| `VIEWGEOMETRYVERTEX` | View the vertices of a all drawing geometry. |
| `COPYPAPERTOMODEL` |  Copy selected objects from paper space to model space. |
| `SKETCHUPCREATEREGIONFROMPOLY` | Create a SketchUp compatible region from a polyline. |
| `SKETCHUPCREATETERRAINFROMPOINTS` | Create a 3D terrain mesh from elevation points for SketchUp. |
| `EXTENDPOLY` | Extend a polyline at extremity by a specified distance . |
| `RENAMELAYERS` | Batch rename layers based on user criteria. |
| `MANAGESCU` | Manage User Coordinate Systems (UCS/SCU) : copy-paste between drawings. |
| `UPDATEXREFS` | BETA - Reload and update all external references in the drawing. |
| `ARRAYCOPY` | Copy entities in a customized array pattern. |
| `POLYOUTLINE` |Generate an outline bounding box or contour around a polyline with a width. |
| `LAYOUTFROMRECTANGLE` | Create a layout viewport matching a selected rectangle in model space. |
| `CUSTOMLAYOUTBAR` | Display a custom toolbar for layout management. |
| `DRAWPAPERFRAME` |Draw a visual frame representing the printed paper dimensions. |

| `FIELDEDITOR` | Debug only - Open an advanced editor for dynamic fields. |
| `TEST` | Debug only - Development/testing command 1. |
| `TEST2` | Debug only - Development/testing command 2. |
| `TEST3` | Debug only - Development/testing command 3. |
| `GETOBJECTBYTESIZE` | Debug only - Calculate and display the memory size (in bytes) of selected objects. |
| `TRIANGLECC` | Debug only - Create computation triangles between elevation points. |
| `RANDOM_POINTS` | Debug only - Generate random point entities within a specific boundary. |
| `DRAWRAINBOWLIGNES` | Debug only - Draw lines with an interpolated rainbow color gradient. |
