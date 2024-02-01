using Autodesk.AutoCAD.Runtime;
using System;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

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
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                AcAp.Idle -= OnIdle;
                doc.Editor.WriteMessage("\nSioForgeCAD - Copyright © HOFFMANN François - 2024.\n");
            }
        }

        public void Terminate()
        { }
    }
}
