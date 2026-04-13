using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SioForgeCAD.Functions
{
    public static class CUSTOMLAYOUTBAR
    {
        public static FrameworkElement ObtenirStatusBarContainer()
        {
            foreach (PresentationSource source in PresentationSource.CurrentSources)
            {
                if (source.RootVisual != null &&
                    source.RootVisual.GetType().FullName == "Autodesk.AutoCAD.StatusBar.StatusBarContainer")
                {
                    return source.RootVisual as FrameworkElement;
                }
            }
            return null;
        }



        private static LayoutBar InjectedLayoutBar;
        private static LayoutTabsDataWrapper TabsDataWrapper;
        public static void Attach()
        {
            List<FrameworkElement> LayoutSwitchControlList = new List<FrameworkElement>();
            if (ObtenirStatusBarContainer() is DependencyObject root)
            {
                var EnfantsVisuels = root.TrouverEnfantsVisuels<FrameworkElement>();
                Debug.WriteLine($"{EnfantsVisuels.Count()} Enfants visuels trouvés");
                foreach (var LayoutSwitchControl in EnfantsVisuels.Where(
                    ct => ct.GetType().FullName == "Autodesk.AutoCAD.UserControls.LayoutSwitchControl"))
                {
                    dynamic Context = LayoutSwitchControl.DataContext;
                    if (InjectedLayoutBar is null && Context != null)
                    {
                        InjectedLayoutBar = new LayoutBar
                        {
                            DataContext = Context
                        };

                        TabsDataWrapper = new LayoutTabsDataWrapper(Context);

                        //Set UI
                        Grid.SetRow(InjectedLayoutBar, Grid.GetRow(LayoutSwitchControl));
                        Grid.SetColumn(InjectedLayoutBar, Grid.GetColumn(LayoutSwitchControl));
                        Grid.SetRowSpan(InjectedLayoutBar, Grid.GetRowSpan(LayoutSwitchControl));
                        Grid.SetColumnSpan(InjectedLayoutBar, Grid.GetColumnSpan(LayoutSwitchControl));


                        var LayoutSwitchControlParent = LayoutSwitchControl.Parent as Grid;
                        LayoutSwitchControlParent.Children.Insert(0, InjectedLayoutBar);
                        LayoutSwitchControlParent.UpdateLayout();

                        InjectedLayoutBar.Visibility = System.Windows.Visibility.Visible;
                        LayoutSwitchControl.Visibility = System.Windows.Visibility.Collapsed;


                        LoadInjectedLayoutBarTabs();

                        TabsDataWrapper.SubscribeCollectionChanged((s, e) =>
                        {
                            LoadInjectedLayoutBarTabs();
                        });

                        if (Context is INotifyPropertyChanged inpc)
                        {
                            inpc.PropertyChanged += Inpc_PropertyChanged;
                        }

                        InjectedLayoutBar.ActiveTabChanged += InjectedLayoutBar_ActiveTabChanged;
                        LayoutSwitchControl.DataContextChanged += LayoutSwitchControl_DataContextChanged;
                    }
                }
            }
        }

        private static void LayoutSwitchControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //reload
        }

        private static void InjectedLayoutBar_ActiveTabChanged(object sender, LayoutTab e)
        {
            if (e.AutoCadData != null)
            {
                TabsDataWrapper.CurrentLayout = e.AutoCadData;
            }
        }

        private static void Inpc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //SYNCHRO : AUTOCAD -> LAYOUTBAR (Changement d'onglet actif depuis AutoCAD)
            if (e.PropertyName == "CurrentLayout")
            {
                var activeAutoCadData = TabsDataWrapper.CurrentLayout;
                var allTabs = InjectedLayoutBar.PinnedItems.Concat(InjectedLayoutBar.Items.OfType<LayoutTab>());

                var targetTab = allTabs.FirstOrDefault(t => t.AutoCadData == activeAutoCadData);
                if (targetTab != null)
                {
                    InjectedLayoutBar.IsSyncing = true; // On coupe la communication sortante

                    InjectedLayoutBar.SetCurrentTab(targetTab);
                    InjectedLayoutBar.IsSyncing = false; // On réactive
                }
            }
        }

        public static void LoadInjectedLayoutBarTabs()
        {
            InjectedLayoutBar.PinnedItems.Clear();
            InjectedLayoutBar.Items.Clear();

            foreach (var item in TabsDataWrapper.TabsCollection)
            {
                var dataWrapper = new LayoutDataWrapper(item);
                var tab = new LayoutTab
                {
                    Title = dataWrapper.Title,
                    AutoCadData = item, // On stocke le pointeur d'AutoCAD !
                    IsPinned = dataWrapper.IsModel
                };

                if (dataWrapper.IsModel)
                    InjectedLayoutBar.PinnedItems.Add(tab);
                else
                    InjectedLayoutBar.Items.Add(tab);

                // Si c'est l'onglet actif au démarrage
                if (item == TabsDataWrapper.CurrentLayout)
                {
                    InjectedLayoutBar.IsSyncing = true;
                    InjectedLayoutBar.SetCurrentTab(tab);
                    InjectedLayoutBar.IsSyncing = false;
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
            private const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            private readonly object _inner;
            private readonly Type _type;

            public LayoutTabsDataWrapper(object layoutTabsData)
            {
                _inner = layoutTabsData ?? throw new ArgumentNullException(nameof(layoutTabsData));
                _type = _inner.GetType();
            }

            public IList TabsCollection => _type.GetProperty("TabsCollection", BF)?.GetValue(_inner) as IList;

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
            private const BindingFlags BF =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private readonly Type _type;

            public LayoutDataWrapper(object layoutData)
            {
                Inner = layoutData;
                _type = Inner?.GetType();
            }

            public object Inner { get; }

            public string GlobalName =>
                (_type?.GetProperty("GlobalName", BF)?.GetValue(Inner) as string) ?? string.Empty;

            public string Title =>
                (_type?.GetProperty("Title", BF)?.GetValue(Inner) as string) ?? string.Empty;

            public bool IsModel =>
                GlobalName.Equals("Model", StringComparison.OrdinalIgnoreCase);
        }




    }
}