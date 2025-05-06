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
| `BLKSETTOBYBBLOCK` | Set all block entities to `BYBLOCK`. |
| `BLKSETTOBYBBLOCKIGNOREHATCH` | Set to `BYBLOCK`, ignore hatches. |
| `BLKSETTOBYBBLOCKHATCHSETTOWHITE` | Set to `BYBLOCK`, make hatches white. |
| `BLKINSEDIT` / `INSEDIT` | Move block basepoint without altering its position. |
| `BLKTOSTATICBLOCK` | Convert dynamic block to static block. |
| `BLKTOXREF` | Convert block to external reference (XREF). |
| `BLKADDENTITIES` | Add entities to an existing block or XREF. |
| `DRAWPERPENDICULARLINEFROMPOINT` | Draw a perpendicular line from a given point to a polyline. |
| `CIRCLETOPOLYLIGNE` | Convert a circle to a polyline. |
| `ELLIPSETOPOLYLIGNE` | Convert an ellipse to a polyline. |
| `POLYLINE3DTOPOLYLIGNE` | Convert 3D polyline to a 2D polyline. |
| `POLYLINE2DTOPOLYLIGNE` | Convert 2D polyline to a 3D polyline. |
| `CURVETOPOLYGON` | Convert somes curves into polygons polyline. |
| `DRAWCPTERRAIN` | Draw terrain from selected elevation points. |
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
| `TAREA` | Calculate area of selected objects. |
| `TLENS` | Compute total length of selected curves. |
| `TLENSBLKATTR` | Compute total length of selected block reference attributes (standards + dynamics). |
| `VEGBLOC` | Create VEGBLOC. |
| `VEGBLOCEDIT` | Edit VEGBLOC. |
| `VEGBLOCCOPYGRIP` | Enable VEGBLOC grip. |
| `VEGBLOCLEGEND` | Create VEGBLOC legend. |
| `VEGBLOCEXTRACT` | Extract VEGBLOC. |
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