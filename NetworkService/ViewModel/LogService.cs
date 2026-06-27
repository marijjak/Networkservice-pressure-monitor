using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace NetworkService.ViewModel
{
    public static class LogService
    {
        private static readonly string LogPath = "log.txt";

        public static ObservableCollection<MeasurementEntry> AllMeasurements { get; }
            = new ObservableCollection<MeasurementEntry>();

        public static void Log(string entityName, double value)
        {
            bool outOfRange = value < 5 || value > 16;
            string status = outOfRange ? "VAN OPSEGA" : "Validno";
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {entityName} | {value:0.##} MP | {status}";

            File.AppendAllText(LogPath, line + Environment.NewLine);

            AllMeasurements.Add(new MeasurementEntry
            {
                EntityName = entityName,
                Value = value,
                Time = DateTime.Now,
                IsOutOfRange = outOfRange
            });
        }
    }

    public class MeasurementEntry
    {
        public string EntityName { get; set; }
        public double Value { get; set; }
        public DateTime Time { get; set; }
        public bool IsOutOfRange { get; set; }
    }
}
