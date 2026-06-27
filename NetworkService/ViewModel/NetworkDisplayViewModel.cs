using NetworkService.Commands;
using NetworkService.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace NetworkService.ViewModel
{
    public class VentilTypeGroup : BaseViewModel
    {
        public string TypeName { get; set; }

        private ObservableCollection<Ventil> _items = new ObservableCollection<Ventil>();
        public ObservableCollection<Ventil> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(nameof(Items)); }
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }
    }

    public class NetworkItem : BaseViewModel
    {
        private Ventil _ventil;
        public Ventil Ventil
        {
            get => _ventil;
            set { _ventil = value; OnPropertyChanged(nameof(Ventil)); OnPropertyChanged(nameof(IsEmpty)); }
        }

        public bool IsEmpty => Ventil == null;

        private bool _isConnectSource;
        public bool IsConnectSource
        {
            get => _isConnectSource;
            set { _isConnectSource = value; OnPropertyChanged(nameof(IsConnectSource)); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public int Row { get; set; }
        public int Column { get; set; }
    }

    public class ConnectionLine : BaseViewModel
    {
        public Ventil From { get; set; }
        public Ventil To { get; set; }
        public string Label => $"{From?.Name} ↔ {To?.Name}";
    }

    public class NetworkDisplayViewModel : BaseViewModel
    {
        public ObservableCollection<NetworkItem> NetworkItems { get; set; }
        public ObservableCollection<VentilTypeGroup> TreeViewGroups { get; set; }
        public ObservableCollection<ConnectionLine> Connections { get; set; }

        private Ventil _selectedTreeItem;
        public Ventil SelectedTreeItem
        {
            get => _selectedTreeItem;
            set { _selectedTreeItem = value; OnPropertyChanged(nameof(SelectedTreeItem)); }
        }

        private NetworkItem _selectedNetworkItem;
        public NetworkItem SelectedNetworkItem
        {
            get => _selectedNetworkItem;
            set
            {
                if (_selectedNetworkItem != null)
                    _selectedNetworkItem.IsSelected = false;
                _selectedNetworkItem = value;
                if (_selectedNetworkItem != null)
                    _selectedNetworkItem.IsSelected = true;
                OnPropertyChanged(nameof(SelectedNetworkItem));
            }
        }

        private int _connectSourceIndex = -1;

        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            private set { _isConnecting = value; OnPropertyChanged(nameof(IsConnecting)); }
        }

        private string _connectHint = "";
        public string ConnectHint
        {
            get => _connectHint;
            set { _connectHint = value; OnPropertyChanged(nameof(ConnectHint)); }
        }

        public ICommand DropCommand { get; set; }
        public ICommand RemoveFromGridCommand { get; set; }
        public ICommand AutoArrangeCommand { get; set; }
        public ICommand ToggleTreeCommand { get; set; }

        public NetworkDisplayViewModel()
        {
            NetworkItems = new ObservableCollection<NetworkItem>();
            for (int i = 0; i < 12; i++)
                NetworkItems.Add(new NetworkItem { Row = i / 4, Column = i % 4 });

            TreeViewGroups = new ObservableCollection<VentilTypeGroup>();
            foreach (var ventil in MainWindowViewModel.Ventili)
                AddToTreeView(ventil);

            Connections = new ObservableCollection<ConnectionLine>();

            MainWindowViewModel.Ventili.CollectionChanged += OnMainVentiliChanged;

            DropCommand = new RelayCommand(Drop);
            RemoveFromGridCommand = new RelayCommand(RemoveFromGrid, _ => SelectedNetworkItem?.Ventil != null);
            AutoArrangeCommand = new RelayCommand(AutoArrange);
            ToggleTreeCommand = new RelayCommand(_ => ToggleTree());
        }

        private void ToggleTree()
        {
            bool anyCollapsed = TreeViewGroups.Any(g => !g.IsExpanded);
            foreach (var g in TreeViewGroups)
                g.IsExpanded = anyCollapsed;
        }

        public void MoveItem(NetworkItem source, NetworkItem target)
        {
            var ventil = source.Ventil;
            if (ventil == null) return;

            source.Ventil = null;
            target.Ventil = ventil;

            MainWindowViewModel.AddHistory($"Premešten {ventil.Name} → ({target.Row},{target.Column})");
            MainWindowViewModel.PushUndo(() =>
            {
                target.Ventil = null;
                source.Ventil = ventil;
                MainWindowViewModel.AddHistory($"Undo: Vraćen {ventil.Name} na originalnu poziciju");
            });
        }

        private void OnMainVentiliChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Ventil ventil in e.NewItems)
                    AddToTreeView(ventil);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Ventil ventil in e.OldItems)
                {
                    RemoveFromTreeView(ventil);

                    var cell = NetworkItems.FirstOrDefault(ni => ni.Ventil == ventil);
                    if (cell != null)
                    {
                        RemoveConnectionsFor(ventil);
                        cell.Ventil = null;
                    }
                }
            }
        }

        private void AddToTreeView(Ventil ventil)
        {
            var group = TreeViewGroups.FirstOrDefault(g => g.TypeName == ventil.Type?.Name);
            if (group == null)
            {
                group = new VentilTypeGroup { TypeName = ventil.Type?.Name ?? "Nepoznat tip" };
                TreeViewGroups.Add(group);
            }
            if (!group.Items.Contains(ventil))
                group.Items.Add(ventil);
        }

        private void RemoveFromTreeView(Ventil ventil)
        {
            foreach (var group in TreeViewGroups)
            {
                if (group.Items.Remove(ventil))
                    break;
            }
            var emptyGroups = TreeViewGroups.Where(g => g.Items.Count == 0).ToList();
            foreach (var g in emptyGroups) TreeViewGroups.Remove(g);
        }

        private IEnumerable<Ventil> GetAllTreeViewVentils()
        {
            foreach (var group in TreeViewGroups)
                foreach (var ventil in group.Items)
                    yield return ventil;
        }

        private void Drop(object parameter)
        {
            var targetCell = parameter as NetworkItem;
            if (targetCell == null || !targetCell.IsEmpty || SelectedTreeItem == null) return;

            var ventil = SelectedTreeItem;
            targetCell.Ventil = ventil;
            RemoveFromTreeView(ventil);

            MainWindowViewModel.AddHistory($"Prevučen {ventil.Name} → ({targetCell.Row},{targetCell.Column})");
            MainWindowViewModel.PushUndo(() =>
            {
                targetCell.Ventil = null;
                AddToTreeView(ventil);
                RemoveConnectionsFor(ventil);
                MainWindowViewModel.AddHistory($"Undo: Uklonjen {ventil.Name} sa mreže");
            });

            SelectedTreeItem = null;
        }

        private void RemoveFromGrid(object parameter)
        {
            if (SelectedNetworkItem?.Ventil == null) return;

            var ventil = SelectedNetworkItem.Ventil;
            var cell = SelectedNetworkItem;

            RemoveConnectionsFor(ventil);
            cell.Ventil = null;
            AddToTreeView(ventil);

            MainWindowViewModel.AddHistory($"Uklonjen {ventil.Name} sa mreže");
            MainWindowViewModel.PushUndo(() =>
            {
                cell.Ventil = ventil;
                RemoveFromTreeView(ventil);
                MainWindowViewModel.AddHistory($"Undo: Vraćen {ventil.Name} na mrežu");
            });
        }

        private void AutoArrange(object parameter)
        {
            var ventilsToPlace = GetAllTreeViewVentils().ToList();
            foreach (var ventil in ventilsToPlace)
            {
                var freeCell = NetworkItems.FirstOrDefault(ni => ni.IsEmpty);
                if (freeCell == null) break;
                freeCell.Ventil = ventil;
                RemoveFromTreeView(ventil);
                MainWindowViewModel.AddHistory($"Auto: {ventil.Name} → ({freeCell.Row},{freeCell.Column})");
            }
        }

        public void HandleConnectionClick(int clickedIndex)
        {
            if (clickedIndex < 0 || clickedIndex >= NetworkItems.Count) return;
            var clickedVentil = NetworkItems[clickedIndex].Ventil;
            if (clickedVentil == null) return;

            if (_connectSourceIndex < 0)
            {
                _connectSourceIndex = clickedIndex;
                NetworkItems[clickedIndex].IsConnectSource = true;
                ConnectHint = $"Odabran: {clickedVentil.Name} — Shift+klik na drugi (veži / odveži)";
                IsConnecting = true;
            }
            else
            {
                var sourceVentil = NetworkItems[_connectSourceIndex].Ventil;
                NetworkItems[_connectSourceIndex].IsConnectSource = false;

                if (clickedIndex != _connectSourceIndex && sourceVentil != null)
                    ToggleConnection(sourceVentil, clickedVentil);

                _connectSourceIndex = -1;
                ConnectHint = "";
                IsConnecting = false;
            }
        }

        public void ToggleConnection(Ventil from, Ventil to)
        {
            bool exists = Connections.Any(c =>
                (c.From == from && c.To == to) || (c.From == to && c.To == from));
            if (exists)
                RemoveConnection(from, to);
            else
                AddConnection(from, to);
        }

        public void RemoveConnection(Ventil from, Ventil to)
        {
            var conn = Connections.FirstOrDefault(c =>
                (c.From == from && c.To == to) || (c.From == to && c.To == from));
            if (conn == null) return;

            Connections.Remove(conn);
            MainWindowViewModel.AddHistory($"Uklonjena linija {from.Name} ↔ {to.Name}");
            MainWindowViewModel.PushUndo(() =>
            {
                bool stillExists = Connections.Any(c =>
                    (c.From == from && c.To == to) || (c.From == to && c.To == from));
                if (!stillExists)
                    Connections.Add(new ConnectionLine { From = from, To = to });
                MainWindowViewModel.AddHistory($"Undo: Vraćena linija {from.Name} ↔ {to.Name}");
            });
        }

        public void AddConnection(Ventil from, Ventil to)
        {
            bool exists = Connections.Any(c =>
                (c.From == from && c.To == to) ||
                (c.From == to && c.To == from));
            if (exists) return;

            Connections.Add(new ConnectionLine { From = from, To = to });
            MainWindowViewModel.AddHistory($"Linija {from.Name} ↔ {to.Name}");
            MainWindowViewModel.PushUndo(() =>
            {
                var conn = Connections.FirstOrDefault(c =>
                    (c.From == from && c.To == to) || (c.From == to && c.To == from));
                if (conn != null) Connections.Remove(conn);
                MainWindowViewModel.AddHistory($"Undo: Uklonjena linija {from.Name} ↔ {to.Name}");
            });
        }

        public void RemoveConnectionsFor(Ventil ventil)
        {
            var toRemove = Connections.Where(c => c.From == ventil || c.To == ventil).ToList();
            foreach (var c in toRemove)
                Connections.Remove(c);
        }

        public int GetCellIndexOf(Ventil ventil)
        {
            if (ventil == null) return -1;
            for (int i = 0; i < NetworkItems.Count; i++)
                if (NetworkItems[i].Ventil == ventil) return i;
            return -1;
        }
    }
}
