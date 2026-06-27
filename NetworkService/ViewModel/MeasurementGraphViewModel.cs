using NetworkService.Commands;
using NetworkService.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace NetworkService.ViewModel
{
    public class MeasurementGraphViewModel : BaseViewModel
    {
        public ObservableCollection<Ventil> Ventili => MainWindowViewModel.Ventili;

        private Ventil _selectedVentil;
        public Ventil SelectedVentil
        {
            get => _selectedVentil;
            set
            {
                _selectedVentil = value;
                OnPropertyChanged(nameof(SelectedVentil));
                OnPropertyChanged(nameof(SelectedEntityLabel));
                LoadMeasurements();
            }
        }

        public string SelectedEntityLabel => _selectedVentil == null
            ? "Odaberi entitet..."
            : $"{_selectedVentil.Name} (ID: {_selectedVentil.Id})";

        private ObservableCollection<MeasurementEntry> _measurements;
        public ObservableCollection<MeasurementEntry> Measurements
        {
            get => _measurements;
            set { _measurements = value; OnPropertyChanged(nameof(Measurements)); }
        }

        public double KablovskiPercent
        {
            get
            {
                if (Ventili.Count == 0) return 50;
                int count = Ventili.Count(v => v.Type?.Name == "Kablovski senzor");
                return (double)count / Ventili.Count * 100;
            }
        }

        public double DigitalniPercent
        {
            get
            {
                if (Ventili.Count == 0) return 50;
                int count = Ventili.Count(v => v.Type?.Name == "Digitalni manometar");
                return (double)count / Ventili.Count * 100;
            }
        }

        public string KablovskiLabel
        {
            get
            {
                int count = Ventili.Count(v => v.Type?.Name == "Kablovski senzor");
                return $"Kablovski senzor  {count} · {KablovskiPercent:0}%";
            }
        }

        public string DigitalniLabel
        {
            get
            {
                int count = Ventili.Count(v => v.Type?.Name == "Digitalni manometar");
                return $"Digitalni manometar  {count} · {DigitalniPercent:0}%";
            }
        }

        public ObservableCollection<string> History => MainWindowViewModel.History;

        public ICommand SelectVentilCommand { get; set; }

        public MeasurementGraphViewModel()
        {
            Measurements = new ObservableCollection<MeasurementEntry>();

            SelectVentilCommand = new RelayCommand(v =>
            {
                if (v is Ventil ventil)
                {
                    SelectedVentil = ventil;
                    MainWindowViewModel.AddHistory($"Izabran {ventil.Name}");
                }
            });

            LogService.AllMeasurements.CollectionChanged += OnMeasurementsChanged;

            MainWindowViewModel.Ventili.CollectionChanged += OnVentiliChanged;
        }

        private void OnMeasurementsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedVentil != null)
                LoadMeasurements();
        }

        private void OnVentiliChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(KablovskiPercent));
            OnPropertyChanged(nameof(DigitalniPercent));
            OnPropertyChanged(nameof(KablovskiLabel));
            OnPropertyChanged(nameof(DigitalniLabel));
            OnPropertyChanged(nameof(Ventili));
        }

        public void LoadMeasurements()
        {
            if (SelectedVentil == null) return;

            var filtered = LogService.AllMeasurements
                .Where(m => m.EntityName == SelectedVentil.Name)
                .ToList();

            var last5 = filtered.Skip(System.Math.Max(0, filtered.Count - 5)).ToList();
            Measurements = new ObservableCollection<MeasurementEntry>(last5);
        }
    }
}
