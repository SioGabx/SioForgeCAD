using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SioForgeCAD.Forms
{
    public partial class LayoutBar : UserControl
    {
        public ObservableCollection<LayoutItem> Items { get; } = new ObservableCollection<LayoutItem>();
        private LayoutItem _currentItem;

        // Variables Drag & Drop
        private Point _dragStart;
        private bool _isDragging;
        private LayoutItem _dragSource;

        public LayoutBar()
        {
            InitializeComponent();
            LayoutBarRoot.DataContext = this;

            // Détection de l'overflow
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

        // --- SÉLECTION ---

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is LayoutItem clickedItem)
            {
                SetCurrentItem(clickedItem);

                // Initialisation du Drag & Drop
                _dragStart = e.GetPosition(null);
                _dragSource = clickedItem;
                _isDragging = false;
            }
        }

        private void SetCurrentItem(LayoutItem item)
        {
            if (_currentItem != null) _currentItem.IsCurrent = false;
            _currentItem = item;
            if (_currentItem != null) _currentItem.IsCurrent = true;
        }

        private void OverflowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem)?.DataContext is LayoutTab clickedTab)
            {
                SetCurrentItem(clickedTab);
                OverflowToggle.IsChecked = false; // Ferme le popup
            }
        }

        // --- AJOUT ET ÉPINGLAGE ---

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var newTab = new LayoutTab { Title = $"Layout {Items.Count + 1}" };
            Items.Add(newTab);
            SetCurrentItem(newTab);

            // Scroll tout à droite
            TabScrollViewer.ScrollToRightEnd();
        }

        public void PinTab(LayoutTab tab)
        {
            tab.IsPinned = !tab.IsPinned;
            if (Items.Contains(tab))
            {
                Items.Remove(tab);
                // Si épinglé, on met au début (index 0). Sinon, on met à la fin ou après les épinglés.
                if (tab.IsPinned) Items.Insert(0, tab);
                else Items.Add(tab);
            }
        }

        // --- SCROLL A LA MOLETTE ---
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

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            _dragSource = null;
            _isDragging = false;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetData(typeof(LayoutTab)) is LayoutTab sourceTab)
            {
                // Trouver la cible sur laquelle on a relâché
                var targetElement = e.OriginalSource as FrameworkElement;
                var targetItem = targetElement?.DataContext as LayoutItem;

                if (targetItem is LayoutGroup targetGroup)
                {
                    // Déplacement d'un Tab vers un Groupe !
                    Items.Remove(sourceTab);
                    targetGroup.SubTabs.Add(sourceTab);
                }
                else if (targetItem is LayoutTab targetTab && targetTab != sourceTab)
                {
                    // Réorganisation classique
                    int index = Items.IndexOf(targetTab);
                    Items.Remove(sourceTab);
                    Items.Insert(index, sourceTab);
                }
            }
            _dragSource = null;
            _isDragging = false;
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