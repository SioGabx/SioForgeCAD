using Autodesk.AutoCAD.LayerManager;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Reflection;
// Note: this code requires a reference to AcLayer.dll

namespace SioForgeCAD.Functions
{
    internal static class TEST
    {
        private static MenuItem AddNewLayer;
        internal static void Testi()
        {
            FieldInfo field = typeof(PaletteHost).GetField("layerManager_", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                object rslt = field.GetValue(null);
                if (rslt is LayerManagerControl lmc)
                {
                    if (AddNewLayer is null)
                    {
                        AddNewLayer = new MenuItem
                        {
                            Text = "Test",
                        };
                        AddNewLayer.Click += AddNewLayer_Click;
                    }
                    System.Windows.Forms.Control LayerGridControl = lmc.FindControlByTypeName("LayerGrid"); //Autodesk.AutoCAD.LayerManager.LayerGrid;

                    if (LayerGridControl != null)
                    {
                        void ContextMenuChangedHandler(object e, EventArgs s)
                        {
                            if (LayerGridControl?.ContextMenu?.MenuItems.Contains(AddNewLayer) == false)
                            {
                                LayerGridControl.ContextMenu.MenuItems.Add(AddNewLayer);
                            }
                            ((System.Windows.Forms.Control)e).ContextMenuChanged -= ContextMenuChangedHandler;
                        }

                        LayerGridControl.ContextMenuChanged += ContextMenuChangedHandler;
                    }
                }

            }
        }

        private static void AddNewLayer_Click(object sender, System.EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}