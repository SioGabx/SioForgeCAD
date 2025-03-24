using Autodesk.AutoCAD.ApplicationServices.Core;
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

            Application.SetSystemVariable("UCSFOLLOW", 0); //Generates a plan view whenever you change from one UCS to another.  
            Application.SetSystemVariable("UCSDETECT", 0);
            Application.SetSystemVariable("ROLLOVERTIPS", 0);
            Application.SetSystemVariable("QPMODE", -1);
            Application.SetSystemVariable("XCOMPAREENABLE", 0);
            Application.SetSystemVariable("LINESMOOTHING", 1);
            Application.SetSystemVariable("LINEFADING", 1);
            Application.SetSystemVariable("MEASUREMENT", 1);//Controls whether the current drawing uses imperial or metric hatch pattern and linetype files. 0 (imperial) or 1 (metric)
            Application.SetSystemVariable("MEASUREINIT", 1); //Controls whether a drawing you start from scratch uses imperial or metric default settings.  0 (imperial) or 1 (metric)
            Application.SetSystemVariable("INSUNITS", 6);//Specifies a drawing-units value for automatic scaling of blocks, images, or xrefs when inserted or attached to a drawing.  https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-A58A87BB-482B-4042-A00A-EEF55A2B4FD8
            Application.SetSystemVariable("PICKAUTO", 5);
            Application.SetSystemVariable("PSLTSCALE", 0); //Controls the linetype scaling of objects displayed in paper space viewports. 
            Application.SetSystemVariable("LTSCALE", 1); //Sets the global linetype scale factor. Use LTSCALE to change the scale factor of linetypes for all objects in a drawing
            Application.SetSystemVariable("CELTSCALE", 1); //Sets the current object linetype scaling factor. - Sets the linetype scaling for new objects relative to the LTSCALE command setting
            Application.SetSystemVariable("MSLTSCALE", 1); //Scales linetypes displayed on the model tab by the annotation scale. 
            Application.SetSystemVariable("HPSCALE", 1); //Scales linetypes displayed on the model tab by the annotation scale. 
            Application.SetSystemVariable("HIDEXREFSCALES", 1); //Determines whether scales contained in external references display in the annotative scale list for the current drawing. //1 (Scales don't display)
            Application.SetSystemVariable("VISRETAIN", 1); //Controls the properties of xref-dependent layers.  https://help.autodesk.com/view/ACDLT/2022/ENU/?guid=GUID-897B1672-4E09-42E0-B857-A9D1F96ED671
            Application.SetSystemVariable("XREFNOTIFY", 2); //Controls the notification for updated or missing xrefs. https://help.autodesk.com/view/ACDLT/2022/ENU/?guid=GUID-D97BECAD-2380-4CA3-896C-A6896BE112F7
            Application.SetSystemVariable("HPLAYER", "."); //Specifies a default layer for new hatches and fills in the current drawing.  https://help.autodesk.com/view/ACDLT/2023/ENU/?guid=GUID-8B64F625-7DD2-4264-8E59-3936F0992070
            Application.SetSystemVariable("FILEDIA", 1); //display of file navigation dialog boxes. https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-99736BD7-E60E-4F4A-83F7-436B6F9C67A1
            Generic.Command("_BASE", new Point3d(0, 0, 0)); //Sets the insertion base point for the current drawing.
            Generic.Command("_AUDIT", "_YES"); //Evaluates the integrity of a drawing and corrects some errors.
            Generic.Command("_SNAP", "_OFF");
            Generic.Command("_INSUNITS", 6); //6 == Meters //Specifies a drawing-units value for automatic scaling of blocks, images, or xrefs when inserted or attached to a drawing. https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-A58A87BB-482B-4042-A00A-EEF55A2B4FD8
            Generic.Command("_-UNITS", 2, 4, 1, 4, 0, "_NO");
            Functions.PURGEALL.Purge();
            db.SetAnnotativeScale("1:1", 1, 1);
        }
    }
}
