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
            Debug.WriteLine("Attach");
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

                        // Placement UI
                        Grid.SetRow(InjectedLayoutBar, Grid.GetRow(LayoutSwitchControl));
                        Grid.SetColumn(InjectedLayoutBar, Grid.GetColumn(LayoutSwitchControl));
                        Grid.SetRowSpan(InjectedLayoutBar, Grid.GetRowSpan(LayoutSwitchControl));
                        Grid.SetColumnSpan(InjectedLayoutBar, Grid.GetColumnSpan(LayoutSwitchControl));

                        var LayoutSwitchControlParent = LayoutSwitchControl.Parent as Grid;
                        LayoutSwitchControlParent.Children.Insert(0, InjectedLayoutBar);
                        LayoutSwitchControlParent.UpdateLayout();

                        InjectedLayoutBar.Visibility = Visibility.Visible;
                        LayoutSwitchControl.Visibility = Visibility.Collapsed;

                        LoadInjectedLayoutBarTabs();

                        // L'abonnement fonctionnera désormais même quand AutoCAD remplace la collection
                        TabsDataWrapper.SubscribeCollectionChanged((s, e) =>
                        {
                            LoadInjectedLayoutBarTabs();
                        });

                        InjectedLayoutBar.ActiveTabChanged += InjectedLayoutBar_ActiveTabChanged;
                    }
                }
            }
        }

        private static void InjectedLayoutBar_ActiveTabChanged(object sender, LayoutTab e)
        {
            Debug.WriteLine("InjectedLayoutBar_ActiveTabChanged");
            if (e.AutoCadData != null)
            {
                TabsDataWrapper.CurrentLayout = e.AutoCadData;
            }
        }


        public static void LoadInjectedLayoutBarTabs()
        {
            Debug.WriteLine("LoadInjectedLayoutBarTabs");
            InjectedLayoutBar.PinnedItems.Clear();
            InjectedLayoutBar.Items.Clear();

            foreach (var item in TabsDataWrapper.TabsCollection)
            {
                var dataWrapper = new LayoutDataWrapper(item);
                var tab = new LayoutTab
                {
                    Title = dataWrapper.Title,
                    AutoCadData = item,
                    IsPinned = dataWrapper.IsModel
                };

                if (dataWrapper.IsModel)
                {
                    InjectedLayoutBar.PinnedItems.Add(tab);
                }
                else
                {
                    InjectedLayoutBar.Items.Add(tab);
                }

                // Si c'est l'onglet actif
                if (dataWrapper.IsCurrent)
                {
                    InjectedLayoutBar.IsSyncing = true;
                    InjectedLayoutBar.SetCurrentTab(tab);
                    InjectedLayoutBar.IsSyncing = false;
                }

                dataWrapper.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(dataWrapper.IsCurrent) && dataWrapper.IsCurrent)
                    {
                        // On exécute ta logique de synchronisation
                        InjectedLayoutBar.IsSyncing = true;
                        InjectedLayoutBar.SetCurrentTab(tab);
                        InjectedLayoutBar.IsSyncing = false;
                    }
                    else if (e.PropertyName == nameof(dataWrapper.Title))
                    {
                        // Bonus : Si le nom de l'onglet change dans AutoCAD, on met à jour le LayoutTab
                        tab.Title = dataWrapper.Title;
                    }
                };
            }
        }

        /// <summary>
        /// Encapsule Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutTabsData via réflexion.
        /// Gère intelligemment le remplacement de la collection d'onglets.
        /// </summary>
        internal sealed class LayoutTabsDataWrapper
        {
            private const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            private readonly object _inner;
            private readonly Type _type;

            // Références pour éviter les fuites mémoires et permettre le réabonnement
            private object _currentCollection;
            private NotifyCollectionChangedEventHandler _externalHandler;

            public LayoutTabsDataWrapper(object layoutTabsData)
            {
                _inner = layoutTabsData ?? throw new ArgumentNullException(nameof(layoutTabsData));
                _type = _inner.GetType();

                // 1. S'abonner au changement de la propriété TabsCollection (DependencyProperty)
                var descriptor = DependencyPropertyDescriptor.FromName("TabsCollection", _type, _type);
                if (descriptor != null)
                {
                    descriptor.AddValueChanged(_inner, OnTabsCollectionReplaced);
                }

                _currentCollection = TabsCollection;
            }

            public IList TabsCollection => _type.GetProperty("TabsCollection", BF)?.GetValue(_inner) as IList;

            public object CurrentLayout
            {
                get => _type.GetProperty("CurrentLayout", BF)?.GetValue(_inner);
                set => _type.GetProperty("CurrentLayout", BF)?.SetValue(_inner, value);
            }

            public void SubscribeCollectionChanged(NotifyCollectionChangedEventHandler h)
            {
                _externalHandler = h;
                if (_currentCollection is INotifyCollectionChanged ncc)
                {
                    ncc.CollectionChanged += _externalHandler;
                }
            }

            private void OnTabsCollectionReplaced(object sender, EventArgs e)
            {
                // On se détache de l'ancienne collection morte
                if (_currentCollection is INotifyCollectionChanged oldNcc && _externalHandler != null)
                {
                    oldNcc.CollectionChanged -= _externalHandler;
                }

                // On récupère la nouvelle créée par AutoCAD
                _currentCollection = TabsCollection;

                // On s'attache à la nouvelle collection
                if (_currentCollection is INotifyCollectionChanged newNcc && _externalHandler != null)
                {
                    newNcc.CollectionChanged += _externalHandler;

                    // On force l'UI à se mettre à jour immédiatement car les pointeurs ont changé
                    _externalHandler.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        /// <summary>
        /// Encapsule Autodesk.AutoCAD.ViewModel.LayoutSwitch.LayoutData via réflexion.
        /// </summary>
        internal sealed class LayoutDataWrapper : INotifyPropertyChanged
        {
            private const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            private readonly Type _type;

            public event PropertyChangedEventHandler PropertyChanged;

            public LayoutDataWrapper(object layoutData)
            {
                Inner = layoutData;
                _type = Inner?.GetType();

                // Si l'objet implémente nativement INotifyPropertyChanged, on s'y abonne
                if (Inner is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += (s, e) => NotifierChangement(e.PropertyName);
                }
                else if (Inner is DependencyObject)
                {
                    // Sinon, on écoute spécifiquement les DependencyProperties d'AutoCAD
                    EcouterDependencyProperty(nameof(IsCurrent));
                    EcouterDependencyProperty(nameof(Title));
                    EcouterDependencyProperty(nameof(IsPinned));
                }
            }

            private void EcouterDependencyProperty(string propertyName)
            {
                var descriptor = DependencyPropertyDescriptor.FromName(propertyName, _type, _type);
                descriptor?.AddValueChanged(Inner, (s, e) => NotifierChangement(propertyName));
            }

            private void NotifierChangement(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public object Inner { get; }

            public string GlobalName =>
                (_type?.GetProperty(nameof(GlobalName), BF)?.GetValue(Inner) as string) ?? string.Empty;

            public bool IsPinned =>
                (_type?.GetProperty(nameof(IsPinned), BF)?.GetValue(Inner) as bool?) ?? false;

            public bool IsCurrent =>
                (_type?.GetProperty(nameof(IsCurrent), BF)?.GetValue(Inner) as bool?) ?? false;

            public string Title =>
                (_type?.GetProperty(nameof(Title), BF)?.GetValue(Inner) as string) ?? string.Empty;

            public bool IsModel =>
                GlobalName.Equals("Model", StringComparison.OrdinalIgnoreCase);
        }
    }
}