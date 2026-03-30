/// NB: this code requires a reference to AcLayer.dll
using Autodesk.AutoCAD.LayerManager;
using SioForgeCAD.Commun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;
using MenuItem = Autodesk.AutoCAD.LayerManager.MenuItem;


namespace SioForgeCAD.Functions
{
    public static class LAYERMANAGERCTXNEWLAYERFROMSELECTED
    {
        public static class ContextMenu
        {
            private static MenuItem AddNewLayer = null;
            private static DataGridView LayerGrid;
            private static LayerManagerControl LayerManager;

            public static void Attach()
            {
                if (LayerGrid is null || LayerManager is null)
                {
                    var Palettes = Layers.GetLayerPaletteControls();
                    LayerGrid = Palettes.LayerGrid;
                    LayerManager = Palettes.LayerManager;
                }

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
                if (AddMenuItem()) //successfuly added
                {
                    Debug.WriteLine($"{nameof(LAYERMANAGERCTXNEWLAYERFROMSELECTED)} successfuly attached");
                    ((Control)e).ContextMenuChanged -= ContextMenuChangedHandler;
                }
            }

            /*      var db = Generic.GetDatabase();
                //Autodesk.AutoCAD.DatabaseServices.TransactionManager transactionManager = LayerManager.LayerViewManager.CurrentDatabase.TransactionManager;
                Autodesk.AutoCAD.DatabaseServices.TransactionManager transactionManager = db.TransactionManager;
                Editor editor = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;
                var PolySelection = editor.GetSelectionRedraw("Selectionnez une polyligne", true, false);
                if (PolySelection.Status != PromptStatus.OK) { return; }
                using (var tr = transactionManager.StartTransaction()) { 
                foreach (var polyObjId in PolySelection.Value.GetObjectIds())
                {
                    if (transactionManager.GetObject(polyObjId, OpenMode.ForWrite) is Entity poly)
                    {
                        poly.ColorIndex = 1;
                    }

                }
                    tr.Commit();
                }
                return;
*/
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


            private static bool AddMenuItem()
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
                                    Menus?.Add(InsertIndex, AddNewLayer);
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}
