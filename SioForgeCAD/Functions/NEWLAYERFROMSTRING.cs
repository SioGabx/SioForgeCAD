using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.MacroRecorder;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;
using MenuItem = Autodesk.AutoCAD.LayerManager.MenuItem;

namespace SioForgeCAD.Functions
{
    public static class NEWLAYERLAYERMANAGERCONTEXTMENU
    {
        public static class ContextMenu
        {
            private static MenuItem AddNewLayer = null;
            private static DataGridView LayerGrid;

            public static void Attach()
            {
                SetLayerGrid();
                if (LayerGrid != null)
                {
                    if (AddNewLayer is null)
                    {
                        AddNewLayer = new MenuItem
                        {
                            Text = "Nouveau calque à partir du nom",
                        };
                        AddNewLayer.Click += OnExecute;
                    }

                    //Attach events
                    LayerGrid.ContextMenuChanged += ContextMenuChangedHandler;
                    LayerGrid.SelectionChanged += SelectionChangedHandler;
                }
            }

            private static void SelectionChangedHandler(object sender, EventArgs e)
            {
                AddNewLayer.Visible = LayerGrid.SelectedRows.Count <= 1;
            }

            private static void ContextMenuChangedHandler(object e, EventArgs s)
            {
                AddMenuItem();
                ((Control)e).ContextMenuChanged -= ContextMenuChangedHandler;
            }

            private static void OnExecute(object sender, EventArgs e)
            {
                DataGridViewSelectedRowCollection CurrentSelectedRows = LayerGrid.SelectedRows;
                var CurrentSelectedRow = CurrentSelectedRows[0];
                string LayerName = string.Empty;
                foreach (DataGridViewCell Cell in CurrentSelectedRow.Cells)
                {
                    if (Cell.OwningColumn.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        LayerName = Cell.Value.ToString();
                        break;
                    }
                }
                using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
                {
                    var originalLayer = Layers.GetLayerIdByName(LayerName).GetDBObject() as LayerTableRecord;
                    Layers.CreateLayer((LayerName + "Copy"), originalLayer.Color, originalLayer.LineWeight, originalLayer.Transparency, originalLayer.IsPlottable);
                        tr.Commit();
                }

            }

            private static void SetLayerGrid()
            {
                if (LayerGrid == null)
                {
                    FieldInfo field = typeof(PaletteHost).GetField("layerManager_", BindingFlags.Static | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        object rslt = field.GetValue(null);
                        if (rslt is LayerManagerControl lmc)
                        {
                            if (lmc.FindControlByTypeName("LayerGrid") is DataGridView LayerGridControl)//Autodesk.AutoCAD.LayerManager.LayerGrid;
                            {
                                LayerGrid = LayerGridControl;
                            }
                        }
                    }
                }
            }

            private static void AddMenuItem()
            {
                var Menus = LayerGrid?.ContextMenu?.MenuItems;
                if (Menus?.Contains(AddNewLayer) == false)
                {
                    int InsertIndex = 0;

                    foreach (System.Windows.Forms.MenuItem Menu in Menus)
                    {
                        InsertIndex++;
                        if (Menu.GetType().GetField("Update", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo fieldInfo)
                        {
                            object updateValue = fieldInfo.GetValue(Menu);

                            if (updateValue != null)
                            {
                                PropertyInfo methodProp = updateValue.GetType().GetProperty("Method");
                                MethodInfo method = (MethodInfo)methodProp.GetValue(updateValue);
                                if (method.Name == "ID_MENUITEM_LISTCTX_NEWLAYER_Update") //this is the best method for not using specific language
                                {
                                    break;
                                }
                            }
                        }
                    }

                    Menus?.Add(InsertIndex, AddNewLayer);
                }
            }

        }
    }
}
