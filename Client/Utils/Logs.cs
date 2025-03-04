using System;

namespace Client.Utils
{
    public static class Logs
    {
        // General Process Logs
        public const string LOG_WAITING_PROCESS = "Waiting for target process...";
        public const string LOG_FOUND_PROCESS = "Target process found";
        public const string LOG_PROCESS_NOT_FOUND = "Target process not found!";
        public const string LOG_PROCESS_OPEN_FAILED = "Failed to open target process!";

        // DLL Injection Logs
        public const string LOG_INJECTING = "Injecting DLL...";
        public const string LOG_INJECTION_SUCCESS = "DLL injection successful!";
        public const string LOG_INJECTION_FAILED = "DLL injection failed!";

        // Injection Errors
        public const string LOG_TARGET_PROCESS_NULL = "Injection failed: Target process is null.";
        public const string LOG_DLL_NOT_FOUND = "Injection failed: DLL file does not exist.";
        public const string LOG_OPEN_PROCESS_FAILED = "Injection failed: Could not open target process.";
        public const string LOG_LOADLIBRARY_NOT_FOUND = "Injection failed: Could not find LoadLibraryA address.";
        public const string LOG_MEMORY_ALLOCATION_FAILED = "Injection failed: Could not allocate memory in target process.";
        public const string LOG_WRITE_MEMORY_FAILED = "Injection failed: Could not write DLL path to process memory.";
        public const string LOG_CREATE_THREAD_FAILED = "Injection failed: Could not create remote thread.";

        // DLL Ejection Logs
        public const string LOG_EJECTING = "Ejecting DLL...";
        public const string LOG_EJECTION_SUCCESS = "DLL ejection successful!";
        public const string LOG_EJECTION_FAILED = "DLL ejection failed!";

        // Ejection Errors
        public const string LOG_EJECTION_PROCESS_NULL = "Ejection failed: Target process is null.";
        public const string LOG_DLL_NOT_IN_PROCESS = "Ejection failed: DLL not found in the target process.";
        public const string LOG_GET_FREELIBRARY_FAILED = "Ejection failed: Could not find FreeLibrary address.";
        public const string LOG_CREATE_EJECTION_THREAD_FAILED = "Ejection failed: Could not create remote thread.";

        // Memory Cleanup Logs
        public const string LOG_CLEANUP_MEMORY = "Cleaning up allocated memory...";
        public const string LOG_CLEANUP_MEMORY_SUCCESS = "Memory cleanup successful.";
        public const string LOG_CLEANUP_MEMORY_FAILED = "Memory cleanup failed!";

        // Thread Management Logs
        public const string LOG_THREAD_STARTED = "Remote thread started successfully.";
        public const string LOG_THREAD_WAITING = "Waiting for remote thread to complete...";
        public const string LOG_THREAD_COMPLETED = "Remote thread execution completed.";
        public const string LOG_THREAD_CLOSE_FAILED = "Failed to close remote thread handle.";
    }
}
