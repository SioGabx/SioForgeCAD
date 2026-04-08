using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SioForgeCAD.Forms
{
    /*
    ╔══════════════════════════════════════════════════════════════════════════╗
    ║  LayoutBar — Barre de layouts style AutoCAD                             ║
    ║                                                                          ║
    ║  FONCTIONNALITÉS :                                                       ║
    ║  • Tabs libres + groupes expand/collapse avec le même style que les tabs ║
    ║  • Overflow : bouton ▾ qui affiche un popup listant tous les tabs        ║
    ║  • Multi-sélection (Ctrl+clic)                                           ║
    ║  • Drag & drop pour réorganiser, déplacer vers un groupe, extraire       ║
    ║  • Tooltip avec aperçu image (TooltipImage sur LayoutTabItem)            ║
    ║  • Événements : TabActivated, TabRightClicked, NewLayoutRequested,       ║
    ║    TabsMoved, TabsGrouped, TabsUngrouped                                 ║
    ║                                                                          ║
    ║  UTILISATION :                                                           ║
    ║    var bar = new LayoutBar();                                             ║
    ║    bar.AddFreeTab("Model", makeCurrent: true);                           ║
    ║    var g = bar.AddGroup("Feuilles", "Plan RDC", "Coupe AA");             ║
    ║    bar.TabActivated      += (s, tab) => { ... };                         ║
    ║    bar.NewLayoutRequested += (s, e)   => { ... };                        ║
    ╚══════════════════════════════════════════════════════════════════════════╝
    */

    public partial class LayoutBar : UserControl
    {
        // ─────────────────────────────────────────────────────────────────────
        // EVENTS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Déclenché quand l'utilisateur active un tab (clic gauche simple).</summary>
        public event EventHandler<LayoutTabItem> TabActivated;

        /// <summary>Déclenché quand l'utilisateur clique sur "+".</summary>
        public event EventHandler NewLayoutRequested;

        /// <summary>Déclenché sur clic droit d'un tab.</summary>
        public event EventHandler<LayoutTabItem> TabRightClicked;

        /// <summary>
        /// Déclenché après un drag-drop de réorganisation.
        /// args : liste des tabs déplacés.
        /// </summary>
        public event EventHandler<IReadOnlyList<LayoutTabItem>> TabsMoved;

        /// <summary>Déclenché quand des tabs sont ajoutés à un groupe.</summary>
        public event EventHandler<TabsGroupedEventArgs> TabsGrouped;

        /// <summary>Déclenché quand des tabs sont retirés d'un groupe (devenus libres).</summary>
        public event EventHandler<IReadOnlyList<LayoutTabItem>> TabsUngrouped;

        // ─────────────────────────────────────────────────────────────────────
        // COLLECTIONS PUBLIQUES
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Tabs libres (non groupés), affichés en premier.</summary>
        public ObservableCollection<LayoutTabItem> FreeTabs { get; }
            = new ObservableCollection<LayoutTabItem>();

        /// <summary>Groupes de tabs.</summary>
        public ObservableCollection<LayoutTabGroup> Groups { get; }
            = new ObservableCollection<LayoutTabGroup>();

        // ─────────────────────────────────────────────────────────────────────
        // STATE PRIVÉ
        // ─────────────────────────────────────────────────────────────────────

        private LayoutTabItem _currentTab;

        /// <summary>Ensemble des tabs actuellement multi-sélectionnés (Ctrl+clic).</summary>
        private readonly HashSet<LayoutTabItem> _multiSelection = new HashSet<LayoutTabItem>();

        // Drag & drop
        private Point _dragStart;
        private bool _isDragging;
        private LayoutTabItem _dragSourceTab;
        private const double DragThreshold = 5.0;

        // ─────────────────────────────────────────────────────────────────────
        // PROPRIÉTÉ : CurrentTab
        // ─────────────────────────────────────────────────────────────────────

        public LayoutTabItem CurrentTab
        {
            get => _currentTab;
            set => SetCurrentTab(value);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUCTEUR
        // ─────────────────────────────────────────────────────────────────────

        public LayoutBar()
        {
            InitializeComponent();

            FreeTabsControl.ItemsSource = FreeTabs;
            GroupsControl.ItemsSource = Groups;
            TabScrollViewer.ScrollChanged += (_, __) => UpdateOverflow();
            // Recalcul de l'overflow quand la taille change
            //SizeChanged += (_, __) => UpdateOverflow();
            //FreeTabs.CollectionChanged += (_, __) => UpdateOverflow();
            //Groups.CollectionChanged += (_, __) => UpdateOverflow();
        }

        // ─────────────────────────────────────────────────────────────────────
        // API PUBLIQUE — Tabs
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Crée et ajoute un tab libre.</summary>
        public LayoutTabItem AddFreeTab(string title, bool makeCurrent = false)
        {
            var tab = new LayoutTabItem { Title = title };
            FreeTabs.Add(tab);
            if (makeCurrent) SetCurrentTab(tab);
            return tab;
        }

        /// <summary>Crée un groupe avec les titres fournis.</summary>
        public LayoutTabGroup AddGroup(string groupTitle, params string[] tabTitles)
        {
            var grp = new LayoutTabGroup { Title = groupTitle };
            foreach (var t in tabTitles)
            {
                var tab = new LayoutTabItem { Title = t, Group = grp };
                grp.Items.Add(tab);
            }
            Groups.Add(grp);
            return grp;
        }

        /// <summary>Ajoute un tab à un groupe existant.</summary>
        public LayoutTabItem AddTabToGroup(LayoutTabGroup group, string title,
                                           bool makeCurrent = false)
        {
            var tab = new LayoutTabItem { Title = title, Group = group };
            group.Items.Add(tab);
            if (makeCurrent) SetCurrentTab(tab);
            return tab;
        }

        /// <summary>Supprime un tab (libre ou dans un groupe).</summary>
        public void RemoveTab(LayoutTabItem tab)
        {
            if (tab == null) return;
            _multiSelection.Remove(tab);

            if (tab.Group != null)
                tab.Group.Items.Remove(tab);
            else
                FreeTabs.Remove(tab);

            if (_currentTab == tab)
            {
                var first = AllTabs().FirstOrDefault();
                if (first != null) SetCurrentTab(first);
                else _currentTab = null;
            }
        }

        /// <summary>Supprime un groupe et tous ses tabs.</summary>
        public void RemoveGroup(LayoutTabGroup group)
        {
            if (group == null) return;
            bool currentWasInGroup = group.Items.Contains(_currentTab);
            foreach (var t in group.Items) _multiSelection.Remove(t);
            Groups.Remove(group);

            if (currentWasInGroup)
            {
                var first = AllTabs().FirstOrDefault();
                if (first != null) SetCurrentTab(first);
                else _currentTab = null;
            }
        }

        /// <summary>Déplace un tab vers un groupe (ou le libère si group == null).</summary>
        public void MoveTabToGroup(LayoutTabItem tab, LayoutTabGroup group)
        {
            if (tab == null) return;

            // Retirer de la source
            if (tab.Group != null)
                tab.Group.Items.Remove(tab);
            else
                FreeTabs.Remove(tab);

            tab.Group = group;

            if (group != null)
            {
                group.Items.Add(tab);
                TabsGrouped?.Invoke(this, new TabsGroupedEventArgs(
                    new[] { tab }, group));
            }
            else
            {
                FreeTabs.Add(tab);
                TabsUngrouped?.Invoke(this, new List<LayoutTabItem> { tab });
            }
        }

        /// <summary>Déplace la multi-sélection vers un groupe (ou libère si null).</summary>
        public void MoveSelectionToGroup(LayoutTabGroup group)
        {
            var tabs = _multiSelection.ToList();
            foreach (var t in tabs) MoveTabToGroup(t, group);
        }

        /// <summary>Retourne tous les tabs (libres + dans tous les groupes).</summary>
        public IEnumerable<LayoutTabItem> AllTabs()
        {
            foreach (var t in FreeTabs) yield return t;
            foreach (var g in Groups)
                foreach (var t in g.Items)
                    yield return t;
        }

        /// <summary>Retourne les tabs multi-sélectionnés (lecture seule).</summary>
        public IReadOnlyCollection<LayoutTabItem> MultiSelection
            => _multiSelection;

        // ─────────────────────────────────────────────────────────────────────
        // LOGIQUE INTERNE — Sélection
        // ─────────────────────────────────────────────────────────────────────

        private void SetCurrentTab(LayoutTabItem tab)
        {
            if (_currentTab != null)
            {
                _currentTab.IsCurrent = false;
                if (_currentTab.Group != null)
                    _currentTab.Group.IsCurrent = false;
            }

            _currentTab = tab;

            if (_currentTab != null)
            {
                _currentTab.IsCurrent = true;
                if (_currentTab.Group != null)
                    _currentTab.Group.IsCurrent = true;
            }
        }

        private void ToggleMultiSelection(LayoutTabItem tab)
        {
            if (_multiSelection.Contains(tab))
            {
                tab.IsMultiSelected = false;
                _multiSelection.Remove(tab);
            }
            else
            {
                tab.IsMultiSelected = true;
                _multiSelection.Add(tab);
            }
        }

        private void ClearMultiSelection()
        {
            foreach (var t in _multiSelection)
                t.IsMultiSelected = false;
            _multiSelection.Clear();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LOGIQUE INTERNE — Overflow
        // ─────────────────────────────────────────────────────────────────────


  private void UpdateOverflow()
        {
            var showOverflow = TabScrollViewer.ScrollableWidth > 0;

            OverflowToggle.Visibility = showOverflow
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (showOverflow)
            {
                // Remplir la liste overflow avec TOUS les tabs
                OverflowList.ItemsSource = AllTabs().ToList();
                UpdateTabScrollViewerMaxWidth();
            }
           

        }
        private void UpdateTabScrollViewerMaxWidth()
        {
            double totalWidth = 0;
            var ParentElement = TabScrollViewer.Parent as FrameworkElement;
            double viewportWidth = ParentElement?.ActualWidth ?? 0;

             foreach (FrameworkElement tab in MainPanel.Children)
            {
                double tabWidth = tab.ActualWidth;

                // Si le tab ne rentre plus complètement, stop
                if (totalWidth + tabWidth > viewportWidth)
                    break;

                totalWidth += tabWidth;
            }

            // Limite le ScrollViewer pour ne montrer que les tabs visibles
            TabScrollViewer.MaxWidth = totalWidth;
        }
        // ─────────────────────────────────────────────────────────────────────
        // EVENT HANDLERS — Tab interactions
        // ─────────────────────────────────────────────────────────────────────

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem lbi) ||
                !(lbi.DataContext is LayoutTabItem tab)) return;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // Multi-sélection Ctrl+clic
                ToggleMultiSelection(tab);
            }
            else
            {
                ClearMultiSelection();
                SetCurrentTab(tab);
                TabActivated?.Invoke(this, tab);
            }

            e.Handled = true;
        }

        private void TabItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem lbi) ||
                !(lbi.DataContext is LayoutTabItem tab)) return;

            TabRightClicked?.Invoke(this, tab);
            e.Handled = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EVENT HANDLERS — Drag & Drop
        // ─────────────────────────────────────────────────────────────────────

        private void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBoxItem lbi) ||
                !(lbi.DataContext is LayoutTabItem tab)) return;

            _dragStart = e.GetPosition(null);
            _dragSourceTab = tab;
            _isDragging = false;
        }

        private void TabItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragSourceTab == null)
                return;

            var pos = e.GetPosition(null);
            if (_isDragging) return;

            if (Math.Abs(pos.X - _dragStart.X) > DragThreshold ||
                Math.Abs(pos.Y - _dragStart.Y) > DragThreshold)
            {
                _isDragging = true;

                // Les tabs multi-sélectionnés sont tous inclus dans le drag
                var draggedTabs = _multiSelection.Count > 0
                    ? _multiSelection.ToList()
                    : new List<LayoutTabItem> { _dragSourceTab };

                var data = new DataObject(typeof(List<LayoutTabItem>), draggedTabs);
                DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);

                _isDragging = false;
                _dragSourceTab = null;
            }
        }

        private void TabItem_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(List<LayoutTabItem>))
                ? DragDropEffects.Move
                : DragDropEffects.None;

            // Indicateurs visuels (left/right)
            if (sender is ListBoxItem lbi)
                ShowDropIndicator(lbi, e.GetPosition(lbi).X < lbi.ActualWidth / 2);

            e.Handled = true;
        }

        private void TabItem_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem lbi)
                HideDropIndicators(lbi);
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (!(sender is ListBoxItem lbi) ||
                !(lbi.DataContext is LayoutTabItem targetTab)) return;
            if (!e.Data.GetDataPresent(typeof(List<LayoutTabItem>))) return;

            HideDropIndicators(lbi);

            var draggedTabs = (List<LayoutTabItem>)e.Data.GetData(typeof(List<LayoutTabItem>));
            bool insertBefore = e.GetPosition(lbi).X < lbi.ActualWidth / 2;

            foreach (var tab in draggedTabs)
                ReorderTab(tab, targetTab, insertBefore);

            TabsMoved?.Invoke(this, draggedTabs);
            e.Handled = true;
        }

        private void Group_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(List<LayoutTabItem>))
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Group_Drop(object sender, DragEventArgs e)
        {
            if (!(sender is FrameworkElement fe) ||
                !(fe.DataContext is LayoutTabGroup group)) return;
            if (!e.Data.GetDataPresent(typeof(List<LayoutTabItem>))) return;

            var draggedTabs = (List<LayoutTabItem>)e.Data.GetData(typeof(List<LayoutTabItem>));
            foreach (var tab in draggedTabs)
                MoveTabToGroup(tab, group);

            TabsGrouped?.Invoke(this, new TabsGroupedEventArgs(draggedTabs, group));
            e.Handled = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // LOGIQUE INTERNE — Réorganisation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Déplace <paramref name="tab"/> juste avant ou juste après
        /// <paramref name="target"/> dans la même collection.
        /// Si les deux tabs sont dans des collections différentes, le tab
        /// source migre vers la collection de la cible.
        /// </summary>
        private void ReorderTab(LayoutTabItem tab, LayoutTabItem target, bool insertBefore)
        {
            if (tab == target) return;

            // Identifier les collections source et cible
            var srcCollection = GetContainerCollection(tab);
            var dstCollection = GetContainerCollection(target);

            if (srcCollection == null || dstCollection == null) return;

            // Retirer de la source
            srcCollection.Remove(tab);

            // Mettre à jour le groupe du tab si nécessaire
            if (srcCollection != dstCollection)
            {
                tab.Group = GetGroupForCollection(dstCollection);
            }

            // Insérer à la bonne position dans la destination
            int targetIdx = dstCollection.IndexOf(target);
            if (!insertBefore) targetIdx++;
            targetIdx = Math.Max(0, Math.Min(targetIdx, dstCollection.Count));
            dstCollection.Insert(targetIdx, tab);
        }

        private ObservableCollection<LayoutTabItem> GetContainerCollection(LayoutTabItem tab)
        {
            if (FreeTabs.Contains(tab)) return FreeTabs;
            foreach (var g in Groups)
                if (g.Items.Contains(tab)) return g.Items;
            return null;
        }

        private LayoutTabGroup GetGroupForCollection(
            ObservableCollection<LayoutTabItem> collection)
        {
            foreach (var g in Groups)
                if (g.Items == collection) return g;
            return null; // collection == FreeTabs
        }

        // ─────────────────────────────────────────────────────────────────────
        // LOGIQUE INTERNE — Indicateurs drop
        // ─────────────────────────────────────────────────────────────────────

        private static void ShowDropIndicator(ListBoxItem lbi, bool onLeft)
        {
            if (!(lbi.Template?.FindName("DropLeftIndicator", lbi) is UIElement l)) return;
            if (!(lbi.Template?.FindName("DropRightIndicator", lbi) is UIElement r)) return;
            ((FrameworkElement)l).Visibility = onLeft ? Visibility.Visible : Visibility.Collapsed;
            ((FrameworkElement)r).Visibility = !onLeft ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void HideDropIndicators(ListBoxItem lbi)
        {
            if (lbi.Template?.FindName("DropLeftIndicator", lbi) is FrameworkElement l)
                l.Visibility = Visibility.Collapsed;
            if (lbi.Template?.FindName("DropRightIndicator", lbi) is FrameworkElement r)
                r.Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EVENT HANDLERS — Autres boutons
        // ─────────────────────────────────────────────────────────────────────

        private void NewLayoutBtn_Click(object sender, RoutedEventArgs e)
            => NewLayoutRequested?.Invoke(this, EventArgs.Empty);

        private void OverflowList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(OverflowList.SelectedItem is LayoutTabItem tab)) return;
            ClearMultiSelection();
            SetCurrentTab(tab);
            TabActivated?.Invoke(this, tab);

            // Fermer le popup
            OverflowToggle.IsChecked = false;

            // Scroll jusqu'au tab (best-effort)
            ScrollToTab(tab);
        }

        private void ScrollToTab(LayoutTabItem tab)
        {
            // Pas d'API directe sur ItemsControl — on peut utiliser
            // BringIntoView via un hack léger après le rendu.
            Dispatcher.InvokeAsync(() =>
            {
                // Le tab sera visible au prochain cycle de layout grâce à
                // IsCurrent = true; on force simplement un scroll à droite
                // si nécessaire via le ScrollViewer.
                TabScrollViewer?.ScrollToHorizontalOffset(
                    TabScrollViewer.HorizontalOffset); // provoque un layout pass
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DATA MODELS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Représente un layout (une présentation AutoCAD).</summary>
    public class LayoutTabItem : INotifyPropertyChanged
    {
        private string _title;
        private bool _isCurrent;
        private bool _isMultiSelected;
        private bool _isEditable = true;
        private ImageSource _tooltipImage;

        /// <summary>Nom affiché dans le tab et dans le tooltip.</summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>True quand ce tab est le tab courant (affiché actif).</summary>
        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        /// <summary>True quand ce tab est inclus dans la multi-sélection (Ctrl+clic).</summary>
        public bool IsMultiSelected
        {
            get => _isMultiSelected;
            set { _isMultiSelected = value; OnPropertyChanged(); }
        }

        /// <summary>Si false, le nom ne peut pas être édité par l'utilisateur.</summary>
        public bool IsEditable
        {
            get => _isEditable;
            set { _isEditable = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Image affichée dans le tooltip de prévisualisation.
        /// Laisser null pour masquer l'aperçu.
        /// Exemple :  tab.TooltipImage = new BitmapImage(new Uri("pack://..."));
        /// </summary>
        public ImageSource TooltipImage
        {
            get => _tooltipImage;
            set { _tooltipImage = value; OnPropertyChanged(); }
        }

        /// <summary>Groupe parent (null si aucun groupe).</summary>
        public LayoutTabGroup Group { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Représente un groupe de layouts (expand/collapse).</summary>
    public class LayoutTabGroup : INotifyPropertyChanged
    {
        private string _title;
        private bool _isExpanded = true;
        private bool _isCurrent;

        /// <summary>Nom du groupe — affiché dans l'en-tête.</summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>Contrôle la visibilité des tabs du groupe.</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExpandIcon));
            }
        }

        /// <summary>True quand l'un des tabs du groupe est le tab courant.</summary>
        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        /// <summary>Icône ▾ / ▸ affichée dans l'en-tête selon l'état.</summary>
        public string ExpandIcon => IsExpanded ? "▾" : "▸";

        /// <summary>Tabs appartenant à ce groupe.</summary>
        public ObservableCollection<LayoutTabItem> Items { get; }
            = new ObservableCollection<LayoutTabItem>();

        /// <summary>Commande de bascule expand/collapse — liée au clic sur l'en-tête.</summary>
        public ICommand ToggleExpandCommand { get; }

        public LayoutTabGroup()
        {
            ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EVENT ARGS
    // ─────────────────────────────────────────────────────────────────────────

    public class TabsGroupedEventArgs : EventArgs
    {
        public IReadOnlyList<LayoutTabItem> Tabs { get; }
        public LayoutTabGroup Group { get; }

        public TabsGroupedEventArgs(IReadOnlyList<LayoutTabItem> tabs,
                                    LayoutTabGroup group)
        {
            Tabs = tabs;
            Group = group;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RELAY COMMAND
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>ICommand relay générique.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute,
                            Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
            => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter)
            => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
