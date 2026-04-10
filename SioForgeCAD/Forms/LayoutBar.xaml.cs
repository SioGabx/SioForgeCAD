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
            Items.Add(new LayoutTab { Title = "Plan R+1" });

            var group = new LayoutGroup { Title = "Feuilles" };
            group.Add(new LayoutTab { Title = "Coupe AA" });
            group.Add(new LayoutTab { Title = "Coupe BB" });
            Items.Add(group);

            Items.Add(new LayoutTab { Title = "Plan R+2" });
            Items.Add(new LayoutTab { Title = "Plan R+3" });
            Items.Add(new LayoutTab { Title = "Plan R+4" });
            Items.Add(new LayoutTab { Title = "Plan R+5" });
            Items.Add(new LayoutTab { Title = "Plan R+6" });
            Items.Add(new LayoutTab { Title = "Plan R+7" });
            Items.Add(new LayoutTab { Title = "Plan R+8" });
            Items.Add(new LayoutTab { Title = "Plan R+9" });
            Items.Add(new LayoutTab { Title = "Plan R+10" });
            Items.Add(new LayoutTab { Title = "Plan R+11" });
            Items.Add(new LayoutTab { Title = "Plan R+12" });
            Items.Add(new LayoutTab { Title = "Plan R+13" });
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
            }
        }

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            // Si on a relâché le clic SANS avoir bougé la souris (donc sans drag)
            if (!_isDragging && _dragSource != null)
            {
                // C'est un vrai "Clic" ! On exécute l'action.
                if (_dragSource is LayoutGroup group)
                {
                    group.IsExpanded = !group.IsExpanded;
                }
                else
                {
                    SetCurrentItem(_dragSource);
                }
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
            if (item == null) return;

            // Si l'élément est épinglé, il est toujours visible à gauche, pas besoin de scroller
            if (item is LayoutTab tab && tab.IsPinned) return;

            // On utilise le Dispatcher pour retarder légèrement l'exécution.
            // Cela garantit que si le LayoutGroup vient juste d'être mis à IsExpanded = true,
            // WPF a eu le temps de dessiner ses sous-onglets dans l'arbre visuel avant de scroller.
            Dispatcher.InvokeAsync(() =>
            {
                FrameworkElement container = null;

                container = TabItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null)
                {
                    container = GetVisualContainerFromItem(TabItemsControl, item);
                }

                if (container != null)
                {
                    ScrollTabIntoView(container);
                }

            }, System.Windows.Threading.DispatcherPriority.Loaded);

        }

        private void ScrollTabIntoView(FrameworkElement container)
        {
            if (container == null || TabScrollViewer == null || PinnedControl == null || TabItemsControl == null)
                return;

            try
            {
                // 1. Obtenir la position de l'élément par rapport au conteneur des onglets scrollables
                GeneralTransform transform = container.TransformToAncestor(TabItemsControl);
                Point itemPos = transform.Transform(new Point(0, 0));

                double itemLocalX = itemPos.X;
                double itemWidth = container.ActualWidth;

                // 2. Récupérer les dimensions utiles de notre layout
                double pinnedWidth = PinnedControl.ActualWidth;
                double viewportWidth = TabScrollViewer.ViewportWidth;
                double currentScroll = TabScrollViewer.HorizontalOffset;

                // 3. Calculer les positions absolues de l'élément dans la zone de défilement totale
                // On ajoute pinnedWidth car le TabItemsControl a une marge à gauche équivalente
                double itemExtentX = pinnedWidth + itemLocalX;
                double itemExtentRight = itemExtentX + itemWidth;

                // 4. Définir les limites de la zone REELLEMENT visible (non cachée par PinnedControl)
                double visibleLeft = currentScroll + pinnedWidth;
                double visibleRight = currentScroll + viewportWidth;

                // 5. Ajuster le scroll si l'élément dépasse
                if (itemExtentX < visibleLeft)
                {
                    // L'élément est caché à gauche (sous les épinglés) -> On l'aligne à gauche
                    TabScrollViewer.ScrollToHorizontalOffset(itemExtentX - pinnedWidth);
                }
                else if (itemExtentRight > visibleRight)
                {
                    // L'élément est caché à droite (hors de l'écran) -> On l'aligne à droite
                    TabScrollViewer.ScrollToHorizontalOffset(itemExtentRight - viewportWidth);
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore : Se produit si l'élément n'est pas encore rendu dans l'arbre visuel WPF
            }
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

                if (targetGrp != null && sourceItem is LayoutTab sTab && sourceItem != targetItem)
                {
                    if (!targetGrp.Contains(sTab))
                    {
                        // 1. Si on cible un Groupe avec un onglet externe -> Flèche à -90 au DÉBUT du groupe
                        var groupContainer = targetItem is LayoutGroup
                            ? targetContainer
                            : GetVisualParent<ContentPresenter>(VisualTreeHelper.GetParent(targetContainer));

                        var placementTarget = groupContainer ?? targetContainer;

                        // PlacementMode.Relative Plus simple pour caler à gauche
                        ShowDragIndicator(placementTarget, -12, (placementTarget.ActualHeight / 2) + 4, -90, PlacementMode.Relative);
                        Debug.WriteLine("Cible un Groupe avec un onglet externe");
                    }
                    else if (targetItem is LayoutGroup)
                    {
                        // Onglet interne déposé directement sur l'en-tête du groupe
                        var firstTabItem = targetGrp.SubTabs.Count > 0 ? targetGrp.SubTabs[0] : null;

                        if (firstTabItem != null)
                        {
                            FrameworkElement firstTabContainer = GetVisualContainerFromItem(targetContainer, firstTabItem);
                            var placementTarget = firstTabContainer ?? targetContainer;
                            ShowDragIndicator(placementTarget, -5, -2, 0);
                            Debug.WriteLine("Onglet interne sur le group tab -> Placé avant le 1er onglet");
                        }
                    }
                    else if (targetItem is LayoutTab targetTabInside)
                    {
                        // 2. Onglet interne au groupe -> Réorganisation classique (Flèche vers le bas)
                        int baseIndex = targetGrp.IndexOf(targetTabInside);
                        int sourceIndex = targetGrp.IndexOf(sTab); // On récupère l'index de la source dans le groupe

                        CalculateInsertionIndex(e, targetContainer, baseIndex, out bool isRightHalf);
                        bool isSameLocation = (!isRightHalf && sourceIndex == baseIndex - 1) ||
                                                                     (isRightHalf && sourceIndex == baseIndex + 1);

                        if (isSameLocation)
                        {
                            // On récupère le conteneur parent (le panel du groupe)
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

                        Debug.WriteLine("Onglet interne au groupe");
                    }
                }
                else if (Items.Contains(targetItem) && targetItem != sourceItem) // Condition simplifiée
                {
                    // 3. Indicateur vertical classique racine
                    int baseIndex = Items.IndexOf(targetItem);
                    int sourceIndex = Items.IndexOf(sourceItem);

                    var FuturLocationIndex = CalculateInsertionIndex(e, targetContainer, baseIndex, out bool isRightHalf);

                    // Un mouvement ne change rien si :
                    // - On cible la MOITIÉ GAUCHE de l'élément situé juste à DROITE de l'élément glissé
                    // - On cible la MOITIÉ DROITE de l'élément situé juste à GAUCHE de l'élément glissé
                    bool isSameLocation = (!isRightHalf && sourceIndex == baseIndex - 1) ||
                                          (isRightHalf && sourceIndex == baseIndex + 1);

                    if (isSameLocation)
                    {
                        // On cherche à partir du parent (qui contient TOUS les onglets), 
                        // ou simplement 'this' si ta classe courante est le conteneur global.
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

                    Debug.WriteLine("Reorder racine");
                }
            }
        }


        /// <summary>
        /// Calcule le nouvel index d'insertion basé sur la position de la souris par rapport au conteneur cible.
        /// </summary>
        private int CalculateInsertionIndex(DragEventArgs e, FrameworkElement targetContainer, int currentItemIndex, out bool isRightHalf)
        {
            isRightHalf = false;

            // Si l'index est invalide (ex: introuvable), on retourne 0 par sécurité
            if (currentItemIndex < 0)
            {
                Debug.WriteLine("index est invalide (ex: introuvable), on retourne 0 par sécurité");
                currentItemIndex = 0;
            }

            if (targetContainer != null)
            {
                Point pos = e.GetPosition(targetContainer);
                isRightHalf = pos.X > targetContainer.ActualWidth / 2;

                // Si on est sur la moitié droite, on insère APRÈS l'élément visé
                if (isRightHalf)
                {
                    currentItemIndex++;
                }
            }

            return currentItemIndex;
        }


        private void ShowDragIndicator(UIElement PlacementTarget, double HorizontalOffset, double VerticalOffset, double Rotation, PlacementMode placementMode = PlacementMode.Top)
        {
            DragIndicator.RenderTransformOrigin = new Point(0.5, 0.5);// On s'assure que la flèche tourne bien autour de son centre
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
            DragIndicator.IsOpen = false;

            LayoutItem sourceItem = e.Data.GetData(typeof(LayoutTab)) as LayoutItem ?? e.Data.GetData(typeof(LayoutGroup)) as LayoutItem ?? _dragSource;

            if (sourceItem != null)
            {
                var targetElement = e.OriginalSource as FrameworkElement;
                var targetContainer = GetVisualParent<ContentPresenter>(targetElement);
                var targetParentContainer = GetVisualParent<ContentPresenter>(targetContainer);

                if (targetContainer?.DataContext is LayoutItem targetItem)
                {
                    LayoutGroup targetGrp = targetParentContainer?.DataContext as LayoutGroup ?? targetContainer.DataContext as LayoutGroup;

                    if (targetGrp != null && sourceItem is LayoutTab sTab && sourceItem != targetItem)
                    {
                        if (!targetGrp.Contains(sTab))
                        {
                            // Si on cible un Groupe avec un onglet externe 
                            RemoveFromAll(sTab);
                            targetGrp.Insert(0, sTab); // BUGFIX : Insert à 0 pour correspondre à la flèche visuelle
                            targetGrp.IsExpanded = true;
                        }
                        else if (targetGrp.Contains(sTab) && targetItem is LayoutTab tTab)
                        {
                            // Si on cible un Groupe avec un onglet interne 
                            var grp = sTab.ParentGroup;
                            int oldIndex = grp.IndexOf(sTab);
                            int baseIndex = grp.IndexOf(tTab);

                            // Utilisation de la nouvelle méthode
                            int newIndex = CalculateInsertionIndex(e, targetContainer, baseIndex, out _);
                            Debug.WriteLine(newIndex);
                            // Compensation du décalage lors du retrait de l'item original
                            if (oldIndex < newIndex) newIndex--;

                            grp.Remove(sTab);
                            grp.Insert(newIndex, sTab);
                        }
                        else if (targetGrp.Contains(sTab) && targetItem is LayoutGroup)
                        {
                            // Ajout du cas manquant : Onglet interne déposé sur l'en-tête de son propre groupe
                            var grp = sTab.ParentGroup;
                            grp.Remove(sTab);
                            grp.Insert(0, sTab); // On le force au début, comme la flèche l'indique
                        }
                    }
                    else if (Items.IndexOf(targetItem) != Items.IndexOf(sourceItem))
                    {
                        // Réorganisation classique racine

                        RemoveFromAll(sourceItem);
                        int baseIndex = Items.IndexOf(targetItem);

                        // Utilisation de la nouvelle méthode
                        int newIndex = CalculateInsertionIndex(e, targetContainer, baseIndex, out _);

                        newIndex = Math.Max(0, Math.Min(newIndex, Items.Count));

                        Items.Insert(newIndex, sourceItem);
                    }
                }
            }

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

        private static T GetVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return GetVisualParent<T>(parentObject);
        }
        private FrameworkElement GetVisualContainerFromItem(DependencyObject parent, object itemData)
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Si l'enfant est un FrameworkElement et que sa donnée correspond au Tab recherché
                if (child is FrameworkElement fe && fe.DataContext == itemData)
                {
                    // On s'assure qu'on ne retourne pas le groupe lui-même par accident
                    if (!(fe.DataContext is LayoutGroup))
                    {
                        return fe;
                    }
                }

                // Sinon, on cherche plus profondément
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
            //_dragSource = null;
            //_isDragging = false;
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