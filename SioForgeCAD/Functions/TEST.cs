using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ViewModel.LayoutSwitch;
using Autodesk.AutoCAD.Windows;
using Autodesk.Private.Windows.ToolBars;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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


        private static LayoutBar InjectedLayoutBar;
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
                                                                           // System.Collections.ObjectModel.ObservableCollection<dynamic> TabsCollection = context.TabsCollection; //Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutData

                       // var host = new Grid();
                        if (InjectedLayoutBar is null && context != null)
                        {
                            InjectedLayoutBar = new LayoutBar();

                            // Copy Grid position
                            Grid.SetRow(InjectedLayoutBar, Grid.GetRow(LayoutSwitchControl));
                            Grid.SetColumn(InjectedLayoutBar, Grid.GetColumn(LayoutSwitchControl));

                            // Copy spans (important if used)
                            Grid.SetRowSpan(InjectedLayoutBar, Grid.GetRowSpan(LayoutSwitchControl));
                            Grid.SetColumnSpan(InjectedLayoutBar, Grid.GetColumnSpan(LayoutSwitchControl));


                            LayoutSwitchControl.Visibility = System.Windows.Visibility.Collapsed;

                            var LayoutSwitchControlParent = LayoutSwitchControl.Parent as Grid;
                            LayoutSwitchControlParent.Children.Insert(0, InjectedLayoutBar);

                            // Tab libre (ex: "Model")
                           // InjectedLayoutBar.AddFreeTab("Model", makeCurrent: true);
                           //InjectedLayoutBar.AddFreeTab("Test1", false);
                           //InjectedLayoutBar.AddFreeTab("Test2", false);
                           //InjectedLayoutBar.AddFreeTab("Test3", false);
                           //InjectedLayoutBar.AddFreeTab("Test4", false);
                           //InjectedLayoutBar.AddFreeTab("Test5", false);

                            // Groupe avec plusieurs feuilles
                            //var groupe = InjectedLayoutBar.AddGroup("Bâtiment A", "Plan RDC", "Plan R+1", "Coupe AA");
                        }
                        else
                        {
                            if (InjectedLayoutBar.Visibility == System.Windows.Visibility.Collapsed)
                            {
                                //InjectedLayoutBar.AddFreeTab("Long long name hello world", false);
                                InjectedLayoutBar.Visibility = System.Windows.Visibility.Visible;
                                LayoutSwitchControl.Visibility = System.Windows.Visibility.Collapsed;
                            }
                            else
                            {

                                InjectedLayoutBar.Visibility = System.Windows.Visibility.Collapsed;
                                LayoutSwitchControl.Visibility = System.Windows.Visibility.Visible;
                            }
                        }
                    }
               
                }
            }

        }


        /// <summary>
        /// Encapsule Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutTabsData via réflexion.
        /// Le type hérite de DependencyObject, ce qui rend dynamic inutilisable
        /// (le RuntimeBinder ne voit pas les propriétés CLR standard dans ce cas).
        /// </summary>
        internal sealed class LayoutTabsDataWrapper
        {
            private static readonly BindingFlags BF =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            private readonly object _inner;
            private readonly Type _type;

            public LayoutTabsDataWrapper(object layoutTabsData)
            {
                _inner = layoutTabsData ?? throw new ArgumentNullException(nameof(layoutTabsData));
                _type = _inner.GetType();
            }

            /// <summary>ObservableCollection&lt;LayoutData&gt;</summary>
            public IList TabsCollection =>
                _type.GetProperty("TabsCollection", BF)?.GetValue(_inner) as IList;

            /// <summary>La présentation courante (LayoutData).</summary>
            public object CurrentLayout
            {
                get => _type.GetProperty("CurrentLayout", BF)?.GetValue(_inner);
                set => _type.GetProperty("CurrentLayout", BF)?.SetValue(_inner, value);
            }

            public void SubscribeCollectionChanged(NotifyCollectionChangedEventHandler h)
            {
                if (TabsCollection is INotifyCollectionChanged ncc) ncc.CollectionChanged += h;
            }

            public void UnsubscribeCollectionChanged(NotifyCollectionChangedEventHandler h)
            {
                if (TabsCollection is INotifyCollectionChanged ncc) ncc.CollectionChanged -= h;
            }
        }

        /// <summary>
        /// Encapsule Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutData via réflexion.
        /// </summary>
        internal sealed class LayoutDataWrapper
        {
            private static readonly BindingFlags BF =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            private readonly object _inner;
            private readonly Type _type;

            public LayoutDataWrapper(object layoutData)
            {
                _inner = layoutData;
                _type = _inner?.GetType();
            }

            public object Inner => _inner;

            public string Name =>
                (_type?.GetProperty("Name", BF)?.GetValue(_inner) as string) ?? string.Empty;

            public bool IsModel =>
                Name.Equals("Model", StringComparison.OrdinalIgnoreCase);
        }




    }
}