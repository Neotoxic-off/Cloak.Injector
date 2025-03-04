using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Utils
{
    public static class Constants
    {
        // Process access flags
        public const int PROCESS_CREATE_THREAD = 0x0002;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Memory allocation flags
        public const uint MEM_COMMIT = 0x00001000;
        public const uint MEM_RESERVE = 0x00002000;
        public const uint MEM_RELEASE = 0x00008000;

        public const uint TOKEN_QUERY = 0x0008;
        public const uint WAIT_OBJECT_0 = 0x00000000;
        public const uint WAIT_TIMEOUT = 0x00000102;

        public static uint PAGE_READWRITE = 4;

        public static string DLL = "Cloak.dll";
        public static string PROCESS_NAME = "Notepad";
        public static string LOGS_PATH = "Logs.txt";

        public static int MONITORING_INTERVAL_MS = 1000;
    }
}
