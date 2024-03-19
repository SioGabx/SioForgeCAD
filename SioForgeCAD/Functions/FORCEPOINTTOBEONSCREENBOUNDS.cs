using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class FORCEPOINTTOBEONSCREENBOUNDS
    {
        //https://through-the-interface.typepad.com/through_the_interface/2014/01/forcing-autocad-object-snapping-using-net.html
        public static void Enable()
        {
            Generic.GetEditor().PointFilter += new PointFilterEventHandler(OnPointFilter);
        }

        public static void Disable()
        {
            Generic.GetEditor().PointFilter -= new PointFilterEventHandler(OnPointFilter);
        }


        static void OnPointFilter(object sender, PointFilterEventArgs e)
        {
            Editor ed = Generic.GetEditor();
            // Only if a command is active
            bool cmdActive =  (short)Application.GetSystemVariable("CMDACTIVE") == 1;
           
            if (e.Context.PointComputed && cmdActive)
            {
                var ViewBox = ed.GetCurrentViewBound();
                ViewBox.Expand(0.9);
                if (!ViewBox.IsPointIn(e.Context.ComputedPoint))
                {
                    e.Result.Retry = true;
                }
            }

        }
    }
}
