using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ViewModel.LayoutSwitch;
using Autodesk.AutoCAD.Windows;
using Autodesk.Private.Windows.ToolBars;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    internal static class TEST
    {
        public static FrameworkElement ObtenirStatusBarContainer()
        {
            foreach (PresentationSource source in PresentationSource.CurrentSources)
            {
                if (source.RootVisual != null)
                {
                    if (source.RootVisual.GetType().FullName == "Autodesk.AutoCAD.StatusBar.StatusBarContainer")
                    {
                        return source.RootVisual as FrameworkElement;
                    }
                }
            }
            return null;
        }

        public static void Test()
        {
            if (ObtenirStatusBarContainer() is DependencyObject root)
            {
                foreach (var control in root.TrouverEnfantsVisuels<FrameworkElement>())
                {
                    if (control.GetType().FullName == "Autodesk.AutoCAD.UserControls.LayoutSwitchControl")
                    {
                        dynamic LayoutSwitchControl = control;

                        control.Visibility = System.Windows.Visibility.Visible;
                        List<dynamic> LayoutSwitchLayoutData = new List<dynamic>(); //Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutData
                        dynamic context = LayoutSwitchControl.DataContext; //Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutTabsData
                        System.Collections.ObjectModel.ObservableCollection<dynamic> TabsCollection = context.TabsCollection; //Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutData
                        //LayoutSwitchControl.Content = new Grid() ;


                        //foreach (var control2 in root.TrouverEnfantsVisuels<FrameworkElement>())
                        //{
                        //    if (control2 is System.Windows.Controls.TabControl TabControl)
                        //    {
                        //        TabControl.ContextMenu = new System.Windows.Controls.ContextMenu();
                        //        TabControl.ContextMenu.Items.Add(new System.Windows.Forms.MenuItem("g"));
                        //        Debug.WriteLine("s");
                        //        //TabControl
                        //    }
                        //}





                    }
                }
            }

        }

       
    }
}