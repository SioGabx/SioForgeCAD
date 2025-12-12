# SioForgeCAD

**SioForgeCAD** is a powerful collection of custom AutoCAD commands designed to boost your productivity and streamline daily drafting tasks. It includes tools for geometry manipulation, block management, visual formatting, terrain modeling, hatch operations, and much more.

## 🛠️ Installation

1. Build the project as a `.dll`.
2. Load the extension into AutoCAD using the `NETLOAD` command.
3. Select the compiled `.dll` file.
4. Once loaded, use the command prefix `SIOFORGECAD` followed by one of the available commands listed below.

ONLY FOR AUTOCAD < 2025
-> https://gilecad.azurewebsites.net/Resources/Migration_NET_Core.pdf

---

## 🧭 Command Reference
<!-- 
Regex to get command name : \[(?:[^\]]*\s*,\s*)?"([^"]+)"
Regex to get README command name : /`([^`]+?)`/g
-->

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
| `SSOC` | Select entities within a crossing polyline. |
| `SSOF` | Select entities strictly inside a polyline. |
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