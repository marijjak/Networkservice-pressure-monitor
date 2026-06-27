using System.ComponentModel;

namespace NetworkService.Model
{
    public class Ventil : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }

        private double _lastMeasurement = -1;
        public double LastMeasurement
        {
            get => _lastMeasurement;
            set
            {
                _lastMeasurement = value;
                OnPropertyChanged(nameof(LastMeasurement));
                OnPropertyChanged(nameof(IsOutOfRange));
                OnPropertyChanged(nameof(HasMeasurement));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(MeasurementDisplay));
            }
        }

        public bool IsOutOfRange => _lastMeasurement != -1 && (_lastMeasurement < 5 || _lastMeasurement > 16);
        public bool HasMeasurement => _lastMeasurement != -1;

        public string Status => _lastMeasurement == -1 ? "Čeka podatke"
                              : IsOutOfRange ? "Van opsega" : "Validno";

        public string MeasurementDisplay => _lastMeasurement == -1 ? "—"
                                          : $"{_lastMeasurement:0.##} MP";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
