
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private LogService? _logService;

        private ProcessService _processService;
        private InjectionService _injectionService;

        public MainViewModel()
        {
            LogService = new LogService();

            _processService = new ProcessService();
            _injectionService = new InjectionService(LogService);
        }

        private async Task Wait()
        {
            CancellationTokenSource token = new CancellationTokenSource();

            LogService?.Record(Utils.Logs.LOG_WAITING_PROCESS);
            await _processService.WaitProcess(Utils.Constants.PROCESS_NAME, token.Token);
            LogService?.Record(Utils.Logs.LOG_FOUND_PROCESS);
        }

        public async void Attach()
        {
            await Wait();
            Inject();
        }

        private void Inject()
        {
            Process? process = null;

            LogService?.Record(Utils.Logs.LOG_INJECTING);

            process = _processService.GetProcess(Utils.Constants.PROCESS_NAME);
            _injectionService.InjectDll(process, Utils.Constants.DLL);
        }

        public void Detach()
        {
            Process? process = _processService.GetProcess(Utils.Constants.PROCESS_NAME);

            LogService?.Record(Utils.Logs.LOG_EJECTING);

            _injectionService.EjectDll(process, Utils.Constants.DLL);
        }

        [RelayCommand]
        private void SaveLogs()
        {
            LogService?.Save();
        }
    }
}
