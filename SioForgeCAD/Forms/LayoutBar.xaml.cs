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
    /// <summary>
    /// Barre de layouts style AutoCAD avec support de groupes expand/collapse.
    ///
    /// UTILISATION :
    ///   var bar = new LayoutBar();
    ///   bar.FreeTabs.Add(new LayoutTabItem { Title = "Model" });
    ///
    ///   var grp = new LayoutTabGroup { Title = "Feuilles A" };
    ///   grp.Items.Add(new LayoutTabItem { Title = "Plan RDC" });
    ///   grp.Items.Add(new LayoutTabItem { Title = "Coupe AA" });
    ///   bar.Groups.Add(grp);
    ///
    ///   bar.TabActivated += (sender, tab) => { /* changer layout AutoCAD */ };
    ///   bar.NewLayoutRequested += (sender, e) => { /* créer layout */ };
    /// </summary>
    public partial class LayoutBar : UserControl
    {
        // ─── EVENTS ───────────────────────────────────────────────────────
        /// <summary>Déclenché quand l'utilisateur clique sur un tab.</summary>
        public event EventHandler<LayoutTabItem> TabActivated;

        /// <summary>Déclenché quand l'utilisateur clique sur le bouton "+".</summary>
        public event EventHandler NewLayoutRequested;

        /// <summary>Déclenché sur clic droit d'un tab.</summary>
        public event EventHandler<LayoutTabItem> TabRightClicked;

        // ─── COLLECTIONS PUBLIQUES ────────────────────────────────────────
        /// <summary>Tabs libres (non groupés), affichés en premier.</summary>
        public ObservableCollection<LayoutTabItem> FreeTabs { get; } = new ObservableCollection<LayoutTabItem>();

        /// <summary>Groupes de tabs.</summary>
        public ObservableCollection<LayoutTabGroup> Groups { get; } = new ObservableCollection<LayoutTabGroup>();

        // ─── SUIVI DU TAB COURANT ─────────────────────────────────────────
        private LayoutTabItem _currentTab;

        public LayoutTabItem CurrentTab
        {
            get => _currentTab;
            set => SetCurrentTab(value);
        }

        // ─── CONSTRUCTEUR ─────────────────────────────────────────────────
        public LayoutBar()
        {
            InitializeComponent();
            FreeTabsControl.ItemsSource = FreeTabs;
            GroupsControl.ItemsSource = Groups;
        }

        // ─── API PUBLIQUE ─────────────────────────────────────────────────

        /// <summary>
        /// Crée et ajoute un tab libre (non groupé).
        /// </summary>
        public LayoutTabItem AddFreeTab(string title, bool makeCurrent = false)
        {
            var tab = new LayoutTabItem { Title = title };
            FreeTabs.Add(tab);
            if (makeCurrent) SetCurrentTab(tab);
            return tab;
        }

        /// <summary>
        /// Crée un groupe avec les titres fournis et l'ajoute à la barre.
        /// </summary>
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

        /// <summary>
        /// Ajoute un tab à un groupe existant.
        /// </summary>
        public LayoutTabItem AddTabToGroup(LayoutTabGroup group, string title, bool makeCurrent = false)
        {
            var tab = new LayoutTabItem { Title = title, Group = group };
            group.Items.Add(tab);
            if (makeCurrent) SetCurrentTab(tab);
            return tab;
        }

        /// <summary>
        /// Supprime un tab (libre ou dans un groupe).
        /// </summary>
        public void RemoveTab(LayoutTabItem tab)
        {
            if (tab == null) return;

            if (tab.Group != null)
            {
                tab.Group.Items.Remove(tab);
            }
            else
            {
                FreeTabs.Remove(tab);
            }

            // Si c'était le tab courant, sélectionner le premier disponible
            if (_currentTab == tab)
            {
                var first = AllTabs().FirstOrDefault();
                if (first != null) SetCurrentTab(first);
                else _currentTab = null;
            }
        }

        /// <summary>
        /// Supprime un groupe et tous ses tabs.
        /// </summary>
        public void RemoveGroup(LayoutTabGroup group)
        {
            if (group == null) return;
            bool currentWasInGroup = group.Items.Contains(_currentTab);
            Groups.Remove(group);

            if (currentWasInGroup)
            {
                var first = AllTabs().FirstOrDefault();
                if (first != null) SetCurrentTab(first);
                else _currentTab = null;
            }
        }

        /// <summary>
        /// Retourne tous les tabs (libres + dans tous les groupes).
        /// </summary>
        public System.Collections.Generic.IEnumerable<LayoutTabItem> AllTabs()
        {
            foreach (var t in FreeTabs) yield return t;
            foreach (var g in Groups)
                foreach (var t in g.Items)
                    yield return t;
        }

        // ─── LOGIQUE INTERNE ──────────────────────────────────────────────

        private void SetCurrentTab(LayoutTabItem tab)
        {
            // Désélectionner l'ancien
            if (_currentTab != null)
            {
                _currentTab.IsCurrent = false;

                // Mettre à jour IsCurrent du groupe parent
                if (_currentTab.Group != null)
                    _currentTab.Group.IsCurrent = false;
            }

            _currentTab = tab;

            // Sélectionner le nouveau
            if (_currentTab != null)
            {
                _currentTab.IsCurrent = true;

                // Mettre à jour IsCurrent du groupe parent
                if (_currentTab.Group != null)
                    _currentTab.Group.IsCurrent = true;
            }
        }

        // ─── EVENT HANDLERS ───────────────────────────────────────────────

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem lbi && lbi.DataContext is LayoutTabItem tab)
            {
                SetCurrentTab(tab);
                TabActivated?.Invoke(this, tab);
                e.Handled = true;
            }
        }

        private void TabItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem lbi && lbi.DataContext is LayoutTabItem tab)
            {
                TabRightClicked?.Invoke(this, tab);
                e.Handled = true;
            }
        }

        private void NewLayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            NewLayoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }



    /// <summary>
    /// Représente un item de layout (une présentation AutoCAD).
    /// </summary>
    public class LayoutTabItem : INotifyPropertyChanged
    {
        private string _title;
        private bool _isCurrent;
        private bool _isEditable = true;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        public bool IsEditable
        {
            get => _isEditable;
            set { _isEditable = value; OnPropertyChanged(); }
        }

        /// <summary>Groupe parent (null si aucun groupe)</summary>
        public LayoutTabGroup Group { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Représente un groupe de layouts. Peut être expand/collapse.
    /// </summary>
    public class LayoutTabGroup : INotifyPropertyChanged
    {
        private string _title;
        private bool _isExpanded = true;
        private bool _isCurrent;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

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

        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        public string ExpandIcon => IsExpanded ? "▾" : "▸";

        public ObservableCollection<LayoutTabItem> Items { get; } = new ObservableCollection<LayoutTabItem>();

        public ICommand ToggleExpandCommand { get; }

        public LayoutTabGroup()
        {
            ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Simple ICommand relay.</summary>
    public class RelayCommand : ICommand
    {
        private readonly System.Action<object> _execute;
        private readonly System.Func<object, bool> _canExecute;

        public RelayCommand(System.Action<object> execute, System.Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event System.EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}




