using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using System;

namespace SioForgeCAD.Functions
{
    public static class DIMDISASSOCIATE
    {
        public static class ContextMenu
        {
            private static ContextMenuExtension cme;

            public static void Attach()
            {
                cme = new ContextMenuExtension();
                MenuItem mi = new MenuItem("Dissocier les cotes");
                mi.Click += OnExecute;
                cme.MenuItems.Add(mi);
                RXClass rxc = RXObject.GetClass(typeof(Dimension));
                if (rxc is null) { return; }
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = RXObject.GetClass(typeof(Dimension));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnExecute(object o, EventArgs e)
            {
                Generic.SendStringToExecute("_DIMDISASSOCIATE");
            }
        }
    }
}
