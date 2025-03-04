using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services
{
    public partial class LogService : ObservableObject
    {
        [ObservableProperty]
        private string? _log;

        [ObservableProperty]
        private ObservableCollection<string> _logs;

        public LogService()
        {
            Logs = new ObservableCollection<string>();
        }

        public void Record(string message)
        {
            Log = message;
            Logs.Add(message);
        }

        public void Save()
        {
            System.IO.File.WriteAllLines(
                Utils.Constants.LOGS_PATH,
                Logs
            );
        }
    }
}
