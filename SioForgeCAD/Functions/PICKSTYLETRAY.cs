using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using System.Drawing;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public static class PICKSTYLETRAY
    {
        private static readonly Pane PickStylePane = new Pane();
        private static readonly ContextMenu ContextMenu = new ContextMenu();

        private static void SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name == "PICKSTYLE")
            {
                UpdateTray();
            }
        }

        public static void AddTray()
        {
            var StatusBar = AcApp.StatusBar;
            if (StatusBar.Panes.IndexOf(PickStylePane) == -1)
            {
                PickStylePane.ToolTipText = "Switch PICKSTYLE between associative hatch selection.";
                PickStylePane.Style = PaneStyles.Normal;
                PickStylePane.Icon = GetImage(IsActive());
                PickStylePane.MouseDown += TrayClick;
                var LineWeightPane = StatusBar.GetDefaultPane(DefaultPane.LineWeight);
                var LineWeightPaneIndex = StatusBar.Panes.IndexOf(LineWeightPane);
                StatusBar.Panes.Insert(LineWeightPaneIndex + 1, PickStylePane);
                AcApp.SystemVariableChanged += SystemVariableChanged;
                AddTrayContextMenu();
            }
        }

        private static void AddTrayContextMenu()
        {
            var PickStyle0 = new System.Windows.Forms.MenuItem("0 - No group selection or associative hatch selection");
            var PickStyle1 = new System.Windows.Forms.MenuItem("1 - Group selection");
            var PickStyle2 = new System.Windows.Forms.MenuItem("2 - Associative hatch selection");
            var PickStyle3 = new System.Windows.Forms.MenuItem("3 - Group selection and associative hatch selection");

            PickStyle0.Tag = "0";
            PickStyle1.Tag = "1";
            PickStyle2.Tag = "2";
            PickStyle3.Tag = "3";

            PickStyle0.Click += (o, e) => SetPickStyle(0);
            PickStyle1.Click += (o, e) => SetPickStyle(1);
            PickStyle2.Click += (o, e) => SetPickStyle(2);
            PickStyle3.Click += (o, e) => SetPickStyle(3);

            ContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { PickStyle0, PickStyle1, PickStyle2, PickStyle3 });
            UpdateCheckedTrayContextMenuMenuItem(GetPickStyle());
        }

        private static void UpdateCheckedTrayContextMenuMenuItem(short PickStyleValue)
        {
            string PickStyleStringValue = PickStyleValue.ToString();
            foreach (System.Windows.Forms.MenuItem menuItem in ContextMenu.MenuItems)
            {
                menuItem.Checked = menuItem.Tag.ToString() == PickStyleStringValue;
            }
        }

        private static Icon GetImage(bool IsActive)
        {
            using (Bitmap bitmap = IsActive ? Properties.Resources.PICKSTYLETRAY_Icon_ON : Properties.Resources.PICKSTYLETRAY_Icon_OFF)
            {
                var iconHandle = bitmap.GetHicon();
                return Icon.FromHandle(iconHandle);
            }
        }

        private static short GetPickStyle()
        {
            return (short)AcApp.GetSystemVariable("PICKSTYLE");
        }

        private static bool IsActive()
        {
            if (GetPickStyle() is short Value)
            {
                /*
                    0 = No group selection or associative hatch selection
                    1 = Group selection
                    2 = Associative hatch selection
                    3 = Group selection and associative hatch selection
                */
                return Value >= 2;
            }
            return false;
        }

        private static void SetPickStyle(short Value)
        {
            AcApp.SetSystemVariable("PICKSTYLE", Value);
            Generic.WriteMessage($"PICKSTYLE is now set to {Value}");
        }

        private static void SetPickStyle(bool Active)
        {
            if (Active)
            {
                SetPickStyle(2);
            }
            else
            {
                SetPickStyle(1);
            }
        }

        public static void UpdateTray()
        {
            PickStylePane.Icon.Dispose();
            PickStylePane.Icon = GetImage(IsActive());
            UpdateCheckedTrayContextMenuMenuItem(GetPickStyle());
        }

        private static void TrayClick(object sender, StatusBarMouseDownEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SetPickStyle(!IsActive());
            }
            else
            {
                var loc = PickStylePane.PointToClient(Cursor.Position);
                PickStylePane.DisplayContextMenu(ContextMenu, loc);
            }
        }
    }
}
