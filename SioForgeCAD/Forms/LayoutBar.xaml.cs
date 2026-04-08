    using System;
    using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

namespace SioForgeCAD.Forms
{
    public partial class LayoutBar : UserControl
    {
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

            // Ajout de tests
            Items.Add(new LayoutTab { Title = "Model" });
            Items.Add(new LayoutTab { Title = "Plan RDC" });

            var group = new LayoutGroup { Title = "Feuilles" };
            group.SubTabs.Add(new LayoutTab { Title = "Coupe AA" });
            group.SubTabs.Add(new LayoutTab { Title = "Coupe BB" });
            Items.Add(group);

        }

        // --- CLICS & SELECTION ---

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is LayoutItem clickedItem)
            {
                // Si c'est un groupe, on affiche ses sous-onglets
                if (clickedItem is LayoutGroup group)
                {
                    GroupPopupList.ItemsSource = group.SubTabs;
                    GroupPopup.PlacementTarget = sender as UIElement;
                    GroupPopup.IsOpen = true;
                }
                // Si c'est un onglet normal, on l'active
                else
                {
                    SetCurrentItem(clickedItem);
                }

                // Préparation Drag & Drop
                _dragStart = e.GetPosition(null);
                _dragSource = clickedItem;
                _isDragging = false;
            }
        }

        private void SetCurrentItem(LayoutItem item)
        {
            if (_currentItem != null) _currentItem.IsCurrent = false;
            _currentItem = item;
            if (_currentItem != null)
            {
                _currentItem.IsCurrent = true;
                ScrollToItem(item); // SCROLL AUTOMATIQUE
            }
        }

        // Permet de scroller jusqu'à l'onglet sélectionné
        private void ScrollToItem(LayoutItem item)
        {
            // On récupère le conteneur UI généré pour cet onglet (le ContentPresenter)
            var container = TabItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            container?.BringIntoView();
        }

        private void GroupSubTab_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is LayoutTab clickedTab)
            {
                SetCurrentItem(clickedTab);
                GroupPopup.IsOpen = false; // Ferme le popup
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
            var newTab = new LayoutTab { Title = $"Layout {Items.Count + 1}" };
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
            if (e.LeftButton == MouseButtonState.Pressed && _dragSource != null && !_isDragging)
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

        // AFFICHER LA FLÈCHE AU SURVOL
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (!_isDragging) return;

            var targetElement = e.OriginalSource as FrameworkElement;
            var targetContainer = GetVisualParent<ContentPresenter>(targetElement);

            if (targetContainer != null && targetContainer.DataContext is LayoutItem)
            {
                Point pos = e.GetPosition(targetContainer);
                // On détermine si on est sur la moitié gauche ou droite de l'onglet ciblé
                bool isRightHalf = pos.X > targetContainer.ActualWidth / 2;

                DragIndicator.PlacementTarget = targetContainer;
                DragIndicator.Placement = PlacementMode.Top;

                // On place la flèche à gauche ou à droite du conteneur (en compensant la marge négative)
                DragIndicator.HorizontalOffset = isRightHalf ? targetContainer.ActualWidth : 0;               
                DragIndicator.IsOpen = true;
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
            DragIndicator.IsOpen = false; // Cacher la flèche

            // 1. Récupérer l'élément source (peut être un LayoutTab OU un LayoutGroup)
            // On essaie d'extraire les deux types possibles, avec un fallback sur _dragSource
            LayoutItem sourceItem = e.Data.GetData(typeof(LayoutTab)) as LayoutItem
                                 ?? e.Data.GetData(typeof(LayoutGroup)) as LayoutItem
                                 ?? _dragSource;

            if (sourceItem != null)
            {
                var targetElement = e.OriginalSource as FrameworkElement;
                var targetContainer = GetVisualParent<ContentPresenter>(targetElement);
                var targetItem = targetContainer?.DataContext as LayoutItem;

                // 2. CAS A : On drop un TAB spécifiquement sur un GROUPE (Insertion dans le groupe)
                if (sourceItem is LayoutTab sourceTab && targetItem is LayoutGroup targetGroup)
                {
                    Items.Remove(sourceTab);
                    if (!targetGroup.SubTabs.Contains(sourceTab))
                    {
                        targetGroup.SubTabs.Add(sourceTab);
                    }
                }
                // 3. CAS B : Réorganisation classique (Groupe ou Tab déplacé dans la barre principale)
                else if (targetItem != null && targetItem != sourceItem)
                {
                    int oldIndex = Items.IndexOf(sourceItem);
                    int newIndex = Items.IndexOf(targetItem);

                    // Si on a droppé sur la moitié droite du conteneur cible, on insère APRÈS
                    if (targetContainer != null)
                    {
                        Point pos = e.GetPosition(targetContainer);
                        if (pos.X > targetContainer.ActualWidth / 2) newIndex++;
                    }

                    // Ajustement de l'index si on déplace un élément de gauche à droite
                    if (oldIndex != -1 && oldIndex < newIndex) newIndex--;

                    // On retire l'élément de son ancienne position
                    if (Items.Contains(sourceItem))
                    {
                        Items.Remove(sourceItem);
                    }

                    // Sécurité pour rester dans les limites de la collection
                    newIndex = Math.Max(0, Math.Min(newIndex, Items.Count));
                    Items.Insert(newIndex, sourceItem);
                }
            }

            // Réinitialisation des variables de drag
            _dragSource = null;
            _isDragging = false;
        }

        // Fonction utilitaire pour remonter l'arbre visuel jusqu'au ContentPresenter
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

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set { _isPinned = value; OnPropertyChanged(); }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class LayoutTab : LayoutItem
    {
        // Tu peux rajouter des propriétés spécifiques à tes Layouts AutoCAD ici
    }

    public class LayoutGroup : LayoutItem
    {
        public ObservableCollection<LayoutTab> SubTabs { get; } = new ObservableCollection<LayoutTab>();
    }
}