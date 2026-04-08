using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SioForgeCAD.Forms
{
    public partial class LayoutBar : UserControl
    {
        // 1. Nouvelle collection dédiée pour figer les épinglés à gauche
        public ObservableCollection<LayoutItem> PinnedItems { get; } = new ObservableCollection<LayoutItem>();
        public ObservableCollection<LayoutItem> Items { get; } = new ObservableCollection<LayoutItem>();

        private LayoutItem _currentItem;
        private Point _dragStart;
        private bool _isDragging;
        private LayoutItem _dragSource;

        public LayoutBar()
        {
            InitializeComponent();
            LayoutBarRoot.DataContext = this;

            TabScrollViewer.ScrollChanged += (s, e) =>
            {
                OverflowToggle.Visibility = TabScrollViewer.ScrollableWidth > 0 ? Visibility.Visible : Visibility.Collapsed;
            };

            // Ajout dans la bonne collection
            PinnedItems.Add(new LayoutTab { Title = "Model", IsPinned = true });
            Items.Add(new LayoutTab { Title = "Plan RDC" });

            var group = new LayoutGroup { Title = "Feuilles" };
            group.Add(new LayoutTab { Title = "Coupe AA" });
            group.Add(new LayoutTab { Title = "Coupe BB" });
            Items.Add(group);
        }

        //private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    if ((sender as FrameworkElement)?.DataContext is LayoutItem clickedItem)
        //    {
        //        if (clickedItem is LayoutGroup group) group.IsExpanded = !group.IsExpanded;
        //        else SetCurrentItem(clickedItem);

        //        _dragStart = e.GetPosition(null);
        //        _dragSource = clickedItem;
        //        _isDragging = false;
        //    }
        //}

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // On ne fait QUE préparer les variables pour le Drag & Drop
            if ((sender as FrameworkElement)?.DataContext is LayoutItem clickedItem)
            {
                _dragStart = e.GetPosition(null);
                _dragSource = clickedItem;
                _isDragging = false;

                // Optionnel : Capture la souris pour s'assurer que le MouseUp sera bien capté ici
                ((FrameworkElement)sender).CaptureMouse();
            }
        }

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            element?.ReleaseMouseCapture();

            // Si on a relâché le clic SANS avoir bougé la souris (donc sans drag)
            if (!_isDragging && _dragSource != null)
            {
                // C'est un vrai "Clic" ! On exécute l'action.
                if (_dragSource is LayoutGroup group)
                {
                    group.IsExpanded = !group.IsExpanded;
                }
                else
                    SetCurrentItem(_dragSource);
            }

            // On réinitialise
            _dragSource = null;
            _isDragging = false;
        }

        private void SetCurrentItem(LayoutItem item)
        {
            if (_currentItem != null) _currentItem.IsCurrent = false;
            _currentItem = item;
            if (_currentItem != null)
            {
                _currentItem.IsCurrent = true;
                if (item is LayoutTab tab && tab.IsInGroup) tab.ParentGroup.IsExpanded = true;
                ScrollToItem(item);
            }
        }

        private void ScrollToItem(LayoutItem item)
        {
            // Peut se trouver dans l'un des deux ItemsControls (Pinned ou Standard)
            var container = TabItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            container?.BringIntoView();
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
        }

        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) TabScrollViewer.LineLeft();
            else TabScrollViewer.LineRight();
            e.Handled = true;
        }

        // --- DRAG AND DROP ---

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            // 2. Annulation du drag si l'item est épinglé
            if (e.LeftButton == MouseButtonState.Pressed && _dragSource != null && !_dragSource.IsPinned && !_isDragging)
            {
                Vector diff = _dragStart - e.GetPosition(null);
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    DragDrop.DoDragDrop(this, _dragSource, DragDropEffects.Move);
                }
            }
        }
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (!_isDragging) return;

            LayoutItem sourceItem = e.Data.GetData(typeof(LayoutTab)) as LayoutItem ?? e.Data.GetData(typeof(LayoutGroup)) as LayoutItem ?? _dragSource;
            var targetElement = e.OriginalSource as FrameworkElement;
            var targetContainer = GetVisualParent<ContentPresenter>(targetElement);
            if (sourceItem is LayoutGroup lg)
            {
                lg.IsExpanded = false;
            }
            if (targetContainer != null && targetContainer.DataContext is LayoutItem targetItem)
            {
                LayoutGroup targetGrp = targetItem as LayoutGroup;
                if (targetItem is LayoutTab tTab && tTab.IsInGroup)
                {
                    targetGrp = tTab.ParentGroup;
                }

                // On s'assure que la flèche tourne bien autour de son centre
                DragIndicator.RenderTransformOrigin = new Point(0.5, 0.5);

                if (targetGrp != null && sourceItem is LayoutTab sTab)
                {
                    if (!targetGrp.Contains(sTab))
                    {
                        // 1. Si on cible un Groupe avec un onglet externe -> Flèche à -90 au DÉBUT du groupe
                        var groupContainer = targetItem is LayoutGroup
                            ? targetContainer
                            : GetVisualParent<ContentPresenter>(VisualTreeHelper.GetParent(targetContainer));

                        var placementTarget = groupContainer ?? targetContainer;

                        DragIndicator.PlacementTarget = placementTarget;
                        DragIndicator.Placement = PlacementMode.Relative; // Plus simple pour caler à gauche

                        // On pivote la flèche (-90°) pour qu'elle pointe vers la droite
                        DragIndicator.RenderTransform = new RotateTransform(-90);

                        // On la place juste avant le groupe (-12px) et on la centre verticalement
                        DragIndicator.HorizontalOffset = -12;
                        DragIndicator.VerticalOffset = (placementTarget.ActualHeight / 2) + 4; // -4 car la flèche fait 8 de haut
                        DragIndicator.IsOpen = true;
                    }
                    else
                    {
                        // 2. Onglet interne au groupe -> Réorganisation classique (Flèche vers le bas)
                        DragIndicator.RenderTransform = new RotateTransform(0); // On réinitialise la rotation

                        Point pos = e.GetPosition(targetContainer);
                        bool isRightHalf = pos.X > targetContainer.ActualWidth / 2;

                        DragIndicator.PlacementTarget = targetContainer;
                        DragIndicator.Placement = PlacementMode.Top;
                        DragIndicator.HorizontalOffset = isRightHalf ? targetContainer.ActualWidth - 5 : -5;
                        DragIndicator.VerticalOffset = -2;
                        DragIndicator.IsOpen = true;
                    }
                }
                else
                {
                    // 3. Indicateur vertical classique (Flèche vers le bas)
                    DragIndicator.RenderTransform = new RotateTransform(0); // On réinitialise la rotation

                    Point pos = e.GetPosition(targetContainer);
                    bool isRightHalf = pos.X > targetContainer.ActualWidth / 2;

                    DragIndicator.PlacementTarget = targetContainer;
                    DragIndicator.Placement = PlacementMode.Top;
                    DragIndicator.HorizontalOffset = isRightHalf ? targetContainer.ActualWidth - 5 : -5;
                    DragIndicator.VerticalOffset = -2;
                    DragIndicator.IsOpen = true;
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
            base.OnDrop(e);
            DragIndicator.IsOpen = false;

            LayoutItem sourceItem = e.Data.GetData(typeof(LayoutTab)) as LayoutItem ?? e.Data.GetData(typeof(LayoutGroup)) as LayoutItem ?? _dragSource;

            if (sourceItem != null)
            {
                var targetElement = e.OriginalSource as FrameworkElement;
                var targetContainer = GetVisualParent<ContentPresenter>(targetElement);
                var targetParentContainer = GetVisualParent<ContentPresenter>(targetContainer);
                if (targetContainer?.DataContext is LayoutItem targetItem)
                {
                    LayoutGroup targetGrp =
                        targetParentContainer.DataContext as LayoutGroup ??
                        targetContainer.DataContext as LayoutGroup;

                    if (targetGrp != null && sourceItem is LayoutTab sTab)
                    {
                        if (!targetGrp.Contains(sTab))
                        {
                            // Si on cible un Groupe avec un onglet externe 
                            RemoveFromAll(sTab);
                            targetGrp.Add(sTab);
                            targetGrp.IsExpanded = true;
                        }
                        else if (targetGrp.Contains(sTab) && targetItem is LayoutTab tTab)
                        {
                            // Si on cible un Groupe avec un onglet interne 
                            var grp = sTab.ParentGroup;
                            int oldIndex = grp.IndexOf(sTab);
                            int newIndex = grp.IndexOf(tTab);

                            if (targetContainer != null && e.GetPosition(targetContainer).X > targetContainer.ActualWidth / 2) newIndex++;
                            if (oldIndex < newIndex) newIndex--;

                            grp.Remove(sTab);
                            grp.Insert(newIndex, sTab);
                        }
                    }
                    else
                    {
                        // Réorganisation classique racine
                        RemoveFromAll(sourceItem);
                        int newIndex = Items.IndexOf(targetItem);
                        
                        if (targetContainer != null && e.GetPosition(targetContainer).X > targetContainer.ActualWidth / 2) newIndex++;

                        newIndex = Math.Max(0, Math.Min(newIndex, Items.Count));
                        Items.Insert(newIndex, sourceItem);
                        
                    }
                }

            }







            //LayoutItem sourceItem = e.Data.GetData(typeof(LayoutTab)) as LayoutItem ?? e.Data.GetData(typeof(LayoutGroup)) as LayoutItem ?? _dragSource;

            //if (sourceItem != null)
            //{
            //    var targetElement = e.OriginalSource as FrameworkElement;
            //    var targetContainer = GetVisualParent<ContentPresenter>(targetElement);
            //    var targetItem = targetContainer?.DataContext as LayoutItem;

            //    // Cas A : Ajout à la fin d'un groupe si la source vient de l'extérieur
            //    if (sourceItem is LayoutTab sourceTab && targetItem is LayoutGroup targetGroup)
            //    {
            //        if (!targetGroup.Contains(sourceTab))
            //        {
            //            RemoveFromAll(sourceTab);
            //            targetGroup.Add(sourceTab);
            //            targetGroup.IsExpanded = true;
            //        }
            //    }
            //    // Cas B : Réorganisation à l'intérieur du MÊME groupe
            //    else if (sourceItem is LayoutTab sTab && targetItem is LayoutTab tTab && tTab.IsInGroup)
            //    {
            //        if (sTab.ParentGroup == tTab.ParentGroup)
            //        {
            //            var grp = sTab.ParentGroup;
            //            int oldIndex = grp.IndexOf(sTab);
            //            int newIndex = grp.IndexOf(tTab);

            //            if (targetContainer != null && e.GetPosition(targetContainer).X > targetContainer.ActualWidth / 2) newIndex++;
            //            if (oldIndex < newIndex) newIndex--;

            //            grp.Remove(sTab);
            //            grp.Insert(newIndex, sTab);
            //        }
            //    }
            //    // Cas C : Réorganisation classique racine
            //    else if (targetItem != null && targetItem != sourceItem && !targetItem.IsPinned)
            //    {
            //        RemoveFromAll(sourceItem);
            //        int newIndex = Items.IndexOf(targetItem);

            //        if (targetContainer != null && e.GetPosition(targetContainer).X > targetContainer.ActualWidth / 2) newIndex++;

            //        newIndex = Math.Max(0, Math.Min(newIndex, Items.Count));
            //        Items.Insert(newIndex, sourceItem);
            //    }
            //}

            _dragSource = null;
            _isDragging = false;
        }

        // Utilitaire pour nettoyer un item de toute collection existante avant déplacement
        private void RemoveFromAll(LayoutItem item)
        {
            Items.Remove(item);
            PinnedItems.Remove(item);
            if (item is LayoutTab tab && tab.ParentGroup != null)
            {
                tab.ParentGroup.Remove(tab);
            }
        }

        private T GetVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return GetVisualParent<T>(parentObject);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            _dragSource = null;
            _isDragging = false;
            DragIndicator.IsOpen = false;
        }
    }

    public abstract class LayoutItem : INotifyPropertyChanged
    {
        private string _title;
        private bool _isPinned;
        private bool _isCurrent;

        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

        // Pinned ne devrait idéalement être changé que via une méthode qui bascule de collection,
        // mais sa simple présence dans le constructeur suffira pour initier l'UI avec l'approche 2 collections
        public bool IsPinned { get => _isPinned; set { _isPinned = value; OnPropertyChanged(); } }
        public bool IsCurrent { get => _isCurrent; set { _isCurrent = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class LayoutTab : LayoutItem
    {
        private LayoutGroup _parentGroup;

        // 5. NOTIFICATION : Assure-toi de notifier IsInGroup quand le ParentGroup change
        public LayoutGroup ParentGroup
        {
            get => _parentGroup;
            set
            {
                if (_parentGroup != value)
                {
                    _parentGroup = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsInGroup));
                }
            }
        }

        public bool IsInGroup => ParentGroup != null;
    }

    public class LayoutGroup : LayoutItem
    {
        public LayoutGroup()
        {
            SubTabs = new ReadOnlyObservableCollection<LayoutTab>(_subTabs);
        }


        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LayoutTab> _subTabs { get; } = new ObservableCollection<LayoutTab>();
        public ReadOnlyObservableCollection<LayoutTab> SubTabs { get; }
        public void Add(LayoutTab tab)
        {
            tab.ParentGroup = this;
            _subTabs.Add(tab);
        }
        public void Insert(int index, LayoutTab tab)
        {
            tab.ParentGroup = this;
            _subTabs.Insert(index, tab);
        }

        public bool Remove(LayoutTab tab)
        {
            tab.ParentGroup = null;
            return _subTabs.Remove(tab);
        }

        public bool Contains(LayoutTab tab)
        {
            return _subTabs.Contains(tab);
        }

        public int IndexOf(LayoutTab tab)
        {
            return _subTabs.IndexOf(tab);
        }

    }
}