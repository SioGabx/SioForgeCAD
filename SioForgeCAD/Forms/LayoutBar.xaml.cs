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

namespace SioForgeCAD.Forms
{
    public partial class LayoutBar : UserControl
    {
        public ObservableCollection<LayoutItem> PinnedItems { get; } = new ObservableCollection<LayoutItem>();
        public ObservableCollection<LayoutItem> Items { get; } = new ObservableCollection<LayoutItem>();

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

            TabScrollViewer.ScrollChanged += (s, e) => OverflowToggle.Visibility = TabScrollViewer.ScrollableWidth > 0 ? Visibility.Visible : Visibility.Collapsed;

            _autoScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15) // Rafraîchissement ultra fluide (~60 fps)
            };
            _autoScrollTimer.Tick += AutoScrollTimer_Tick;



            PinnedItems.Add(new LayoutTab { Title = "Model", IsPinned = true });
            Items.Add(new LayoutTab { Title = "Plan RDC" });
            Items.Add(new LayoutTab { Title = "Plan R+1" });

            var group = new LayoutGroup { Title = "Feuilles" };
            group.Add(new LayoutTab { Title = "Coupe AA" });
            group.Add(new LayoutTab { Title = "Coupe BB" });
            Items.Add(group);

            Items.Add(new LayoutTab { Title = "Plan R+2" });
            Items.Add(new LayoutTab { Title = "Plan R+3" });
            Items.Add(new LayoutTab { Title = "Plan R+4" });
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
                if (item is LayoutTab tab && tab.IsInGroup)
                {
                    tab.ParentGroup.IsExpanded = true;
                }

                ScrollToItem(item);
            }
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

                if (itemExtentX < visibleLeft)
                {
                    TabScrollViewer.ScrollToHorizontalOffset(itemExtentX - PinnedControl.ActualWidth);
                }
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
                SetCurrentItem(clickedTab);
                OverflowToggle.IsChecked = false;
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var newTab = new LayoutTab { Title = $"Layout {Items.Count + PinnedItems.Count + 1}" };
            Items.Add(newTab);
            SetCurrentItem(newTab);
            TabScrollViewer.ScrollToRightEnd();
            ClearSelection();
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
                        .Where(i => i.IsSelected && !i.IsPinned && !(i is LayoutTab tab && tab.ParentGroup != null && tab.ParentGroup.IsSelected))
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
            if (selectedItems == null || !selectedItems.Any()) return;

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

            Debug.WriteLine("dist" + dist);
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

            // Récupération de la liste complète glissée
            List<LayoutItem> sourceItems = e.Data.GetData("SelectedItems") as List<LayoutItem>;
            if (sourceItems == null || !sourceItems.Any())
            {
                _dragSource = null;
                _isDragging = false;
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
                    _dragSource = null;
                    _isDragging = false;
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
                        targetGrp.IsExpanded = true;
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

            _dragSource = null;
            _isDragging = false;
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
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

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
}