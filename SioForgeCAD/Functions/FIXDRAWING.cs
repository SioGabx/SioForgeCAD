using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class FIXDRAWING
    {
        public static void Fix()
        {
            Database db = Generic.GetDatabase();
            Generic.Command("_AUDIT", "_YES"); //Evaluates the integrity of a drawing and corrects some errors.
            Generic.SetSystemVariable("UCSFOLLOW", 0); //Generates a plan view whenever you change from one UCS to another.  
            Generic.SetSystemVariable("UCSDETECT", 0);
            Generic.SetSystemVariable("ROLLOVERTIPS", 0);
            Generic.SetSystemVariable("QPMODE", -1);
            Generic.SetSystemVariable("CETRANSPARENCY", -1); //Sets the transparency level for new objects. -1 (ByLayer) 
            Generic.SetSystemVariable("FILLMODE", 1); //Specifies whether hatches and fills, 2D solids, and wide polylines are filled in. 
            Generic.SetSystemVariable("XCOMPAREENABLE", 0);
            Generic.SetSystemVariable("LINESMOOTHING", 1);
            Generic.SetSystemVariable("HPASSOC", 1); //Controls whether hatches and fills are associative. 1=Hatches and fills are associated with their defining boundary objects and are updated when the boundary objects change
            Generic.SetSystemVariable("DRAWORDERCTL", 3); //Controls the default display behavior of overlapping objects when they are created or edited. 
            Generic.SetSystemVariable("SORTENTS", 127);//Controls object sorting in support of draw order for several operations. 
            Generic.SetSystemVariable("WIPEOUTFRAME", 2); //Controls the display of frames for wipeout objects. 2 = Frames are displayed, but not plotted https://help.autodesk.com/view/ACD/2025/ENU/?guid=GUID-AF1A9E90-35FB-4A49-AA39-E3456B4F264D
            Generic.SetSystemVariable("LINEFADING", 1);
            Generic.SetSystemVariable("MEASUREMENT", 1);//Controls whether the current drawing uses imperial or metric hatch pattern and linetype files. 0 (imperial) or 1 (metric)
            Generic.SetSystemVariable("MEASUREINIT", 1); //Controls whether a drawing you start from scratch uses imperial or metric default settings.  0 (imperial) or 1 (metric)
            Generic.SetSystemVariable("INSUNITS", 6);//Specifies a drawing-units value for automatic scaling of blocks, images, or xrefs when inserted or attached to a drawing.  https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-A58A87BB-482B-4042-A00A-EEF55A2B4FD8
            Generic.SetSystemVariable("PICKADD", 2); //2 = Turns on PICKADD. Each object selected, either individually or by windowing, is added to the current selection set. If the SELECT command is used, keeps objects selected after the command ends.https://help.autodesk.com/view/ACD/2025/ENU/?guid=GUID-47C2A568-30EE-4F07-916F-884CDE25CBCA
            Generic.SetSystemVariable("PICKAUTO", 5);
            Generic.SetSystemVariable("HIDEXREFSCALES", 1);
            Generic.SetSystemVariable("INDEXCTL", 0);
            Generic.SetSystemVariable("LOCKUI", 0);
            Generic.SetSystemVariable("PSLTSCALE", 0); //Controls the linetype scaling of objects displayed in paper space viewports. 
            Generic.SetSystemVariable("LTSCALE", 1); //Sets the global linetype scale factor. Use LTSCALE to change the scale factor of linetypes for all objects in a drawing
            Generic.SetSystemVariable("CELTSCALE", 1); //Sets the current object linetype scaling factor. - Sets the linetype scaling for new objects relative to the LTSCALE command setting
            Generic.SetSystemVariable("MSLTSCALE", 1); //Scales linetypes displayed on the model tab by the annotation scale. 
            Generic.SetSystemVariable("HPSCALE", 1); //Scales linetypes displayed on the model tab by the annotation scale. 
            Generic.SetSystemVariable("HIDEXREFSCALES", 1); //Determines whether scales contained in external references display in the annotative scale list for the current drawing. //1 (Scales don't display)
            Generic.SetSystemVariable("VISRETAIN", 1); //Controls the properties of xref-dependent layers.  https://help.autodesk.com/view/ACDLT/2022/ENU/?guid=GUID-897B1672-4E09-42E0-B857-A9D1F96ED671
            Generic.SetSystemVariable("XREFNOTIFY", 2); //Controls the notification for updated or missing xrefs. https://help.autodesk.com/view/ACDLT/2022/ENU/?guid=GUID-D97BECAD-2380-4CA3-896C-A6896BE112F7
            Generic.SetSystemVariable("HPLAYER", "."); //Specifies a default layer for new hatches and fills in the current drawing.  https://help.autodesk.com/view/ACDLT/2023/ENU/?guid=GUID-8B64F625-7DD2-4264-8E59-3936F0992070
            Generic.SetSystemVariable("FILEDIA", 1); //display of file navigation dialog boxes. https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-99736BD7-E60E-4F4A-83F7-436B6F9C67A1
            //Generic.SetSystemVariable("DYNMODE", 0); //Turns Dynamic Input features on and off. : 0=All Dynamic Input features, including dynamic prompts, off 
            Generic.SetSystemVariable("DYNMODE", 3); //Turns Dynamic Input features on and off. 3 = Both pointer input and dimensional input on https://help.autodesk.com/view/ACD/2025/ENU/?guid=GUID-1ED138FF-2679-45C4-9C2C-332A821C9D12

            Generic.Command("_BASE", new Point3d(0, 0, 0)); //Sets the insertion base point for the current drawing.
            Generic.Command("_SNAP", "_OFF");
            Generic.Command("_INSUNITS", 6); //6 == Meters //Specifies a drawing-units value for automatic scaling of blocks, images, or xrefs when inserted or attached to a drawing. https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-A58A87BB-482B-4042-A00A-EEF55A2B4FD8
            Generic.Command("_-UNITS", 2, 4, 1, 4, 0, "_NO");
            Generic.Command("_INSBASE", Point3d.Origin);

            db.SetAnnotativeScale("1:1", 1, 1);
        }
    }
}
