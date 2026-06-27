using NetworkService.Commands;
using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace NetworkService.ViewModel
{
    public class NetworkEntitiesViewModel : BaseViewModel
    {
        private ObservableCollection<Ventil> _filteredVentili;
        public ObservableCollection<Ventil> FilteredVentili
        {
            get => _filteredVentili;
            set { _filteredVentili = value; OnPropertyChanged(nameof(FilteredVentili)); }
        }

        private Ventil _selectedVentil;
        public Ventil SelectedVentil
        {
            get => _selectedVentil;
            set
            {
                _selectedVentil = value;
                OnPropertyChanged(nameof(SelectedVentil));
                OnPropertyChanged(nameof(HasSelection));
            }
        }

        public bool HasSelection => _selectedVentil != null;

        private string _newName;
        public string NewName
        {
            get => _newName;
            set { _newName = value; OnPropertyChanged(nameof(NewName)); if (_nameTouched) ValidateName(); }
        }

        private EntityType _newType;
        public EntityType NewType
        {
            get => _newType;
            set { _newType = value; OnPropertyChanged(nameof(NewType)); if (_typeTouched) ValidateType(); }
        }

        private bool _nameTouched;
        private bool _typeTouched;

        public int NextId => MainWindowViewModel.Ventili.Count == 0 ? 1
            : MainWindowViewModel.Ventili.Max(v => v.Id) + 1;

        public ObservableCollection<EntityType> EntityTypes { get; set; }
        public ObservableCollection<string> TypeFilterOptions { get; set; }

        private string _nameError;
        public string NameError
        {
            get => _nameError;
            set { _nameError = value; OnPropertyChanged(nameof(NameError)); OnPropertyChanged(nameof(HasNameError)); }
        }

        public bool HasNameError => !string.IsNullOrEmpty(_nameError);

        private string _typeError;
        public string TypeError
        {
            get => _typeError;
            set { _typeError = value; OnPropertyChanged(nameof(TypeError)); OnPropertyChanged(nameof(HasTypeError)); }
        }

        public bool HasTypeError => !string.IsNullOrEmpty(_typeError);

        private string _selectedTypeFilter = "Svi tipovi";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set { _selectedTypeFilter = value; OnPropertyChanged(nameof(SelectedTypeFilter)); }
        }

        private string _idFilterValue;
        public string IdFilterValue
        {
            get => _idFilterValue;
            set { _idFilterValue = value; OnPropertyChanged(nameof(IdFilterValue)); }
        }

        private bool _filterLess;
        public bool FilterLess
        {
            get => _filterLess;
            set { _filterLess = value; OnPropertyChanged(nameof(FilterLess)); }
        }

        private bool _filterGreater = true;
        public bool FilterGreater
        {
            get => _filterGreater;
            set { _filterGreater = value; OnPropertyChanged(nameof(FilterGreater)); }
        }

        private bool _filterEqual;
        public bool FilterEqual
        {
            get => _filterEqual;
            set { _filterEqual = value; OnPropertyChanged(nameof(FilterEqual)); }
        }

        private bool _filterOutOfRange;
        public bool FilterOutOfRange
        {
            get => _filterOutOfRange;
            set { _filterOutOfRange = value; OnPropertyChanged(nameof(FilterOutOfRange)); }
        }

        private bool _filterInRange;
        public bool FilterInRange
        {
            get => _filterInRange;
            set { _filterInRange = value; OnPropertyChanged(nameof(FilterInRange)); }
        }

        private bool _filterAllRange = true;
        public bool FilterAllRange
        {
            get => _filterAllRange;
            set { _filterAllRange = value; OnPropertyChanged(nameof(FilterAllRange)); }
        }

        private string _toastMessage;
        public string ToastMessage
        {
            get => _toastMessage;
            set { _toastMessage = value; OnPropertyChanged(nameof(ToastMessage)); }
        }

        private bool _toastVisible;
        public bool ToastVisible
        {
            get => _toastVisible;
            set { _toastVisible = value; OnPropertyChanged(nameof(ToastVisible)); }
        }

        private bool _confirmDeleteVisible;
        public bool ConfirmDeleteVisible
        {
            get => _confirmDeleteVisible;
            set { _confirmDeleteVisible = value; OnPropertyChanged(nameof(ConfirmDeleteVisible)); }
        }

        private Ventil _pendingDelete;

        public string PendingDeleteName => _pendingDelete == null ? ""
            : $"{_pendingDelete.Name} (ID: {_pendingDelete.Id})";

        public ObservableCollection<string> History => MainWindowViewModel.History;

        public int KpiTotal => MainWindowViewModel.Ventili.Count;
        public int KpiValid => MainWindowViewModel.Ventili.Count(v => v.HasMeasurement && !v.IsOutOfRange);
        public int KpiOutOfRange => MainWindowViewModel.Ventili.Count(v => v.IsOutOfRange);
        public int KpiWaiting => MainWindowViewModel.Ventili.Count(v => !v.HasMeasurement);

        private void RefreshKpis()
        {
            OnPropertyChanged(nameof(KpiTotal));
            OnPropertyChanged(nameof(KpiValid));
            OnPropertyChanged(nameof(KpiOutOfRange));
            OnPropertyChanged(nameof(KpiWaiting));
        }

        private void OnVentilChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Ventil.LastMeasurement) ||
                e.PropertyName == nameof(Ventil.HasMeasurement) ||
                e.PropertyName == nameof(Ventil.IsOutOfRange))
                RefreshKpis();
        }

        public ICommand AddCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand ConfirmDeleteCommand { get; set; }
        public ICommand CancelDeleteCommand { get; set; }
        public ICommand ApplyFilterCommand { get; set; }
        public ICommand ResetFilterCommand { get; set; }
        public ICommand CancelFormCommand { get; set; }
        public ICommand EnterCommand { get; set; }
        public ICommand EscapeCommand { get; set; }

        public NetworkEntitiesViewModel()
        {
            EntityTypes = new ObservableCollection<EntityType>
            {
                MainWindowViewModel.KablovskiSenzor,
                MainWindowViewModel.DigitalniManometar
            };

            TypeFilterOptions = new ObservableCollection<string>
            {
                "Svi tipovi",
                "Kablovski senzor",
                "Digitalni manometar"
            };

            FilteredVentili = new ObservableCollection<Ventil>(MainWindowViewModel.Ventili);

            foreach (var v in MainWindowViewModel.Ventili)
                v.PropertyChanged += OnVentilChanged;

            MainWindowViewModel.Ventili.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (Ventil v in e.NewItems)
                    {
                        v.PropertyChanged += OnVentilChanged;
                        if (!FilteredVentili.Contains(v)) FilteredVentili.Add(v);
                    }
                if (e.OldItems != null)
                    foreach (Ventil v in e.OldItems)
                    {
                        v.PropertyChanged -= OnVentilChanged;
                        FilteredVentili.Remove(v);
                    }
                OnPropertyChanged(nameof(NextId));
                RefreshKpis();
            };

            AddCommand = new RelayCommand(_ => AddVentil());
            DeleteCommand = new RelayCommand(_ => RequestDelete(), _ => SelectedVentil != null);
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());
            ApplyFilterCommand = new RelayCommand(_ => ApplyFilter());
            ResetFilterCommand = new RelayCommand(_ => ResetFilter());
            CancelFormCommand = new RelayCommand(_ => CancelForm());
            EnterCommand = new RelayCommand(_ => OnEnter());
            EscapeCommand = new RelayCommand(_ => OnEscape());
        }

        private void OnEnter()
        {
            if (ConfirmDeleteVisible)
                ConfirmDelete();
            else
                ApplyFilter();
        }

        private void OnEscape()
        {
            if (ConfirmDeleteVisible)
                CancelDelete();
            else
                CancelForm();
        }

        private void ValidateName()
        {
            if (string.IsNullOrWhiteSpace(NewName))
                NameError = "Naziv ne sme biti prazan";
            else if (MainWindowViewModel.Ventili.Any(v => v.Name == NewName))
                NameError = "Entitet sa ovim nazivom već postoji";
            else
                NameError = "";
        }

        private void ValidateType()
        {
            TypeError = NewType == null ? "Tip je obavezan" : "";
        }

        private bool ValidateAll()
        {
            _nameTouched = true;
            _typeTouched = true;
            ValidateName();
            ValidateType();
            return string.IsNullOrEmpty(NameError) && string.IsNullOrEmpty(TypeError);
        }

        private void AddVentil()
        {
            if (!ValidateAll()) return;

            int newId = NextId;
            var ventil = new Ventil { Id = newId, Name = NewName, Type = NewType };
            MainWindowViewModel.Ventili.Add(ventil);

            MainWindowViewModel.AddHistory($"Dodat {NewName} · Tip: {NewType.Name} · ID: {newId}");
            MainWindowViewModel.PushUndo(() =>
            {
                MainWindowViewModel.Ventili.Remove(ventil);
                MainWindowViewModel.AddHistory($"Undo: Uklonjen {ventil.Name}");
            });

            RestartSimulator();
            ShowToast($"Entitet \"{NewName}\" uspešno dodat · Tip: {NewType.Name} · ID: {newId}");
            CancelForm();
        }

        private void RequestDelete()
        {
            if (SelectedVentil == null || ConfirmDeleteVisible) return;
            _pendingDelete = SelectedVentil;
            OnPropertyChanged(nameof(PendingDeleteName));
            ConfirmDeleteVisible = true;
        }

        private void ConfirmDelete()
        {
            if (_pendingDelete == null) return;

            var deleted = _pendingDelete;
            ConfirmDeleteVisible = false;
            _pendingDelete = null;

            MainWindowViewModel.Ventili.Remove(deleted);

            MainWindowViewModel.AddHistory($"Obrisan {deleted.Name} · ID: {deleted.Id}");
            MainWindowViewModel.PushUndo(() =>
            {
                MainWindowViewModel.Ventili.Add(deleted);
                MainWindowViewModel.AddHistory($"Undo: Vraćen {deleted.Name}");
            });

            RestartSimulator();
            ShowToast($"Entitet \"{deleted.Name}\" uspešno obrisan · ID: {deleted.Id}");
            OnPropertyChanged(nameof(NextId));
        }

        private void CancelDelete()
        {
            _pendingDelete = null;
            ConfirmDeleteVisible = false;
        }

        private void ApplyFilter()
        {
            var result = MainWindowViewModel.Ventili.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedTypeFilter) && SelectedTypeFilter != "Svi tipovi")
                result = result.Where(v => v.Type.Name == SelectedTypeFilter);

            if (!string.IsNullOrWhiteSpace(IdFilterValue) && int.TryParse(IdFilterValue, out int idVal))
            {
                if (FilterLess) result = result.Where(v => v.Id < idVal);
                else if (FilterGreater) result = result.Where(v => v.Id > idVal);
                else if (FilterEqual) result = result.Where(v => v.Id == idVal);
            }

            if (FilterOutOfRange)
                result = result.Where(v => v.IsOutOfRange);
            else if (FilterInRange)
                result = result.Where(v => !v.IsOutOfRange && v.LastMeasurement != -1);

            FilteredVentili = new ObservableCollection<Ventil>(result);

            MainWindowViewModel.AddHistory($"Filter: {SelectedTypeFilter}, ID {GetIdFilterLabel()}, {GetRangeFilterLabel()}");
        }

        private string GetIdFilterLabel()
        {
            if (string.IsNullOrWhiteSpace(IdFilterValue)) return "svi ID";
            string op = FilterLess ? "<" : FilterGreater ? ">" : "=";
            return $"ID {op} {IdFilterValue}";
        }

        private string GetRangeFilterLabel()
        {
            if (FilterOutOfRange) return "Van opsega";
            if (FilterInRange) return "Unutar opsega";
            return "Svi";
        }

        private void ResetFilter()
        {
            SelectedTypeFilter = "Svi tipovi";
            IdFilterValue = "";
            FilterGreater = true;
            FilterLess = false;
            FilterEqual = false;
            FilterAllRange = true;
            FilterOutOfRange = false;
            FilterInRange = false;
            FilteredVentili = new ObservableCollection<Ventil>(MainWindowViewModel.Ventili);
        }

        private void CancelForm()
        {
            NewName = "";
            NewType = null;
            NameError = "";
            TypeError = "";
            _nameTouched = false;
            _typeTouched = false;
            OnPropertyChanged(nameof(NextId));
        }

        private void RestartSimulator()
        {
            try
            {
                foreach (var p in System.Diagnostics.Process.GetProcessesByName("MeteringSimulator"))
                    p.Kill();
                System.Diagnostics.Process.Start(
                    "../../../MeteringSimulator/MeteringSimulator/bin/Debug/MeteringSimulator.exe");
            }
            catch { }
        }

        private async void ShowToast(string message)
        {
            ToastMessage = message;
            ToastVisible = true;
            await System.Threading.Tasks.Task.Delay(3500);
            ToastVisible = false;
        }
    }
}
