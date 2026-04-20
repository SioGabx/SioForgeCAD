using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Windows.Data;
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
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static SioForgeCAD.Commun.Mist.User32PInvoke;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Forms
{
    public partial class LayoutBar : UserControl
    {
        public enum DuplicatePosition
        {
            Beginning,
            End,
            Before,
            After
        }

        #region Propriétés et Variables

        public ObservableCollection<LayoutItem> PinnedItems { get; } = new ObservableCollection<LayoutItem>();
        public ObservableCollection<LayoutItem> Items { get; } = new ObservableCollection<LayoutItem>();

        public event EventHandler<LayoutTab> ActiveTabChanged;

        /// <summary>
        /// Empêche la boucle infinie de synchronisation (AutoCAD -> WPF -> AutoCAD).
        /// </summary>
        public bool IsSyncing { get; set; }

        private Window _ghostWindow;
        private readonly DispatcherTimer _autoScrollTimer;

        private LayoutItem _currentItem;
        private LayoutItem _lastSelectedItem;

        // Variables Drag & Drop
        private Point? _dragStart;
        private bool _isDragging;
        private LayoutItem _dragSource;

        private const double DRAG_THRESHOLD = 35.0;

        public static bool CanLayoutSwitch
        {
            get
            {
                var doc = Generic.GetDocument();
                if (doc == null || doc.IsDisposed) return false;

                var sysVar = Generic.GetSystemVariable("TRACEMODE");
                if (sysVar != null && (short)sysVar == 2) return false;

                if (doc.Editor.IsQuiescent) return !Utils.IsInBlockEditor();

                return false;
            }
        }

        #endregion

        #region Constructeur & Cycle de vie

        public LayoutBar()
        {
            InitializeComponent();
            LayoutBarRoot.DataContext = this;

            _autoScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) }; // ~60 FPS

            Loaded += LayoutBar_Loaded;
            Unloaded += LayoutBar_Unloaded;
        }

        private void LayoutBar_Loaded(object sender, RoutedEventArgs e)
        {
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
            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;

            var lm = LayoutManager.Current;
            lm.LayoutSwitched -= OnLayoutSwitched;
            lm.LayoutCreated -= OnLayoutsChanged;
            lm.LayoutRemoved -= OnLayoutsChanged;
            lm.LayoutRenamed -= OnLayoutRenamed;

            ActiveTabChanged -= LayoutBar_ActiveTabChanged;
            TabScrollViewer.ScrollChanged -= TabScrollViewer_ScrollChanged;
            _autoScrollTimer.Tick -= AutoScrollTimer_Tick;
        }

        #endregion

        #region Synchronisation AutoCAD <-> WPF

        private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e) => ReloadTabs(e.Document);

        private bool _isDuplicating = false;
        private void OnLayoutsChanged(object sender, LayoutEventArgs e)
        {
            if (!_isDuplicating)
            {
                ReloadTabs(Application.DocumentManager.MdiActiveDocument);
            }
        }
        private void OnLayoutRenamed(object sender, LayoutRenamedEventArgs e) => ReloadTabs(Application.DocumentManager.MdiActiveDocument);

        private void OnLayoutSwitched(object sender, LayoutEventArgs e)
        {
            if (IsSyncing) return;

            IsSyncing = true;
            try
            {
                var tab = GetVisibleItemsList().FirstOrDefault(t => t.Title == e.Name);
                if (tab != null) SetCurrentTab(tab);
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void LayoutBar_ActiveTabChanged(object sender, LayoutTab e)
        {
            if (IsSyncing) return;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                if (LayoutManager.Current.CurrentLayout != e.Title && LayoutManager.Current.LayoutExists(e.Title))
                {
                    LayoutManager.Current.CurrentLayout = e.Title;
                }
                tr.Commit();
            }
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
                            UpdateLayoutData(tr, tab, currentOrder++, null, true);
                        }
                        else if (item is LayoutGroup group)
                        {
                            foreach (var subTab in group.SubTabs)
                            {
                                UpdateLayoutData(tr, subTab, currentOrder++, group.Title, group.IsExpanded);
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
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
                if (tr.GetObject(layoutId, OpenMode.ForWrite) is Layout layout)
                {
                    if (layout.TabOrder != order) layout.TabOrder = order;
                    layout.SetLayoutUIState(tr, groupName, isExpanded, tab.IsPinned);
                }
            }
        }

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
                        layouts.Add((Layout)tr.GetObject(entry.Value, OpenMode.ForRead));
                    }

                    var activeGroups = new Dictionary<string, LayoutGroup>();

                    foreach (var layout in layouts.OrderBy(l => l.TabOrder))
                    {
                        var uiState = layout.GetLayoutUIState();
                        bool isPinnedUI = uiState?.IsPinned ?? false;

                        var newTab = new LayoutTab
                        {
                            Title = layout.LayoutName,
                            IsModel = layout.ModelType,
                            IsPinned = layout.ModelType || isPinnedUI, // Le model est TOUJOURS épinglé
                            AutoCadData = layout.ObjectId
                        };

                        if (newTab.IsPinned)
                        {
                            PinnedItems.Add(newTab);
                        }

                        if (!newTab.IsModel)
                        {
                            string groupName = uiState?.GroupName;
                            bool isExpanded = uiState?.IsExpanded ?? true;

                            if (!string.IsNullOrEmpty(groupName))
                            {
                                if (!activeGroups.TryGetValue(groupName, out LayoutGroup group))
                                {
                                    group = new LayoutGroup { Title = groupName, IsExpanded = isExpanded };
                                    activeGroups[groupName] = group;
                                    Items.Add(group);
                                }
                                group.Add(newTab);
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

        private static void ExpandLayoutItemGroup(LayoutItem item, bool IsExpanded)
        {
            if (item is LayoutGroup gp)
            {
                gp.IsExpanded = IsExpanded;
            }
        }

        private bool TryCreateNewLayout(string title)
        {
            if (IsSyncing) return false;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                if (!LayoutManager.Current.LayoutExists(title))
                {
                    var id = LayoutManager.Current.CreateLayout(title);
                    LayoutManager.Current.SetCurrentLayoutId(id);
                    tr.Commit();
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Interactions & Sélection

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is LayoutItem clickedItem)
            {
                _dragStart = e.GetPosition(this);
                Debug.WriteLine("_dragStart to " + _dragStart.ToString());
                _dragSource = GetTabFromRegularList(clickedItem);
                _isDragging = false;
            }
        }

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!CanLayoutSwitch) return;

            if (!_isDragging && _dragSource != null)
            {
                bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                bool isShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

                HandleSelection(_dragSource, isCtrl, isShift);
            }

            _dragSource = null;
            _isDragging = false;
        }

        private void HandleSelection(LayoutItem targetItem, bool isCtrl, bool isShift)
        {
            // Toggle d'un groupe (Expand/Collapse)
            if (targetItem is LayoutGroup group && !isCtrl && !isShift)
            {
                group.IsExpanded = !group.IsExpanded;
                SyncTabOrderToAutoCAD();
                ScrollToItem(group);
                return;
            }

            // Sélection multiple via SHIFT
            if (isShift && _lastSelectedItem != null)
            {
                var flatList = GetVisibleItemsList(false);
                int startIndex = flatList.IndexOf(_lastSelectedItem);
                int endIndex = flatList.IndexOf(targetItem);

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
            // Sélection multiple via CTRL
            else if (isCtrl)
            {
                targetItem.IsSelected = !targetItem.IsSelected;

                if (targetItem is LayoutGroup gp)
                {
                    foreach (var subTab in gp.SubTabs) subTab.IsSelected = gp.IsSelected;
                }
                _lastSelectedItem = targetItem.IsSelected ? targetItem : null;
            }
            // Sélection simple
            else
            {
                ClearSelection();
                targetItem.IsSelected = true;
                SetCurrentItem(targetItem);
                _lastSelectedItem = targetItem;
            }
        }

        public void SetCurrentTab(LayoutItem item) => SetCurrentItem(item);

        private void SetCurrentItem(LayoutItem item)
        {
            if (_currentItem != null) _currentItem.IsCurrent = false;
            item = GetTabFromRegularList(item);

            _currentItem = item;
            _lastSelectedItem = item;

            if (_currentItem != null)
            {
                _currentItem.IsCurrent = true;
                ScrollToItem(item);

                if (item is LayoutTab selectedTab && !IsSyncing)
                {
                    ActiveTabChanged?.Invoke(this, selectedTab);
                }
            }
            ClearSelection();
        }

        private LayoutItem GetTabFromRegularList(LayoutItem item)
        {
            if (item is LayoutTab itemTab && itemTab.IsPinned)
            {
                var RegularItemsList = GetVisibleItemsList(false);
                return RegularItemsList.Where(t => t.Title == item.Title).DefaultIfEmpty(item).First();
            }
            return item;
        }

        private void ClearSelection()
        {
            foreach (var item in PinnedItems) item.IsSelected = false;
            foreach (var item in Items)
            {
                item.IsSelected = false;
                if (item is LayoutGroup group)
                {
                    foreach (var subTab in group.SubTabs) subTab.IsSelected = false;
                }
            }
        }

        private List<LayoutItem> GetVisibleItemsList(bool IncludePinnedItems = true)
        {
            List<LayoutItem> list = new List<LayoutItem>((IncludePinnedItems ? PinnedItems.ToList() : new List<LayoutItem>()));
            foreach (var item in Items)
            {
                list.Add(item);
                if (item is LayoutGroup group) list.AddRange(group.SubTabs);
            }
            return list;
        }

        #endregion

        #region Drag & Drop

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && _dragSource != null && _dragStart is Point dragStart)
            {
                Point currentPos = e.GetPosition(this);
                Vector diff = dragStart - currentPos;
                Debug.WriteLine("check _dragStart + current pos " + currentPos);
                if (Math.Abs(diff.X) > DRAG_THRESHOLD || Math.Abs(diff.Y) > DRAG_THRESHOLD)
                {
                    _isDragging = true;

                    if (!_dragSource.IsSelected)
                    {
                        ClearSelection();
                        _dragSource.IsSelected = true;
                        _lastSelectedItem = _dragSource;
                    }

                    var selectedItems = GetVisibleItemsList(false)
                        .Where(i => i.IsSelected && !(i is LayoutTab tab && tab.ParentGroup?.IsSelected == true))
                        .ToList();

                    //Close all groups for preview
                    selectedItems.ForEach(t => ExpandLayoutItemGroup(t, false));

                    Debug.WriteLine("CreatePreviewWindow");
                    CreatePreviewWindow(selectedItems);
                    _autoScrollTimer.Start();

                    DragDrop.DoDragDrop(this, new DataObject("SelectedItems", selectedItems), DragDropEffects.Move);

                    DragDropEnd();
                    _autoScrollTimer.Stop();
                }
            }
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);
            if (_ghostWindow != null && _isDragging)
            {
                Win32Point w32Mouse = new Win32Point();
                GetCursorPos(ref w32Mouse);

                _ghostWindow.Left = w32Mouse.X + 5;
                _ghostWindow.Top = w32Mouse.Y + 5;

                e.UseDefaultCursors = true;
                e.Handled = true;
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            Debug.WriteLine("OnDragOver");
            base.OnDragOver(e);
            if (!_isDragging) return;
            Debug.WriteLine("OnDragOver and _isDragging");
            var sourceItem = (e.Data.GetData("SelectedItems") as List<LayoutItem>)?.FirstOrDefault() ?? _dragSource;
            if (sourceItem == null) return;

            var targetElement = e.OriginalSource as FrameworkElement;
            var targetContainer = UIHelper.GetVisualParent<ContentPresenter>(targetElement);

            if (targetContainer?.DataContext is LayoutItem targetItem)
            {
                if (targetItem == sourceItem) return;
                if (sourceItem is LayoutGroup lg) lg.IsExpanded = false;
                LayoutGroup targetGrp = targetItem as LayoutGroup;
                if (targetItem is LayoutTab tTab && tTab.IsInGroup) targetGrp = tTab.ParentGroup;

                if (targetGrp != null && sourceItem is LayoutTab sTab && sourceItem != targetItem)
                {
                    // --- CAS 1 : Déplacement vers ou à l'intérieur d'un GROUPE ---
                    if (!targetGrp.Contains(sTab))
                    {
                        // A. Si l'onglet source n'appartient pas encore à ce groupe (entrée dans un groupe)
                        var placementTarget = (targetItem is LayoutGroup ? targetContainer : UIHelper.GetVisualParent<ContentPresenter>(VisualTreeHelper.GetParent(targetContainer))) ?? targetContainer;
                        ShowDragIndicator(placementTarget, -12, (placementTarget.ActualHeight / 2) + 4, -90, PlacementMode.Relative);
                    }
                    else if (targetItem is LayoutGroup)
                    {
                        // B. Si on survole l'en-tête du groupe lui-même
                        var firstTabItem = targetGrp.SubTabs.FirstOrDefault();
                        if (firstTabItem != null)
                        {
                            var firstTabContainer = UIHelper.GetVisualContainerFromItem(targetContainer, firstTabItem);
                            ShowDragIndicator(firstTabContainer ?? targetContainer, -5, -2, 0);
                        }
                    }
                    else if (targetItem is LayoutTab targetTabInside)
                    {
                        // C. Réorganisation à l'intérieur du même groupe
                        int baseIndex = targetGrp.IndexOf(targetTabInside);
                        int sourceIndex = targetGrp.IndexOf(sTab);
                        CalculateInsertionIndex(e, targetContainer, baseIndex, out bool isRightHalf);

                        if ((!isRightHalf && sourceIndex == baseIndex - 1) || (isRightHalf && sourceIndex == baseIndex + 1))
                        {
                            var sourceTabContainer = UIHelper.GetVisualContainerFromItem(VisualTreeHelper.GetParent(targetContainer), sTab);
                            if (sourceTabContainer != null) ShowDragIndicator(sourceTabContainer, sourceTabContainer.ActualWidth / 2, -2, 0);
                        }
                        else
                        {
                            ShowDragIndicator(targetContainer, isRightHalf ? targetContainer.ActualWidth - 5 : -5, -2, 0);
                        }
                    }
                }
                // --- CAS 2 : Déplacement entre éléments de la LISTE PRINCIPALE (Hors groupes) ---
                else if (Items.Contains(targetItem) && targetItem != sourceItem)
                {
                    int baseIndex = Items.IndexOf(targetItem);
                    int sourceIndex = Items.IndexOf(sourceItem);
                    CalculateInsertionIndex(e, targetContainer, baseIndex, out bool isRightHalf);

                    if ((!isRightHalf && sourceIndex == baseIndex - 1) || (isRightHalf && sourceIndex == baseIndex + 1))
                    {
                        var sourceTabContainer = UIHelper.GetVisualContainerFromItem(VisualTreeHelper.GetParent(targetContainer), sourceItem);
                        if (sourceTabContainer != null) ShowDragIndicator(sourceTabContainer, sourceTabContainer.ActualWidth / 2, -2, 0);
                    }
                    else
                    {
                        ShowDragIndicator(targetContainer, isRightHalf ? targetContainer.ActualWidth - 5 : -5, -2, 0);
                    }
                }
            }
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            DragIndicator.IsOpen = false;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            Debug.WriteLine("OnDrop");
            base.OnDrop(e);

            if (!(e.Data.GetData("SelectedItems") is List<LayoutItem> sourceItems) || sourceItems.Count == 0) return;
            DragDropEnd();
            try
            {
                var targetElement = e.OriginalSource as FrameworkElement;
                var targetContainer = UIHelper.GetVisualParent<ContentPresenter>(targetElement);
                var targetParentContainer = UIHelper.GetVisualParent<ContentPresenter>(targetContainer);

                if (targetContainer?.DataContext is LayoutItem targetItem)
                {
                    if (sourceItems.Contains(targetItem)) return;

                    LayoutGroup targetGrp = targetParentContainer?.DataContext as LayoutGroup ?? targetContainer.DataContext as LayoutGroup;
                    int insertIndex = 0;
                    LayoutGroup targetGroupDest = null;

                    // 1. Définition du parent cible et calcul de l'index brut
                    if (targetGrp != null)
                    {
                        targetGroupDest = targetGrp;
                        if (!targetGrp.Contains(targetItem as LayoutTab)) insertIndex = 0;
                        else if (targetItem is LayoutTab tTab) insertIndex = CalculateInsertionIndex(e, targetContainer, targetGrp.IndexOf(tTab), out _);
                        else if (targetItem is LayoutGroup) insertIndex = 0;
                    }
                    else
                    {
                        insertIndex = CalculateInsertionIndex(e, targetContainer, Items.IndexOf(targetItem), out _);
                    }

                    // 2. Compensation des index (si on retire des éléments placés AVANT l'index cible)
                    if (targetGroupDest != null)
                        insertIndex -= sourceItems.Count(s => s is LayoutTab t && t.ParentGroup == targetGroupDest && targetGroupDest.IndexOf(t) < insertIndex);
                    else
                        insertIndex -= sourceItems.Count(s => Items.Contains(s) && Items.IndexOf(s) < insertIndex);

                    insertIndex = Math.Max(0, insertIndex);

                    // 3. Suppression et Réinsertion
                    var itemsToProcess = sourceItems.Where(i => !(i is LayoutTab t && sourceItems.Contains(t.ParentGroup))).ToList();

                    foreach (var item in itemsToProcess) RemoveFromAll(item, true, false);

                    for (int i = 0; i < itemsToProcess.Count; i++)
                    {
                        var itemToInsert = itemsToProcess[i];
                        if (targetGroupDest != null && itemToInsert is LayoutTab t)
                        {
                            targetGroupDest.Insert(Math.Min(insertIndex + i, targetGroupDest.SubTabs.Count), t);
                        }
                        else
                        {
                            Items.Insert(Math.Min(insertIndex + i, Items.Count), itemToInsert);
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

        private void RemoveFromAll(LayoutItem item, bool RemoveFromRegulars, bool RemoveFromPinneds)
        {
            if (RemoveFromRegulars) Items.Remove(item);
            if (RemoveFromPinneds) PinnedItems.Remove(item);
            if (item is LayoutTab tab && tab.ParentGroup != null) tab.ParentGroup.Remove(tab);
        }

        private static int CalculateInsertionIndex(DragEventArgs e, FrameworkElement targetContainer, int currentItemIndex, out bool isRightHalf)
        {
            isRightHalf = false;
            if (currentItemIndex < 0) currentItemIndex = 0;

            if (targetContainer != null)
            {
                isRightHalf = e.GetPosition(targetContainer).X > targetContainer.ActualWidth / 2;
                if (isRightHalf) currentItemIndex++;
            }
            return currentItemIndex;
        }

        private void InsertItems(List<LayoutItem> itemsToInsert, LayoutGroup targetGroup, int startIndex)
        {
            for (int i = 0; i < itemsToInsert.Count; i++)
            {
                var item = itemsToInsert[i];

                // Règle métier : Si on cible un groupe ET que l'élément est un onglet (un groupe ne va pas dans un groupe)
                if (targetGroup != null && item is LayoutTab tab)
                {
                    int index = Math.Min(startIndex + i, targetGroup.SubTabs.Count);
                    targetGroup.Insert(index, tab);
                }
                else
                {
                    // Insertion à la racine (Items)
                    int index = Math.Min(startIndex, Items.Count);
                    Items.Insert(index, item);
                    startIndex++; // On incrémente l'index racine pour que les éléments suivants ne s'empilent pas à l'envers
                }
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            DragDropEnd();
        }

        #endregion

        #region Drag & Drop Visuals (Fenêtre fantôme & Timer)

        private void CreatePreviewWindow(List<LayoutItem> selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;

            var factory = new FrameworkElementFactory(typeof(StackPanel));
            factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var previewItemsControl = new ItemsControl
            {
                Margin = new Thickness(10, 0, 0, 0),
                ItemsSource = selectedItems,
                ItemsPanel = new ItemsPanelTemplate(factory),
                ItemContainerStyle = TabItemsControl.ItemContainerStyle
            };

            _ghostWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true,
                IsHitTestVisible = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Resources = this.Resources,
                Content = new Border
                {
                    Opacity = 0.85,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, Opacity = 0.5, Direction = 270 },
                    Child = previewItemsControl
                }
            };
            _ghostWindow.Show();
        }

        private void DragDropEnd()
        {
            Debug.WriteLine("DragDropEnd");
            DragIndicator.IsOpen = false;
            if (_ghostWindow != null)
            {
                try { _ghostWindow.Close(); } catch { } finally { _ghostWindow = null; }
            }
        }

        private void ShowDragIndicator(UIElement placementTarget, double hOffset, double vOffset, double rotation, PlacementMode mode = PlacementMode.Top)
        {
            DragIndicator.RenderTransformOrigin = new Point(0.5, 0.5);
            DragIndicator.RenderTransform = new RotateTransform(rotation);
            DragIndicator.PlacementTarget = placementTarget;
            DragIndicator.Placement = mode;
            DragIndicator.HorizontalOffset = hOffset;
            DragIndicator.VerticalOffset = vOffset;
            DragIndicator.IsOpen = true;
        }

        private void AutoScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!_isDragging || TabScrollViewer == null || PinnedControl == null) return;

            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            Point mousePos = this.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));

            const double scrollTolerance = 80.0;
            double leftVisibleEdge = PinnedControl.ActualWidth;
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
                double intensity = Math.Pow((1.0 - (dist / scrollTolerance)).Clamp(0, 1), 2);
                double step = 2.0 + (13.0 * intensity); // entre 2.0 et 15.0

                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset + (scrollLeft ? -step : step));
            }
        }
        #endregion

        #region UI Events & Scrolling

        private void ScrollToItem(LayoutItem item)
        {
            if (item == null || (item is LayoutTab tab && tab.IsPinned)) return;

            Dispatcher.InvokeAsync(() =>
            {
                var container = TabItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement
                                ?? UIHelper.GetVisualContainerFromItem(TabItemsControl, item);

                if (container != null) ScrollTabIntoView(container);
            }, DispatcherPriority.Loaded);
        }

        private void ScrollTabIntoView(FrameworkElement container)
        {
            if (container == null || TabScrollViewer == null || PinnedControl == null) return;

            try
            {
                GeneralTransform transform = container.TransformToAncestor(TabItemsControl);
                Point itemPos = transform.Transform(new Point(0, 0));

                double itemExtentX = PinnedControl.ActualWidth + itemPos.X;
                double itemExtentRight = itemExtentX + container.ActualWidth;
                double visibleLeft = TabScrollViewer.HorizontalOffset + PinnedControl.ActualWidth;
                double visibleRight = TabScrollViewer.HorizontalOffset + TabScrollViewer.ViewportWidth;
                double visibleWidth = visibleRight - visibleLeft;

                if (container.ActualWidth > visibleWidth || itemExtentX < visibleLeft)
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

        private void TabScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            OverflowToggle.Visibility = TabScrollViewer.ScrollableWidth > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Calculate the target position
            double scrollStep = e.Delta * -2;
            double targetOffset = TabScrollViewer.HorizontalOffset + scrollStep;

            targetOffset = Math.Max(0, Math.Min(targetOffset, TabScrollViewer.ScrollableWidth));

            DoubleAnimation scrollAnimation = new DoubleAnimation
            {
                From = TabScrollViewer.HorizontalOffset,
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(200),
                // QuadraticEase makes the start/stop feel natural rather than robotic
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // We use BeginAnimation on a dummy dependency property
            TabScrollViewer.BeginAnimation(ScrollHelper.HorizontalOffsetProperty, scrollAnimation);

            e.Handled = true;
        }

        private void OverflowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CanLayoutSwitch) return;

            if ((sender as MenuItem)?.DataContext is LayoutTab clickedTab)
            {
                if (clickedTab.IsInGroup) clickedTab.ParentGroup.IsExpanded = true;
                SetCurrentItem(clickedTab);
                OverflowToggle.IsChecked = false;
                ClearSelection();
            }
        }

        private string GenerateUniqueLayoutName(string baseName)
        {
            string newName = $"{baseName} (Copie)";
            int counter = 2;

            while (LayoutManager.Current.LayoutExists(newName))
            {
                newName = $"{baseName} (Copie {counter})";
                counter++;
            }
            return newName;
        }

        private string GenerateUniqueGroupName(string baseName)
        {
            string newName = $"{baseName} (Copie)";
            int counter = 2;

            // On vérifie l'existence du nom dans les éléments de niveau racine (Items)
            while (Items.OfType<LayoutGroup>().Any(g => g.Title == newName))
            {
                newName = $"{baseName} (Copie {counter})";
                counter++;
            }
            return newName;
        }

        private void InsertDuplicatedItems(List<LayoutItem> newItems, DuplicatePosition pos, LayoutItem targetItem)
        {
            int rootInsertIndex = Items.Count;
            int groupInsertIndex = -1;
            LayoutGroup targetGroup = null;

            // Calcul de l'index de base en fonction de la cible
            switch (pos)
            {
                case DuplicatePosition.Beginning:
                    rootInsertIndex = 0;
                    groupInsertIndex = 0; // Si applicable
                    break;

                case DuplicatePosition.End:
                    rootInsertIndex = Items.Count;
                    // groupInsertIndex reste à -1 (ajout à la fin)
                    break;

                case DuplicatePosition.Before:
                    if (targetItem is LayoutTab tTabBefore && tTabBefore.IsInGroup)
                    {
                        targetGroup = tTabBefore.ParentGroup;
                        groupInsertIndex = targetGroup.IndexOf(tTabBefore);
                        rootInsertIndex = Items.IndexOf(targetGroup);
                    }
                    else
                    {
                        rootInsertIndex = targetItem != null ? Items.IndexOf(targetItem) : 0;
                    }
                    break;

                case DuplicatePosition.After:
                    if (targetItem is LayoutTab tTabAfter && tTabAfter.IsInGroup)
                    {
                        targetGroup = tTabAfter.ParentGroup;
                        groupInsertIndex = targetGroup.IndexOf(tTabAfter) + 1;
                        rootInsertIndex = Items.IndexOf(targetGroup) + 1;
                    }
                    else
                    {
                        rootInsertIndex = targetItem != null ? Items.IndexOf(targetItem) + 1 : Items.Count;
                    }
                    break;
            }

            if (rootInsertIndex < 0) rootInsertIndex = Items.Count;

            // Insertion des éléments
            for (int i = 0; i < newItems.Count; i++)
            {
                var item = newItems[i];

                // Si c'est un onglet et qu'on cible l'intérieur d'un groupe
                if (item is LayoutTab tab && targetGroup != null)
                {
                    int insertAt = groupInsertIndex >= 0 ? groupInsertIndex + i : targetGroup.SubTabs.Count;
                    targetGroup.Insert(Math.Min(insertAt, targetGroup.SubTabs.Count), tab);
                }
                else
                {
                    // Soit c'est un groupe (qui ne peut pas aller dans un groupe), soit on cible la racine
                    Items.Insert(Math.Min(rootInsertIndex, Items.Count), item);
                    rootInsertIndex++; // On décale l'index racine pour le prochain élément
                }
            }
        }

        private LayoutTab DuplicateSingleTab(LayoutTab originalTab)
        {
            string newName = GenerateUniqueLayoutName(originalTab.Title);

            // Clonage physique dans AutoCAD
            LayoutManager.Current.CloneLayout(originalTab.Title, newName, 0);

            // Création de l'objet WPF
            var newLayoutId = LayoutManager.Current.GetLayoutId(newName);
            return new LayoutTab
            {
                Title = newName,
                IsModel = false,
                IsPinned = false,
                AutoCadData = newLayoutId
            };
        }



        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TryCreateNewLayout($"Présentation_{DateTime.Now.Ticks}"))
            {
                TabScrollViewer.ScrollToRightEnd();
                ClearSelection();
            }
        }

        private void Item_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.ToolTip is ToolTip tt && fe.DataContext is LayoutTab tab)
            {
                if (fe.ContextMenu?.IsOpen == true || OverflowToggle.IsChecked == true)
                {
                    e.Handled = true;
                    return;
                }

                if ((tt.Content as Border)?.Child is System.Windows.Controls.Image img)
                {
                    img.Source = LayoutManager.Current.GetLayoutImage(tab.Title);
                }
            }
        }

        #endregion

        #region Menu Contextuel

        private void TabContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu && menu.PlacementTarget is FrameworkElement fe && fe.DataContext is LayoutTab tab)
            {
                _dragStart = null; //avoid drag
                Debug.WriteLine("_dragStart to null");
                var targetTabs = GetTargetedTabsForContextMenu(sender);

                foreach (MenuItem menuItem in menu.Items.OfType<MenuItem>())
                {
                    switch (menuItem.Name)
                    {
                        case "TabMenuItem_Pin":
                            menuItem.Visibility = (!tab.IsPinned && !tab.IsModel) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                            break;
                        case "TabMenuItem_Unpin":
                            menuItem.Visibility = (tab.IsPinned && !tab.IsModel) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                            break;
                        case "TabMenuItem_Rename":
                        case "TabMenuItem_Delete":
                            menuItem.IsEnabled = !tab.IsModel;
                            break;
                        case "TabMenuItem_Publish":
                            menuItem.IsEnabled = targetTabs.Count > 1;
                            break;
                        case "TabMenuItem_CreateGp":
                            var ct = targetTabs.Count(t => t.IsInGroup);
                            menuItem.IsEnabled = !tab.IsModel && ct <= 0;
                            break;
                    }
                }
            }
        }

        private void ContextMenu_Epingler_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender, t => !t.IsPinned && !t.IsModel);
            if (targetTabs.Count == 0) return;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in targetTabs)
                {
                    tab.IsPinned = true;
                    if (!PinnedItems.Contains(tab)) PinnedItems.Add(tab);

                    if (tab.AutoCadData is ObjectId layoutId && !layoutId.IsNull)
                    {
                        var layout = tr.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                        layout?.SetLayoutUIState(tr, tab.ParentGroup?.Title, true, true);
                    }
                }
                tr.Commit();
            }
            ClearSelection();
        }

        private void ContextMenu_Desepingler_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender, t => t.IsPinned && !t.IsModel);
            if (targetTabs.Count == 0) return;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in targetTabs)
                {
                    tab.IsPinned = false;
                    PinnedItems.Remove(tab);

                    if (tab.AutoCadData is ObjectId layoutId && !layoutId.IsNull)
                    {
                        var layout = tr.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                        layout?.SetLayoutUIState(tr, tab.ParentGroup?.Title, true, false);
                    }
                }
                tr.Commit();
            }
            ClearSelection();
        }

        private void ContextMenu_Renommer_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender, t => !t.IsModel);
            foreach (var tab in targetTabs)
            {
                Debug.WriteLine($"Renommer : {tab.Title}");
                // Implémentation du renommage
            }
        }

        private void ContextMenu_Tracer_Click(object sender, RoutedEventArgs e) => Debug.WriteLine("Tracer");
        private void ContextMenu_Publier_Click(object sender, RoutedEventArgs e) => Debug.WriteLine("Publier");

        private void ContextMenu_Supprimer_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender, t => !t.IsModel);
            if (targetTabs.Count == 0) return;

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in targetTabs)
                {
                    try { LayoutManager.Current.DeleteLayout(tab.Title); } catch { }
                }
                tr.Commit();
            }
            ClearSelection();
        }

        private void ContextMenu_CreerGroupe_Click(object sender, RoutedEventArgs e)
        {
            var targetTabs = GetTargetedTabsForContextMenu(sender, t => !t.IsModel);
            if (targetTabs.Count == 0) return;

            var newGroup = new LayoutGroup { Title = "Nouveau Groupe", IsExpanded = true };

            int insertIndex = Items.IndexOf(targetTabs.First());
            Items.Insert(insertIndex == -1 ? Items.Count : insertIndex, newGroup);

            foreach (var tab in targetTabs)
            {
                RemoveFromAll(tab, true, false);
                newGroup.Add(tab);
            }

            ClearSelection();
            SyncTabOrderToAutoCAD();
        }

        private void ContextMenu_RenommerGroupe_Click(object sender, RoutedEventArgs e) => Debug.WriteLine("Renommer Groupe");
        private void ContextMenu_TracerGroupe_Click(object sender, RoutedEventArgs e) => Debug.WriteLine("Tracer Groupe");
        private void ContextMenu_PublierGroupe_Click(object sender, RoutedEventArgs e) => Debug.WriteLine("Publier Groupe");

        private void ContextMenu_SupprimerGroupeSeul_Click(object sender, RoutedEventArgs e)
        {
            var targetGroup = GetTargetedGroupForContextMenu(sender);
            if (targetGroup == null) return;

            int groupIndex = Items.IndexOf(targetGroup);
            if (groupIndex == -1) return;

            var tabsToExtract = targetGroup.SubTabs.ToList();
            foreach (var tab in tabsToExtract)
            {
                targetGroup.Remove(tab);
                Items.Insert(groupIndex++, tab);
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

            using (Generic.GetLock())
            using (var tr = Generic.GetTrans())
            {
                foreach (var tab in tabsToDelete)
                {
                    try { LayoutManager.Current.DeleteLayout(tab.Title); } catch { }
                }
                tr.Commit();
            }

            Items.Remove(targetGroup);
            ClearSelection();
        }

        private static LayoutGroup GetTargetedGroupForContextMenu(object sender)
        {
            if (!(sender is MenuItem menuItem)) return null;
            var contextMenu = menuItem.Parent as ContextMenu ?? (menuItem.Parent as MenuItem)?.Parent as ContextMenu;
            return (contextMenu?.PlacementTarget as FrameworkElement)?.DataContext as LayoutGroup;
        }

        private List<LayoutTab> GetTargetedTabsForContextMenu(object sender, Func<LayoutTab, bool> predicate = null)
        {
            predicate = predicate ?? (_ => true);
            LayoutTab clickedTab = null;

            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu menuFromItem)
            {
                clickedTab = (menuFromItem.PlacementTarget as FrameworkElement)?.DataContext as LayoutTab;
            }
            else if (sender is ContextMenu contextMenu)
            {
                clickedTab = (contextMenu.PlacementTarget as FrameworkElement)?.DataContext as LayoutTab;
            }

            if (clickedTab?.IsSelected == false && clickedTab?.IsCurrent == false)
            {
                return predicate(clickedTab) ? new List<LayoutTab> { clickedTab } : new List<LayoutTab>();
            }

            return GetVisibleItemsList().OfType<LayoutTab>().Where(t => (t.IsSelected || t.IsCurrent) && predicate(t)).ToList();
        }

        #endregion

        private void ContextMenu_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            // 1. Déterminer ce qui doit être dupliqué (un groupe entier OU des onglets sélectionnés)
            var itemsToDuplicate = new List<LayoutItem>();

            var selectedItems = GetVisibleItemsList(false) // true/false selon si vous autorisez la duplication d'onglets épinglés
          .Where(i => i.IsSelected &&
                      !(i is LayoutTab tab && (tab.IsModel || tab.ParentGroup?.IsSelected == true)))
          .ToList();
            if (selectedItems.Count > 0)
            {
                itemsToDuplicate.AddRange(selectedItems);
            }
            else
            {
                // Fallback de sécurité : si l'utilisateur fait un clic droit direct sur un élément 
                // non sélectionné (ce qui ne devrait pas arriver souvent avec le comportement WPF)
                var targetGroup = GetTargetedGroupForContextMenu(sender);
                if (targetGroup != null)
                {
                    itemsToDuplicate.Add(targetGroup);
                }
                else
                {
                    itemsToDuplicate.AddRange(GetTargetedTabsForContextMenu(sender, t => !t.IsModel));
                }
            }

            if (itemsToDuplicate.Count == 0) return;

            // -----------------------------------------------------------------------
            // À FAIRE : Afficher la boîte de dialogue pour définir la position
            // (userChoicePosition et userChoiceTarget)
            // -----------------------------------------------------------------------
            DuplicatePosition userChoicePosition = DuplicatePosition.After;
            LayoutItem userChoiceTarget = itemsToDuplicate.Last();

            _isDuplicating = true;

            try
            {
                using (Generic.GetLock())
                using (var tr = Generic.GetTrans())
                {
                    var newlyCreatedItems = new List<LayoutItem>();

                    // 2. Traitement de chaque élément
                    foreach (var item in itemsToDuplicate)
                    {
                        if (item is LayoutTab tab)
                        {
                            newlyCreatedItems.Add(DuplicateSingleTab(tab));
                        }
                        else if (item is LayoutGroup group)
                        {
                            // Duplication du conteneur Groupe
                            var newGroup = new LayoutGroup
                            {
                                Title = GenerateUniqueGroupName(group.Title),
                                IsExpanded = group.IsExpanded
                            };

                            // Duplication de chaque présentation contenue dans ce groupe
                            foreach (var subTab in group.SubTabs)
                            {
                                var newSubTab = DuplicateSingleTab(subTab);
                                newGroup.Add(newSubTab);
                            }

                            newlyCreatedItems.Add(newGroup);
                        }
                    }

                    // 3. Insertion visuelle au bon endroit
                    InsertDuplicatedItems(newlyCreatedItems, userChoicePosition, userChoiceTarget);

                    tr.Commit();

                    // 4. Synchronisation de l'ordre global (XData)
                    SyncTabOrderToAutoCAD();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la duplication : {ex.Message}");
            }
            finally
            {
                _isDuplicating = false;
                ReloadTabs(Application.DocumentManager.MdiActiveDocument);
            }
        }
    }



    #region Classes Utilitaires (Visual Tree)

    internal static class UIHelper
    {
        public static T GetVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            return parentObject as T ?? GetVisualParent<T>(parentObject);
        }

        public static FrameworkElement GetVisualContainerFromItem(DependencyObject parent, object itemData)
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.DataContext == itemData && !(fe.DataContext is LayoutGroup)) return fe;

                var result = GetVisualContainerFromItem(child, itemData);
                if (result != null) return result;
            }
            return null;
        }
    }

    #endregion

    #region Modèles de Données (LayoutItems)

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

        public object AutoCadData { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        private bool _isExpanded;

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
                        foreach (var tab in SubTabs_) tab.IsSelected = value;
                        _isSyncing = false;
                    }
                }
            }
        }

        internal void EvaluateGroupSelection()
        {
            if (_isSyncing || SubTabs_.Count == 0) return;
            bool allSelected = SubTabs_.All(t => t.IsSelected);
            if (base.IsSelected != allSelected)
            {
                _isSyncing = true;
                base.IsSelected = allSelected;
                _isSyncing = false;
            }
        }

        public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }

        private ObservableCollection<LayoutTab> SubTabs_ { get; } = new ObservableCollection<LayoutTab>();
        public ReadOnlyObservableCollection<LayoutTab> SubTabs { get; }

        public void Add(LayoutTab tab) { tab.ParentGroup = this; SubTabs_.Add(tab); EvaluateGroupSelection(); }
        public void Insert(int index, LayoutTab tab) { tab.ParentGroup = this; SubTabs_.Insert(index, tab); EvaluateGroupSelection(); }
        public bool Remove(LayoutTab tab) { tab.ParentGroup = null; bool removed = SubTabs_.Remove(tab); if (removed) EvaluateGroupSelection(); return removed; }
        public bool Contains(LayoutTab tab) => SubTabs_.Contains(tab);
        public int IndexOf(LayoutTab tab) => SubTabs_.IndexOf(tab);
    }

    #endregion

    #region Extensions AutoCAD XData

    public static class XDataExtensions
    {
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
                    if (tv.TypeCode == (short)DxfCode.ExtendedDataAsciiString && tv.Value is string val && val.StartsWith("Group:"))
                    {
                        groupName = val.Substring(6);
                        if (string.IsNullOrEmpty(groupName)) groupName = null;
                    }
                    else if (tv.TypeCode == (short)DxfCode.ExtendedDataInteger16)
                    {
                        if (int16Index == 0) isExpanded = (short)tv.Value == 1;
                        else if (int16Index == 1) isPinned = (short)tv.Value == 1;
                        int16Index++;
                    }
                }
                return (groupName, isExpanded, isPinned);
            }
            return null;
        }

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

    #endregion

    public static class ScrollHelper
    {
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(ScrollHelper),
                new PropertyMetadata(0.0, OnHorizontalOffsetChanged));

        public static void SetHorizontalOffset(DependencyObject target, double value)
            => target.SetValue(HorizontalOffsetProperty, value);

        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer viewer)
            {
                viewer.ScrollToHorizontalOffset((double)e.NewValue);
            }
        }
    }
}