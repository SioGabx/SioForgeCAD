/// NB: this code requires a reference to AcLayer.dll
using Autodesk.AutoCAD.LayerManager;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
            private static LayerManagerControl LayerManager;

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
                var Data = CurrentSelectedRow.DataBoundItem;
                string LayerName = Data?.GetType()?.GetProperty("Name")?.GetValue(Data)?.ToString();
                if (LayerName is null) { return; }

                List<string> Names = new List<string>();
                foreach (var item in (LayerGrid.DataSource as IEnumerable))
                {
                    var itemName = item?.GetType()?.GetProperty("Name")?.GetValue(item)?.ToString();
                    if (!(itemName is null)) Names.Add(itemName);
                }
                string NewLayerName = LayerName + "_";

                for (int index = 1; Names.Contains(NewLayerName); index++)
                {
                    NewLayerName = LayerName + "_" + index;
                }

                MakeNewLayer(NewLayerName, false);
            }

            private static void MakeNewLayer(string layerName, bool bMakeFrozen)
            {
                Type typeLayerManagerControl = LayerManager.GetType();
                MethodInfo methodeCreate = typeLayerManagerControl.GetMethod("MakeNewLayer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodeCreate == null)
                {
                    return;
                }
                object[] arguments = new object[] { layerName, bMakeFrozen };
                methodeCreate.Invoke(LayerManager, arguments);
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
                                LayerManager = lmc;
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
