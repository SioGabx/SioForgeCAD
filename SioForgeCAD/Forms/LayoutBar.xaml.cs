using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static SioForgeCAD.Commun.Mist.User32PInvoke;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Forms
{
    public partial class LayoutBar : UserControl
    {
        public ObservableCollection<LayoutItem> PinnedItems { get; } = new ObservableCollection<LayoutItem>();
        public ObservableCollection<LayoutItem> Items { get; } = new ObservableCollection<LayoutItem>();

        // Événement déclenché quand l'utilisateur change d'onglet
        public event EventHandler<LayoutTab> ActiveTabChanged;

        // Empêche la boucle infinie (AutoCAD change la barre -> la barre prévient AutoCAD -> etc.)
        public bool IsSyncing { get; set; }

        private Window _ghostWindow;
        private readonly DispatcherTimer _autoScrollTimer;
        private LayoutItem _currentItem;
        private LayoutItem _lastSelectedItem; // Pour la sélection avec Shift
        private Point _dragStart;
        private bool _isDragging;
        private LayoutItem _dragSource;

        public LayoutBar()
        {
            InitializeComponent();
            LayoutBarRoot.DataContext = this;

            this.Loaded += LayoutBar_Loaded;
            this.Unloaded += LayoutBar_Unloaded;

            _autoScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15) // Rafraîchissement ultra fluide (~60 fps)
            };

        }

        private void TabScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            OverflowToggle.Visibility = TabScrollViewer.ScrollableWidth > 0 ?
                System.Windows.Visibility.Visible :
                System.Windows.Visibility.Collapsed;
        }

        private void LayoutBar_Loaded(object sender, RoutedEventArgs e)
        {
            // S'abonner aux événements quand le contrôle est affiché
            Application.DocumentManager.DocumentActivated += OnDocumentActivated;

            var lm = LayoutManager.Current;
            lm.LayoutSwitched += OnLayoutSwitched;
            lm.LayoutCreated += OnLayoutsChanged;
            lm.LayoutRemoved += OnLayoutsChanged;
            lm.LayoutRenamed += OnLayoutRenamed;

            ActiveTabChanged += LayoutBar_ActiveTabChanged;

            TabScrollViewer.ScrollChanged += TabScrollViewer_ScrollChanged;
            _autoScrollTimer.Tick += AutoScrollTimer_Tick;

            ReloadTabs(Application.DocumentManager.MdiActiveDocument);
        }

        private void LayoutBar_Unloaded(object sender, RoutedEventArgs e)
        {
            // CRUCIAL : Se désabonner pour éviter que le contrôle reste en mémoire
            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;

            var lm = LayoutManager.Current;
            lm.LayoutSwitched -= OnLayoutSwitched;
            lm.LayoutCreated -= OnLayoutsChanged;
            lm.LayoutRemoved -= OnLayoutsChanged;
            lm.LayoutRenamed -= OnLayoutRenamed;

            this.ActiveTabChanged -= LayoutBar_ActiveTabChanged;

            TabScrollViewer.ScrollChanged -= TabScrollViewer_ScrollChanged;
            _autoScrollTimer.Tick -= AutoScrollTimer_Tick;
        }

        // ====================================================================
        // GESTIONNAIRES D'ÉVÉNEMENTS (AUTOCAD -> WPF)
        // ====================================================================

        private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            ReloadTabs(e.Document);
        }

        private void OnLayoutSwitched(object sender, LayoutEventArgs e)
        {
            if (IsSyncing)
            {
                return;
            }

            IsSyncing = true;
            try
            {
                var tab = GetVisibleItemsList().FirstOrDefault(t => t.Title == e.Name);
                if (tab != null)
                {
                    SetCurrentTab(tab);
                }
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void OnLayoutsChanged(object sender, LayoutEventArgs e)
        {
            ReloadTabs(Application.DocumentManager.MdiActiveDocument);
        }
        private void OnLayoutRenamed(object sender, LayoutRenamedEventArgs e)
        {
            ReloadTabs(Application.DocumentManager.MdiActiveDocument);
        }

        // ====================================================================
        // GESTIONNAIRE D'ÉVÉNEMENT (WPF -> AUTOCAD)
        // ====================================================================

        private void LayoutBar_ActiveTabChanged(object sender, LayoutTab e)
        {
            if (IsSyncing)
            {
                return;
            }

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                if (LayoutManager.Current.CurrentLayout != e.Title)
                {
                    if (LayoutManager.Current.LayoutExists(e.Title))
                    {
                        LayoutManager.Current.CurrentLayout = e.Title;
                    }
                }
                tr.Commit();
            }
        }

        private bool LayoutBar_NewLayoutRequest(Dictionary<string, object> properties)
        {
            if (IsSyncing)
            {
                return false;
            }

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                try
                {
                    var Title = properties["Title"].ToString();
                    if (!LayoutManager.Current.LayoutExists(Title))
                    {
                        var id = LayoutManager.Current.CreateLayout(Title);
                        LayoutManager.Current.SetCurrentLayoutId(id);
                        return true;
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }
            return false;
        }

        private void SyncTabOrderToAutoCAD()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            IsSyncing = true;
            try
            {
                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    int currentOrder = 1;

                    foreach (var item in Items)
                    {
                        if (item is LayoutTab tab)
                        {
                            // Élément à la racine : pas de groupe
                            UpdateLayoutData(tr, tab, currentOrder, null, true);
                            currentOrder++;
                        }
                        else if (item is LayoutGroup group)
                        {
                            // Élément dans un groupe : on transmet l'état IsExpanded du groupe
                            foreach (var subTab in group.SubTabs)
                            {
                                UpdateLayoutData(tr, subTab, currentOrder, group.Title, group.IsExpanded);
                                currentOrder++;
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la synchro : {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private static void UpdateLayoutData(Transaction tr, LayoutTab tab, int order, string groupName, bool isExpanded)
        {
            if (tab.AutoCadData is ObjectId layoutId && !layoutId.IsNull)
            {
                var layout = tr.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                if (layout != null)
                {
                    if (layout.TabOrder != order)
                    {
                        layout.TabOrder = order;
                    }
                    layout.SetLayoutUIState(tr, groupName, isExpanded, tab.IsPinned); // <-- Utilisation de la nouvelle méthode
                }
            }
        }

        // ====================================================================
        // LOGIQUE DE CHARGEMENT DES DONNÉES
        // ====================================================================

        private void ReloadTabs(Document doc)
        {
            if (doc == null) return;

            Dispatcher.Invoke(() =>
            {
                Items.Clear();
                PinnedItems.Clear();

                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layoutDict = (DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId, OpenMode.ForRead);
                    var layouts = new List<Layout>();

                    foreach (DBDictionaryEntry entry in layoutDict)
                    {
                        var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                        layouts.Add(layout);
                    }

                    layouts = layouts.OrderBy(l => l.TabOrder).ToList();

                    // Dictionnaire pour suivre les groupes qu'on instancie à la volée
                    var activeGroups = new Dictionary<string, LayoutGroup>();

                    foreach (var layout in layouts)
                    {
                        var uiState = layout.GetLayoutUIState();
                        bool isPinnedUI = uiState?.IsPinned ?? false;

                        var newTab = new LayoutTab
                        {
                            Title = layout.LayoutName,
                            IsModel = layout.ModelType, // On retient s'il s'agit du Model
                            IsPinned = layout.ModelType || isPinnedUI, // Le model est TOUJOURS épinglé visuellement
                            AutoCadData = layout.ObjectId
                        };

                        if (newTab.IsPinned)
                        {
                            PinnedItems.Add(newTab);
                        }
                        else
                        {
                            string groupName = uiState?.GroupName;
                            bool isExpanded = uiState?.IsExpanded ?? true;

                            if (!string.IsNullOrEmpty(groupName))
                            {
                                if (!activeGroups.TryGetValue(groupName, out LayoutGroup value))
                                {
                                    var newGroup = new LayoutGroup
                                    {
                                        Title = groupName,
                                        IsExpanded = isExpanded
                                    };
                                    value = newGroup;
                                    activeGroups[groupName] = value;
                                    Items.Add(newGroup);
                                }
                                value.Add(newTab);
                            }
                            else
                            {
                                Items.Add(newTab);
                            }
                        }

                        if (layout.LayoutName == LayoutManager.Current.CurrentLayout)
                        {
                            IsSyncing = true;
                            SetCurrentTab(newTab);
                            IsSyncing = false;
                        }
                    }

                    tr.Commit();
                }
            });
        }

        // ====================================================================
        // CONTEXT MENU
        // ====================================================================
        // ====================================================================
        // GESTION DYNAMIQUE DU CONTEXTMENU
        // ====================================================================
        private void TabContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu && menu.PlacementTarget is FrameworkElement fe && fe.DataContext is LayoutTab tab)
            {
                foreach (var item in menu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        string Name = menuItem.Name;

                        if (Name == "TabMenuItem_Pin")
                        {
                            menuItem.Visibility = (!tab.IsPinned && !tab.IsModel) ?
                                System.Windows.Visibility.Visible : 
                                System.Windows.Visibility.Collapsed;
                        }

                        if (Name == "TabMenuItem_Unpin")
                        {
                            menuItem.Visibility = (tab.IsPinned && !tab.IsModel) ? 
                                System.Windows.Visibility.Visible : 
                                System.Windows.Visibility.Collapsed;
                        }

                        if (Name == "TabMenuItem_Rename" || Name == "TabMenuItem_Delete")
                            menuItem.IsEnabled = !tab.IsModel;
                        if (Name == "TabMenuItem_CreateGp")
                            menuItem.IsEnabled = !tab.IsModel && tab.IsInGroup;
                    }
                }
            }
        }

        // ====================================================================
        // ACTIONS EPINGLER / DESEPINGLER
        // ====================================================================
        private void ContextMenu_Epingler_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender).Where(t => !t.IsPinned && !t.IsModel).ToList();
            if (targetTabs.Count == 0) return;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in targetTabs)
                {
                    RemoveFromAll(tab);
                    tab.IsPinned = true;
                    PinnedItems.Add(tab);

                    if (tab.AutoCadData is ObjectId layoutId && !layoutId.IsNull)
                    {
                        var layout = tr.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                        // On met null pour le groupe car il passe à la racine des épinglés
                        layout?.SetLayoutUIState(tr, null, true, true);
                    }
                }
                tr.Commit();
            }
            ClearSelection();
            SyncTabOrderToAutoCAD();
        }

        private void ContextMenu_Desepingler_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender).Where(t => t.IsPinned && !t.IsModel).ToList();
            if (targetTabs.Count == 0) return;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in targetTabs)
                {
                    RemoveFromAll(tab);
                    tab.IsPinned = false;
                    Items.Add(tab); // Redescend à la racine par défaut

                    if (tab.AutoCadData is ObjectId layoutId && !layoutId.IsNull)
                    {
                        var layout = tr.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                        layout?.SetLayoutUIState(tr, null, true, false);
                    }
                }
                tr.Commit();
            }
            ClearSelection();
            SyncTabOrderToAutoCAD();
        }

        private void ContextMenu_Renommer_Click(object sender, RoutedEventArgs e)
        {
            var selectedTabs = GetVisibleItemsList().Where(t => t.IsSelected && t is LayoutTab).Cast<LayoutTab>().ToList();

            if (selectedTabs.Count > 1)
            {
                Debug.WriteLine("=== Renommage multiple ===");
                foreach (var tab in selectedTabs)
                {
                    Debug.WriteLine($"- {tab.Title} renommé en {tab.Title}-m");
                    tab.Title += "-m";
                }
            }
            else if (selectedTabs.Count == 1)
            {
                var tab = selectedTabs[0];
                Debug.WriteLine($"Renommer un seul onglet : {tab.Title}");
            }
        }

        private void ContextMenu_Tracer_Click(object sender, RoutedEventArgs e)
        {
            var selectedTabs = GetVisibleItemsList().Where(t => t.IsSelected && t is LayoutTab).ToList();
            Debug.WriteLine($"Tracer la sélection : {selectedTabs.Count} présentation(s).");
            // TODO: Appel de la commande AutoCAD Plot pour les LayoutData correspondants
        }

        private void ContextMenu_Publier_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender);
            Debug.WriteLine($"Publier la sélection : {targetTabs.Count} présentation(s).");
            // TODO: Appel de la commande AutoCAD Publish
        }

        private void ContextMenu_Supprimer_Click(object sender, RoutedEventArgs e)
        {
            // On récupère la bonne liste et on exclut les onglets épinglés
            var targetTabs = GetTargetedTabsForContextMenu(sender)
                .Where(t => !t.IsPinned)
                .ToList();

            if (targetTabs.Count == 0)
            {
                return;
            }

            Debug.WriteLine($"Tentative de suppression de {targetTabs.Count} présentation(s).");
            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in targetTabs)
                {
                    try
                    {
                        LayoutManager.Current.DeleteLayout(tab.Title);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"Impossible de supprimer la présentation '{tab.Title}' : {ex.Message}");
                    }
                }
                tr.Commit();
            }

            ClearSelection();
        }

        // ====================================================================
        // GESTION DES GROUPES
        // ====================================================================

        private void ContextMenu_CreerGroupe_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer les onglets sélectionnés (non épinglés)
            var selectedTabs = GetVisibleItemsList()
                .Where(t => t.IsSelected && !t.IsPinned && t is LayoutTab)
                .Cast<LayoutTab>()
                .ToList();

            if (selectedTabs.Count == 0) return;

            var newGroup = new LayoutGroup
            {
                Title = "Nouveau Groupe",
                IsExpanded = true
            };

            // Trouver l'index d'insertion (à la place du premier onglet sélectionné)
            int insertIndex = Items.IndexOf(selectedTabs.First());
            if (insertIndex == -1) insertIndex = Items.Count;

            Items.Insert(insertIndex, newGroup);

            // Déplacer les onglets dans le groupe
            foreach (var tab in selectedTabs)
            {
                RemoveFromAll(tab);
                newGroup.Add(tab);
            }

            ClearSelection();
            SyncTabOrderToAutoCAD();
        }


        private void ContextMenu_RenommerGroupe_Click(object sender, RoutedEventArgs e)
        {
            var targetGroup = GetTargetedGroupForContextMenu(sender);
            if (targetGroup != null)
            {
                // TODO: Implémenter l'UI de renommage (TextBox)
                Debug.WriteLine($"Renommer le groupe : {targetGroup.Title}");
            }
        }

        private void ContextMenu_TracerGroupe_Click(object sender, RoutedEventArgs e)
        {
            var targetGroup = GetTargetedGroupForContextMenu(sender);
            if (targetGroup != null)
            {
                Debug.WriteLine($"Tracer le groupe : {targetGroup.SubTabs.Count} présentation(s).");
                // TODO: Appel de la commande AutoCAD Plot pour les LayoutData du groupe
            }
        }

        private void ContextMenu_PublierGroupe_Click(object sender, RoutedEventArgs e)
        {
            var targetGroup = GetTargetedGroupForContextMenu(sender);
            if (targetGroup != null)
            {
                Debug.WriteLine($"Publier le groupe : {targetGroup.SubTabs.Count} présentation(s).");
                // TODO: Appel de la commande AutoCAD Publish pour le groupe
            }
        }

        private void ContextMenu_SupprimerGroupeSeul_Click(object sender, RoutedEventArgs e)
        {
            var targetGroup = GetTargetedGroupForContextMenu(sender);
            if (targetGroup == null) return;

            int groupIndex = Items.IndexOf(targetGroup);
            if (groupIndex == -1) return;

            // On extrait la liste avant de modifier la collection
            var tabsToExtract = targetGroup.SubTabs.ToList();

            foreach (var tab in tabsToExtract)
            {
                targetGroup.Remove(tab);
                Items.Insert(groupIndex, tab); // Replace au niveau racine
                groupIndex++; // Maintient l'ordre
            }

            Items.Remove(targetGroup);
            SyncTabOrderToAutoCAD();
        }

        private void ContextMenu_SupprimerGroupeEtContenu_Click(object sender, RoutedEventArgs e)
        {
            var targetGroup = GetTargetedGroupForContextMenu(sender);
            if (targetGroup == null) return;

            var tabsToDelete = targetGroup.SubTabs.ToList();
            if (tabsToDelete.Count == 0)
            {
                Items.Remove(targetGroup);
                return;
            }

            Debug.WriteLine($"Suppression du groupe et de {tabsToDelete.Count} présentation(s).");

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in tabsToDelete)
                {
                    try
                    {
                        LayoutManager.Current.DeleteLayout(tab.Title);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"Impossible de supprimer la présentation '{tab.Title}' : {ex.Message}");
                    }
                }
                tr.Commit();
            }

            Items.Remove(targetGroup);
            ClearSelection();
            // Inutile d'appeler SyncTabOrderToAutoCAD() ici car les événements AutoCAD de suppression (OnLayoutsChanged) vont recharger l'UI.
        }

        private static LayoutGroup GetTargetedGroupForContextMenu(object sender)
        {
            if (!(sender is MenuItem menuItem)) return null;

            // Remonter pour trouver le ContextMenu principal (utile si on est dans un sous-menu)
            var contextMenu = menuItem.Parent as ContextMenu ?? (menuItem.Parent as MenuItem)?.Parent as ContextMenu;
            var placementTarget = contextMenu?.PlacementTarget as FrameworkElement;

            return placementTarget?.DataContext as LayoutGroup;
        }

        private List<LayoutTab> GetTargetedTabsForContextMenu(object sender)
        {
            // 1. On récupère le MenuItem cliqué
            if (!(sender is MenuItem menuItem))
            {
                return new List<LayoutTab>();
            }

            // 2. On remonte au ContextMenu parent
            var contextMenu = menuItem.Parent as ContextMenu;

            // 3. On trouve l'élément UI sur lequel le clic droit a été déclenché (ta Grid)
            var placementTarget = contextMenu?.PlacementTarget as FrameworkElement;

            // 4. On récupère le DataContext (ton LayoutTab)
            var clickedTab = placementTarget?.DataContext as LayoutTab;

            // 5. Logique de ciblage
            if (clickedTab?.IsSelected == false)
            {
                // Si le clic a eu lieu sur un onglet NON sélectionné, l'action ne s'applique qu'à lui
                return new List<LayoutTab> { clickedTab };
            }

            // Sinon (l'onglet cliqué fait partie de la sélection), on applique l'action à TOUTE la sélection
            return GetVisibleItemsList()
                .Where(t => t.IsSelected && t is LayoutTab)
                .Cast<LayoutTab>()
                .ToList();
        }


        private void ClearSelection()
        {
            foreach (var item in PinnedItems)
            {
                item.IsSelected = false;
            }

            foreach (var item in Items)
            {
                item.IsSelected = false;
                if (item is LayoutGroup group)
                {
                    foreach (var subTab in group.SubTabs)
                    {
                        subTab.IsSelected = false;
                    }
                }
            }
        }

        // Aplatit la hiérarchie pour permettre une sélection par plage (Shift)
        private List<LayoutItem> GetVisibleItemsList()
        {
            var list = new List<LayoutItem>();
            list.AddRange(PinnedItems);
            foreach (var item in Items)
            {
                list.Add(item);
                if (item is LayoutGroup group)
                {
                    list.AddRange(group.SubTabs);
                }
            }
            return list;
        }

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is LayoutItem clickedItem)
            {
                _dragStart = e.GetPosition(null);
                _dragSource = clickedItem;
                _isDragging = false;
            }
        }

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging && _dragSource != null)
            {
                bool isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                bool isShiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

                if (_dragSource is LayoutGroup group && !isCtrlPressed && !isShiftPressed)
                {
                    group.IsExpanded = !group.IsExpanded;
                    SyncTabOrderToAutoCAD();
                    ScrollToItem(group);
                }
                else
                {
                    if (isShiftPressed && _lastSelectedItem != null)
                    {
                        // MULTI-SÉLECTION (Shift + Clic) : Sélection d'une plage
                        var flatList = GetVisibleItemsList();
                        int startIndex = flatList.IndexOf(_lastSelectedItem);
                        int endIndex = flatList.IndexOf(_dragSource);

                        if (startIndex != -1 && endIndex != -1)
                        {
                            ClearSelection();
                            int min = Math.Min(startIndex, endIndex);
                            int max = Math.Max(startIndex, endIndex);
                            for (int i = min; i <= max; i++)
                            {
                                flatList[i].IsSelected = true;
                            }
                        }
                    }
                    else if (isCtrlPressed)
                    {
                        // MULTI-SÉLECTION (Ctrl + Clic)
                        _dragSource.IsSelected = !_dragSource.IsSelected;
                        if (_dragSource is LayoutGroup gp)
                        {
                            gp.SubTabs.ToList().ForEach(t => t.IsSelected = gp.IsSelected);
                        }
                        _lastSelectedItem = _dragSource.IsSelected ? _dragSource : null;
                    }
                    else
                    {
                        // SÉLECTION SIMPLE
                        ClearSelection();
                        _dragSource.IsSelected = true;
                        SetCurrentItem(_dragSource);
                        _lastSelectedItem = _dragSource;
                    }
                }
            }

            _dragSource = null;
            _isDragging = false;
        }

        public void SetCurrentTab(LayoutItem item)
        {
            SetCurrentItem(item);
        }

        private void SetCurrentItem(LayoutItem item)
        {
            if (_currentItem != null)
            {
                _currentItem.IsCurrent = false;
            }

            _currentItem = item;
            _lastSelectedItem = item;
            if (_currentItem != null)
            {
                _currentItem.IsCurrent = true;
                //if (item is LayoutTab tab && tab.IsInGroup)
                //{
                //   // tab.ParentGroup.IsExpanded = true;
                //}

                ScrollToItem(item);
                // On prévient TEST.cs que l'onglet a changé, SEULEMENT si ce n'est pas une synchro d'AutoCAD
                if (item is LayoutTab selectedTab && !IsSyncing)
                {
                    ActiveTabChanged?.Invoke(this, selectedTab);
                }
            }
            ClearSelection();
        }

        private void ScrollToItem(LayoutItem item)
        {
            if (item == null || (item is LayoutTab tab && tab.IsPinned))
            {
                return;
            }

            Dispatcher.InvokeAsync(() =>
            {
                FrameworkElement container = TabItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement ?? GetVisualContainerFromItem(TabItemsControl, item);
                if (container != null)
                {
                    ScrollTabIntoView(container);
                }
            }, DispatcherPriority.Loaded);
        }

        private void ScrollTabIntoView(FrameworkElement container)
        {
            if (container == null || TabScrollViewer == null || PinnedControl == null || TabItemsControl == null)
            {
                return;
            }

            try
            {
                GeneralTransform transform = container.TransformToAncestor(TabItemsControl);
                Point itemPos = transform.Transform(new Point(0, 0));

                double itemExtentX = PinnedControl.ActualWidth + itemPos.X;
                double itemExtentRight = itemExtentX + container.ActualWidth;
                double visibleLeft = TabScrollViewer.HorizontalOffset + PinnedControl.ActualWidth;
                double visibleRight = TabScrollViewer.HorizontalOffset + TabScrollViewer.ViewportWidth;

                // On calcule l'espace réellement disponible pour l'affichage
                double visibleWidth = visibleRight - visibleLeft;

                // 1. Si l'élément est plus grand que la zone visible, on l'aligne d'office à gauche.
                if (container.ActualWidth > visibleWidth)
                {
                    TabScrollViewer.ScrollToHorizontalOffset(itemExtentX - PinnedControl.ActualWidth);
                }
                // 2. Si le bord gauche dépasse de l'écran, on ramène à gauche.
                else if (itemExtentX < visibleLeft)
                {
                    TabScrollViewer.ScrollToHorizontalOffset(itemExtentX - PinnedControl.ActualWidth);
                }
                // 3. Si le bord droit dépasse (et que l'élément n'est pas trop grand), on aligne à droite.
                else if (itemExtentRight > visibleRight)
                {
                    TabScrollViewer.ScrollToHorizontalOffset(itemExtentRight - TabScrollViewer.ViewportWidth);
                }
            }
            catch (InvalidOperationException) { }
        }

        private void OverflowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is LayoutTab clickedTab)
            {
                if (clickedTab.IsInGroup)
                {
                    clickedTab.ParentGroup.IsExpanded = true;
                }

                SetCurrentItem(clickedTab);
                OverflowToggle.IsChecked = false;
                ClearSelection();
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>()
            {
                {"Title", $"Présentation_{DateTime.Now.Ticks}" }
            };

            if (LayoutBar_NewLayoutRequest(properties))
            {
                TabScrollViewer.ScrollToRightEnd();
                ClearSelection();
            }

        }

        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                TabScrollViewer.LineLeft();
            }
            else
            {
                TabScrollViewer.LineRight();
            }

            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed && _dragSource?.IsPinned == false && !_isDragging)
            {
                Vector diff = _dragStart - e.GetPosition(null);
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;

                    if (!_dragSource.IsSelected)
                    {
                        // Si on drag un élément qui n'est pas sélectionné, on le sélectionne lui seul
                        ClearSelection();
                        _dragSource.IsSelected = true;
                        _lastSelectedItem = _dragSource;
                    }

                    // 1. On calcule la liste des éléments sélectionnés AVANT de créer la fenêtre
                    var selectedItems = GetVisibleItemsList()
                        .Where(i => i.IsSelected && !i.IsPinned && !(i is LayoutTab tab && tab.ParentGroup?.IsSelected == true))
                        .ToList();

                    // 2. On passe la liste pour générer la prévisualisation complète
                    CreatePreviewWindow(selectedItems);

                    _autoScrollTimer.Start();

                    DragDrop.DoDragDrop(this, new DataObject("SelectedItems", selectedItems), DragDropEffects.Move);
                    _autoScrollTimer.Stop();
                }
            }
        }

        private void CreatePreviewWindow(List<LayoutItem> selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                return;
            }

            // 1. On prépare un panel horizontal pour empiler nos vrais contrôles WPF
            var factory = new FrameworkElementFactory(typeof(StackPanel));
            factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            // 2. On utilise un ItemsControl qui va instancier de vrais éléments (clones parfaits)
            var previewItemsControl = new ItemsControl
            {
                Margin = new Thickness(10, 0, 0, 0),
                ItemsSource = selectedItems,
                ItemsPanel = new ItemsPanelTemplate(factory),
                // On récupère ton style de conteneur original pour respecter tes ZIndex/Marges
                ItemContainerStyle = TabItemsControl.ItemContainerStyle
            };

            // 3. On crée la fenêtre ultra-légère
            _ghostWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true,
                IsHitTestVisible = false, // La souris passe à travers les contrôles
                SizeToContent = SizeToContent.WidthAndHeight,

                // MAGIE ICI : On donne à la fenêtre l'accès à tes <UserControl.Resources>.
                // C'est ce qui lui permet de trouver tes DataTemplates (LayoutTab, LayoutGroup) et tes pinceaux !
                Resources = this.Resources,

                Content = new Border
                {
                    Opacity = 0.85, // Transparence globale du groupe qui est déplacé
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 10,
                        Opacity = 0.5,
                        Direction = 270
                    },
                    Child = previewItemsControl // On injecte notre générateur de contrôles
                }
            };

            _ghostWindow.Show();
        }

        private void DragDropEnd()
        {
            DragIndicator.IsOpen = false;

            if (_ghostWindow != null)
            {
                _ghostWindow.Close();
                _ghostWindow = null;
            }
        }



        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            if (_ghostWindow != null && _isDragging)
            {
                Win32Point w32Mouse = new Win32Point();
                GetCursorPos(ref w32Mouse);

                const int offset = 5;// On ajoute un petit décalage pour que le pointeur soit bien lisible au-dessus
                _ghostWindow.Left = w32Mouse.X + offset;
                _ghostWindow.Top = w32Mouse.Y + offset;

                //On dit à WPF de garder le vrai pointeur de la souris !
                e.UseDefaultCursors = true;
                e.Handled = true;
            }
        }

        private void AutoScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!_isDragging || TabScrollViewer == null || PinnedControl == null)
            {
                return;
            }

            // 1. Obtenir la position GLOBALE de la souris et la convertir en position locale
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            Point screenPos = new Point(w32Mouse.X, w32Mouse.Y);
            Point mousePos = this.PointFromScreen(screenPos); // Position relative au UserControl

            const double scrollTolerance = 80.0;
            const double minScrollStep = 2.0;
            const double maxScrollStep = 15.0;

            // 2. Définir les vrais bords visibles de la zone de scroll
            double leftVisibleEdge = PinnedControl.ActualWidth;

            // Le bord droit est le bord gauche + la largeur RÉELLE du ScrollViewer (qui exclut les boutons de droite)
            double rightVisibleEdge = leftVisibleEdge + TabScrollViewer.ActualWidth;

            double dist = 0;
            bool scrollLeft = false;

            if (mousePos.X < leftVisibleEdge + scrollTolerance)
            {
                dist += (mousePos.X - leftVisibleEdge);
                scrollLeft = true;
            }
            else if (mousePos.X > rightVisibleEdge - scrollTolerance)
            {
                dist += (rightVisibleEdge - mousePos.X);
            }

            if (dist != 0)
            {
                double intensity = (double)(1.0 - (dist / scrollTolerance));
                intensity = Math.Max(0.0, Math.Min(1.0, intensity));
                intensity = Math.Pow(intensity, 2); // Effet exponentiel
                double step = minScrollStep + ((maxScrollStep - minScrollStep) * intensity);

                if (scrollLeft)
                {
                    TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset - step);
                }
                else
                {
                    TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset + step);
                }
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (!_isDragging)
            {
                return;
            }

            LayoutItem sourceItem = (e.Data.GetData("SelectedItems") as List<LayoutItem>)?.FirstOrDefault()
                                    ?? _dragSource;

            if (sourceItem == null)
            {
                return;
            }

            var targetElement = e.OriginalSource as FrameworkElement;
            var targetContainer = GetVisualParent<ContentPresenter>(targetElement);

            if (sourceItem is LayoutGroup lg)
            {
                lg.IsExpanded = false;
            }

            if (targetContainer?.DataContext is LayoutItem targetItem)
            {
                LayoutGroup targetGrp = targetItem as LayoutGroup;
                if (targetItem is LayoutTab tTab && tTab.IsInGroup)
                {
                    targetGrp = tTab.ParentGroup;
                }

                if (targetGrp != null && sourceItem is LayoutTab sTab && sourceItem != targetItem)
                {
                    if (!targetGrp.Contains(sTab))
                    {
                        var groupContainer = targetItem is LayoutGroup ? targetContainer : GetVisualParent<ContentPresenter>(VisualTreeHelper.GetParent(targetContainer));
                        var placementTarget = groupContainer ?? targetContainer;
                        ShowDragIndicator(placementTarget, -12, (placementTarget.ActualHeight / 2) + 4, -90, PlacementMode.Relative);
                    }
                    else if (targetItem is LayoutGroup)
                    {
                        var firstTabItem = targetGrp.SubTabs.Count > 0 ? targetGrp.SubTabs[0] : null;
                        if (firstTabItem != null)
                        {
                            FrameworkElement firstTabContainer = GetVisualContainerFromItem(targetContainer, firstTabItem);
                            ShowDragIndicator(firstTabContainer ?? targetContainer, -5, -2, 0);
                        }
                    }
                    else if (targetItem is LayoutTab targetTabInside)
                    {
                        int baseIndex = targetGrp.IndexOf(targetTabInside);
                        int sourceIndex = targetGrp.IndexOf(sTab);
                        CalculateInsertionIndex(e, targetContainer, baseIndex, out bool isRightHalf);

                        bool isSameLocation = (!isRightHalf && sourceIndex == baseIndex - 1) || (isRightHalf && sourceIndex == baseIndex + 1);
                        if (isSameLocation)
                        {
                            var parentPanel = VisualTreeHelper.GetParent(targetContainer);
                            FrameworkElement sourceTabContainer = GetVisualContainerFromItem(parentPanel, sTab);
                            if (sourceTabContainer != null)
                            {
                                ShowDragIndicator(sourceTabContainer, sourceTabContainer.ActualWidth / 2, -2, 0);
                            }
                        }
                        else
                        {
                            ShowDragIndicator(targetContainer, isRightHalf ? targetContainer.ActualWidth - 5 : -5, -2, 0);
                        }
                    }
                }
                else if (Items.Contains(targetItem) && targetItem != sourceItem)
                {
                    int baseIndex = Items.IndexOf(targetItem);
                    int sourceIndex = Items.IndexOf(sourceItem);
                    CalculateInsertionIndex(e, targetContainer, baseIndex, out bool isRightHalf);

                    bool isSameLocation = (!isRightHalf && sourceIndex == baseIndex - 1) || (isRightHalf && sourceIndex == baseIndex + 1);
                    if (isSameLocation)
                    {
                        var parentPanel = VisualTreeHelper.GetParent(targetContainer);
                        FrameworkElement sourceTabContainer = GetVisualContainerFromItem(parentPanel, sourceItem);
                        if (sourceTabContainer != null)
                        {
                            ShowDragIndicator(sourceTabContainer, sourceTabContainer.ActualWidth / 2, -2, 0);
                        }
                    }
                    else
                    {
                        ShowDragIndicator(targetContainer, isRightHalf ? targetContainer.ActualWidth - 5 : -5, -2, 0);
                    }
                }

            }
        }

        private static int CalculateInsertionIndex(DragEventArgs e, FrameworkElement targetContainer, int currentItemIndex, out bool isRightHalf)
        {
            isRightHalf = false;
            if (currentItemIndex < 0)
            {
                currentItemIndex = 0;
            }

            if (targetContainer != null)
            {
                Point pos = e.GetPosition(targetContainer);
                isRightHalf = pos.X > targetContainer.ActualWidth / 2;
                if (isRightHalf)
                {
                    currentItemIndex++;
                }
            }
            return currentItemIndex;
        }

        private void ShowDragIndicator(UIElement PlacementTarget, double HorizontalOffset, double VerticalOffset, double Rotation, PlacementMode placementMode = PlacementMode.Top)
        {
            DragIndicator.RenderTransformOrigin = new Point(0.5, 0.5);
            DragIndicator.RenderTransform = new RotateTransform(Rotation);
            DragIndicator.PlacementTarget = PlacementTarget;
            DragIndicator.Placement = placementMode;
            DragIndicator.HorizontalOffset = HorizontalOffset;
            DragIndicator.VerticalOffset = VerticalOffset;
            DragIndicator.IsOpen = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            DragIndicator.IsOpen = false;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            DragDropEnd();
            try
            {
                // Récupération de la liste complète glissée
                if (!(e.Data.GetData("SelectedItems") is List<LayoutItem> sourceItems) || sourceItems.Count == 0)
                {
                    return;
                }

                var targetElement = e.OriginalSource as FrameworkElement;
                var targetContainer = GetVisualParent<ContentPresenter>(targetElement);
                var targetParentContainer = GetVisualParent<ContentPresenter>(targetContainer);

                if (targetContainer?.DataContext is LayoutItem targetItem)
                {
                    // Si on drop sur soi-même ou dans sa propre sélection, on annule
                    if (sourceItems.Contains(targetItem))
                    {
                        return;
                    }

                    LayoutGroup targetGrp = targetParentContainer?.DataContext as LayoutGroup ?? targetContainer.DataContext as LayoutGroup;
                    int insertIndex = 0;
                    LayoutGroup targetGroupDest = null;

                    // 1. Calcul de l'index d'insertion
                    if (targetGrp != null)
                    {
                        targetGroupDest = targetGrp;
                        if (!targetGrp.Contains(targetItem as LayoutTab)) // Sur l'en-tête, venant de l'extérieur
                        {
                            insertIndex = 0;
                            // targetGrp.IsExpanded = true;
                        }
                        else if (targetItem is LayoutTab tTab) // Sur un onglet interne
                        {
                            int baseIndex = targetGrp.IndexOf(tTab);
                            insertIndex = CalculateInsertionIndex(e, targetContainer, baseIndex, out _);
                        }
                        else if (targetItem is LayoutGroup) // Sur l'en-tête de son propre groupe
                        {
                            insertIndex = 0;
                        }
                    }
                    else
                    {
                        int baseIndex = Items.IndexOf(targetItem);
                        insertIndex = CalculateInsertionIndex(e, targetContainer, baseIndex, out _);
                    }

                    // 2. Compensation du décalage : retirer les éléments va décaler l'index cible
                    if (targetGroupDest != null)
                    {
                        int preItemsCount = sourceItems.Count(s => s is LayoutTab t && t.ParentGroup == targetGroupDest && targetGroupDest.IndexOf(t) < insertIndex);
                        insertIndex -= preItemsCount;
                    }
                    else
                    {
                        int preItemsCount = sourceItems.Count(s => Items.Contains(s) && Items.IndexOf(s) < insertIndex);
                        insertIndex -= preItemsCount;
                    }

                    // 3. Retirer tous les éléments de leurs emplacements d'origine
                    foreach (var item in sourceItems)
                    {
                        RemoveFromAll(item);
                    }

                    insertIndex = Math.Max(0, insertIndex);

                    var itemsToProcess = sourceItems.Where(i => !(i is LayoutTab t && sourceItems.Contains(t.ParentGroup))).ToList();

                    // 3. Retirer tous les éléments (utiliser la liste filtrée)
                    foreach (var item in itemsToProcess)
                    {
                        RemoveFromAll(item);
                    }

                    // 4. Réinsérer séquentiellement (utiliser la liste filtrée)
                    for (int i = 0; i < itemsToProcess.Count; i++)
                    {
                        var itemToInsert = itemsToProcess[i];

                        if (targetGroupDest != null && itemToInsert is LayoutTab t)
                        {
                            // Si le groupe est visé, on n'ajoute que des LayoutTabs
                            int finalIndex = Math.Min(insertIndex + i, targetGroupDest.SubTabs.Count);
                            targetGroupDest.Insert(finalIndex, t);
                        }
                        else
                        {
                            // Les LayoutGroup (ou éléments mis à la racine)
                            int finalIndex = Math.Min(insertIndex + i, Items.Count);
                            Items.Insert(finalIndex, itemToInsert);
                        }
                    }
                }
            }
            finally
            {
                ClearSelection();
                SyncTabOrderToAutoCAD();
                _dragSource = null;
                _isDragging = false;
            }
        }

        private void RemoveFromAll(LayoutItem item)
        {
            Items.Remove(item);
            PinnedItems.Remove(item);
            if (item is LayoutTab tab && tab.ParentGroup != null)
            {
                tab.ParentGroup.Remove(tab);
            }
        }

        private static T GetVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
            {
                return null;
            }

            if (parentObject is T parent)
            {
                return parent;
            }

            return GetVisualParent<T>(parentObject);
        }

        private static FrameworkElement GetVisualContainerFromItem(DependencyObject parent, object itemData)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.DataContext == itemData)
                {
                    if (!(fe.DataContext is LayoutGroup))
                    {
                        return fe;
                    }
                }
                var result = GetVisualContainerFromItem(child, itemData);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            DragDropEnd();
        }
    }

    public abstract class LayoutItem : INotifyPropertyChanged
    {
        private string _title;
        private bool _isPinned;
        private bool _isCurrent;
        private bool _isSelected;

        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
        public bool IsPinned { get => _isPinned; set { _isPinned = value; OnPropertyChanged(); } }
        public bool IsCurrent { get => _isCurrent; set { _isCurrent = value; OnPropertyChanged(); } }
        public virtual bool IsSelected
        {
            get => _isSelected;
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public object AutoCadData { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class LayoutTab : LayoutItem
    {
        private LayoutGroup _parentGroup;
        public LayoutGroup ParentGroup
        {
            get => _parentGroup;
            set { if (_parentGroup != value) { _parentGroup = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsInGroup)); } }
        }
        public bool IsInGroup => ParentGroup != null;
        public override bool IsSelected
        {
            get => base.IsSelected;
            set { if (base.IsSelected != value) { base.IsSelected = value; ParentGroup?.EvaluateGroupSelection(); } }
        }
        public bool IsModel { get; set; }
    }

    public class LayoutGroup : LayoutItem
    {
        private bool _isSyncing = false;

        public LayoutGroup()
        {
            SubTabs = new ReadOnlyObservableCollection<LayoutTab>(SubTabs_);
        }

        public override bool IsSelected
        {
            get => base.IsSelected;
            set
            {
                if (base.IsSelected != value)
                {
                    base.IsSelected = value;
                    if (!_isSyncing)
                    {
                        _isSyncing = true;
                        foreach (var tab in SubTabs_)
                        {
                            tab.IsSelected = value;
                        }

                        _isSyncing = false;
                    }
                }
            }
        }

        internal void EvaluateGroupSelection()
        {
            if (_isSyncing || SubTabs_.Count == 0)
            {
                return;
            }

            bool allSelected = SubTabs_.All(t => t.IsSelected);
            if (base.IsSelected != allSelected)
            {
                _isSyncing = true;
                base.IsSelected = allSelected;
                _isSyncing = false;
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LayoutTab> SubTabs_ { get; } = new ObservableCollection<LayoutTab>();
        public ReadOnlyObservableCollection<LayoutTab> SubTabs { get; }
        public void Add(LayoutTab tab) { tab.ParentGroup = this; SubTabs_.Add(tab); EvaluateGroupSelection(); }
        public void Insert(int index, LayoutTab tab) { tab.ParentGroup = this; SubTabs_.Insert(index, tab); EvaluateGroupSelection(); }
        public bool Remove(LayoutTab tab) { tab.ParentGroup = null; bool removed = SubTabs_.Remove(tab); if (removed) { EvaluateGroupSelection(); } return removed; }
        public bool Contains(LayoutTab tab) => SubTabs_.Contains(tab);
        public int IndexOf(LayoutTab tab) => SubTabs_.IndexOf(tab);
    }


    public static class XDataExtensions
    {
        // Renvoie le nom du groupe, l'état étendu, ET l'état épinglé
        public static (string GroupName, bool IsExpanded, bool IsPinned)? GetLayoutUIState(this DBObject obj)
        {
            string appName = Generic.GetExtensionDLLName();
            ResultBuffer rb = obj.GetXDataForApplication(appName);

            if (rb != null)
            {
                string groupName = null;
                bool isExpanded = true;
                bool isPinned = false;
                int int16Index = 0;

                foreach (TypedValue tv in rb.AsArray())
                {
                    if (tv.TypeCode == (short)DxfCode.ExtendedDataAsciiString)
                    {
                        string val = tv.Value as string;
                        if (val?.StartsWith("Group:") == true)
                        {
                            groupName = val.Substring(6);
                            if (groupName == "") groupName = null; // Nettoyage si vide
                        }
                    }
                    else if (tv.TypeCode == (short)DxfCode.ExtendedDataInteger16)
                    {
                        // Le 1er entier correspond à IsExpanded, le 2ème à IsPinned
                        if (int16Index == 0) isExpanded = (short)tv.Value == 1;
                        else if (int16Index == 1) isPinned = (short)tv.Value == 1;
                        int16Index++;
                    }
                }
                return (groupName, isExpanded, isPinned);
            }
            return null;
        }

        // Assigne le groupe, son état ET l'état épinglé
        public static void SetLayoutUIState(this DBObject obj, Transaction tr, string groupName, bool isExpanded, bool isPinned)
        {
            string appName = Generic.GetExtensionDLLName();
            Database db = obj.Database;

            RegAppTable regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!regTable.Has(appName))
            {
                regTable.UpgradeOpen();
                RegAppTableRecord app = new RegAppTableRecord { Name = appName };
                regTable.Add(app);
                tr.AddNewlyCreatedDBObject(app, true);
            }

            obj.XData = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Group:" + (groupName ?? "")),
                new TypedValue((int)DxfCode.ExtendedDataInteger16, isExpanded ? (short)1 : (short)0),
                new TypedValue((int)DxfCode.ExtendedDataInteger16, isPinned ? (short)1 : (short)0)
            );
        }
    }
}