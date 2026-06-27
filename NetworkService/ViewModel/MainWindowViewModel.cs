using NetworkService.Commands;
using NetworkService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        public static ObservableCollection<Ventil> Ventili { get; set; }
            = new ObservableCollection<Ventil>();

        public static EntityType KablovskiSenzor = new EntityType
        {
            Name = "Kablovski senzor",
            ImagePath = "/Images/kablovski.png"
        };
        public static EntityType DigitalniManometar = new EntityType
        {
            Name = "Digitalni manometar",
            ImagePath = "/Images/digitalni.png"
        };

        public static ObservableCollection<string> History { get; set; }
            = new ObservableCollection<string>();

        private static Stack<Action> _undoStack = new Stack<Action>();

        private static NetworkDisplayViewModel _displayViewModel = new NetworkDisplayViewModel();
        private static MeasurementGraphViewModel _graphViewModel = new MeasurementGraphViewModel();

        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(nameof(CurrentViewModel)); }
        }

        private string _currentViewName = "Entities";
        public string CurrentViewName
        {
            get => _currentViewName;
            set { _currentViewName = value; OnPropertyChanged(nameof(CurrentViewName)); }
        }

        public ICommand NavigateCommand { get; set; }
        public ICommand UndoCommand { get; set; }
        public ICommand UndoAllCommand { get; set; }

        public MainWindowViewModel()
        {
            Ventili.Add(new Ventil { Id = 1, Name = "Ventil A-01", Type = KablovskiSenzor });
            Ventili.Add(new Ventil { Id = 2, Name = "Ventil B-02", Type = DigitalniManometar });
            Ventili.Add(new Ventil { Id = 3, Name = "Ventil C-03", Type = KablovskiSenzor });

            NavigateCommand = new RelayCommand(Navigate);
            UndoCommand = new RelayCommand(_ => Undo(), _ => _undoStack.Count > 0);
            UndoAllCommand = new RelayCommand(_ => UndoAll(), _ => _undoStack.Count > 0);

            CurrentViewModel = new NetworkEntitiesViewModel();

            StartTcpListener();
        }

        private void Navigate(object param)
        {
            switch (param?.ToString())
            {
                case "Entities": CurrentViewModel = new NetworkEntitiesViewModel(); CurrentViewName = "Entities"; break;
                case "Display":  CurrentViewModel = _displayViewModel; CurrentViewName = "Display"; break;
                case "Graph":    CurrentViewModel = _graphViewModel; CurrentViewName = "Graph"; break;
            }
        }

        public static void AddHistory(string action)
        {
            History.Insert(0, $"{History.Count + 1}. {action}");
        }

        public static void PushUndo(Action undoAction)
        {
            _undoStack.Push(undoAction);
        }

        private void Undo()
        {
            if (_undoStack.Count > 0)
                _undoStack.Pop()?.Invoke();
        }

        private void UndoAll()
        {
            while (_undoStack.Count > 0)
                _undoStack.Pop()?.Invoke();
        }

        private void StartTcpListener()
        {
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var thread = new Thread(() =>
            {
                while (true)
                {
                    var client = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        using (var stream = client.GetStream())
                        {
                            byte[] buf = new byte[1024];
                            int n = stream.Read(buf, 0, buf.Length);
                            string msg = Encoding.ASCII.GetString(buf, 0, n);

                            if (msg == "Need object count")
                            {
                                byte[] data = Encoding.ASCII.GetBytes(Ventili.Count.ToString());
                                stream.Write(data, 0, data.Length);
                            }
                            else
                            {
                                try
                                {
                                    string[] parts = msg.Split(':');
                                    int index = int.Parse(parts[0].Split('_')[1]);
                                    double value = double.Parse(
                                        parts[1].Replace(',', '.'),
                                        System.Globalization.CultureInfo.InvariantCulture);

                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        if (index < Ventili.Count)
                                        {
                                            Ventili[index].LastMeasurement = value;
                                            LogService.Log(Ventili[index].Name, value);
                                        }
                                    });
                                }
                                catch { }
                            }
                        }
                    });
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }
    }
}
