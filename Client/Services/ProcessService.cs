using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services
{
    public class ProcessService
    {
        public async Task WaitProcess(string processName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    return;
                }
                await Task.Delay(Utils.Constants.MONITORING_INTERVAL_MS, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public Process? GetProcess(string processName)
        {
            return Process.GetProcessesByName(processName).FirstOrDefault();
        }
    }
}
