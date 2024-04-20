using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class PICKSTYLETRAY
    {
        private static readonly Pane PickStylePane = new Pane();

        private static void SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name == "PICKSTYLE")
            {
                UpdateTray();
            }
        }

        public static void AddTray()
        {
            var StatusBar = Autodesk.AutoCAD.ApplicationServices.Application.StatusBar;
            if (StatusBar.Panes.IndexOf(PickStylePane) == -1)
            {
                PickStylePane.ToolTipText = "Select boudaries with hatch";
                PickStylePane.Style = PaneStyles.Normal;
                PickStylePane.Icon = GetImage(IsActive());
                PickStylePane.MouseDown += TrayClick;
                var LineWeightPane = StatusBar.GetDefaultPane(DefaultPane.LineWeight);
                var LineWeightPaneIndex = StatusBar.Panes.IndexOf(LineWeightPane);
                StatusBar.Panes.Insert(LineWeightPaneIndex + 1, PickStylePane);
                Autodesk.AutoCAD.ApplicationServices.Application.SystemVariableChanged += SystemVariableChanged;
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

        private static bool IsActive()
        {
            var Variable = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("PICKSTYLE");
            if (Variable is short Value)
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

        private static void SetPickStyle(bool Active)
        {
            if (Active)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("PICKSTYLE", 2);
            }
            else
            {
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("PICKSTYLE", 1);
            }
        }

        public static void UpdateTray()
        {
            PickStylePane.Icon.Dispose();
            PickStylePane.Icon = GetImage(IsActive());
        }

        private static void TrayClick(object sender, StatusBarMouseDownEventArgs e)
        {
            SetPickStyle(!IsActive());
            UpdateTray();
        }
    }
}
