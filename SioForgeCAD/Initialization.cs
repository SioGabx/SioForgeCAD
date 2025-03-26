using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using System;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD
{
    public class Initialization : IExtensionApplication
    {
        public void Initialize()
        {
            AcAp.Idle += OnIdle;
        }

        private void OnIdle(object sender, EventArgs e)
        {
            var doc = Generic.GetDocument();
            if (doc != null)
            {
                AcAp.Idle -= OnIdle;
                doc.Editor.WriteMessage($"\n{Generic.GetExtensionDLLName()} - Copyright © HOFFMANN François / SioGabx - {DateTime.Now.Year}.\n");
            }
            InitPlugin();
        }

        public static void InitPlugin()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Attach();
            Functions.ELLIPSETOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE2DTOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE3DTOPOLYLIGNE.ContextMenu.Attach();
            Functions.CONVERTIMAGETOOLE.ContextMenu.Attach();
            Functions.DIMDISASSOCIATE.ContextMenu.Attach();

            Functions.PICKSTYLETRAY.AddTray();

            Functions.WIPEOUTGRIP.EnableOverrule(true);
        }

        public void Terminate() { }
    }
}
