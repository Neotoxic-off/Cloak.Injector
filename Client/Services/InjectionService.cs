using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Client.Utils;

namespace Client.Services
{
    public class InjectionService
    {
        public LogService? LogService { get; set; }

        public InjectionService(LogService? logService = null)
        {
            LogService = logService;
        }

        public bool InjectDll(Process targetProcess, string dllPath)
        {
            bool success;
            byte[] pathBytes;
            uint allocSize;
            IntPtr procHandle = IntPtr.Zero;
            IntPtr loadLibraryAddr = IntPtr.Zero;
            IntPtr allocMemAddress = IntPtr.Zero;

            LogService?.Record(Logs.LOG_INJECTING);
            if (!ValidateInjection(targetProcess, dllPath))
            {
                return false;
            }

            dllPath = Path.GetFullPath(dllPath);
            procHandle = OpenTargetProcess(targetProcess);
            if (procHandle == IntPtr.Zero) return false;

            loadLibraryAddr = GetLoadLibraryAddress();
            if (loadLibraryAddr == IntPtr.Zero)
            {
                CloseProcessHandle(procHandle);
                return false;
            }

            pathBytes = GetPathBytes(dllPath);
            allocSize = (uint)pathBytes.Length;
            LogService?.Record($"Allocating {allocSize} bytes for DLL path");

            allocMemAddress = AllocateMemory(procHandle, allocSize);
            if (allocMemAddress == IntPtr.Zero)
            {
                CloseProcessHandle(procHandle);
                return false;
            }

            if (!WriteMemory(procHandle, allocMemAddress, pathBytes))
            {
                CleanupInjection(procHandle, allocMemAddress, false);
                return false;
            }

            success = CreateRemoteThread(procHandle, loadLibraryAddr, allocMemAddress);
            CleanupInjection(procHandle, allocMemAddress, success);
            LogService?.Record(success ? Logs.LOG_INJECTION_SUCCESS : Logs.LOG_INJECTION_FAILED);
            return success;
        }

        public bool EjectDll(Process targetProcess, string dllPath)
        {
            IntPtr dllBaseAddr = IntPtr.Zero;
            IntPtr procHandle = IntPtr.Zero;
            bool success;

            LogService?.Record(Logs.LOG_EJECTING);
            if (targetProcess == null)
            {
                LogService?.Record(Logs.LOG_EJECTION_PROCESS_NULL);
                return false;
            }

            dllBaseAddr = GetDllBaseAddress(targetProcess, Path.GetFileName(dllPath));
            if (dllBaseAddr == IntPtr.Zero)
            {
                LogService?.Record(Logs.LOG_DLL_NOT_IN_PROCESS);
                return false;
            }

            procHandle = OpenTargetProcess(targetProcess);
            if (procHandle == IntPtr.Zero) return false;

            success = UnloadDll(procHandle, dllBaseAddr);
            CloseProcessHandle(procHandle);
            LogService?.Record(success ? Logs.LOG_EJECTION_SUCCESS : Logs.LOG_EJECTION_FAILED);
            return success;
        }

        private bool ValidateInjection(Process targetProcess, string dllPath)
        {
            if (targetProcess == null)
            {
                LogService?.Record(Logs.LOG_TARGET_PROCESS_NULL);
                return false;
            }

            if (!File.Exists(dllPath))
            {
                LogService?.Record(Logs.LOG_DLL_NOT_FOUND);
                return false;
            }

            return true;
        }

        private IntPtr OpenTargetProcess(Process targetProcess)
        {
            IntPtr procHandle = IntPtr.Zero;

            procHandle = Utils.Kernel32.OpenProcess(
                Utils.Constants.PROCESS_CREATE_THREAD |
                Utils.Constants.PROCESS_QUERY_INFORMATION |
                Utils.Constants.PROCESS_VM_OPERATION |
                Utils.Constants.PROCESS_VM_WRITE |
                Utils.Constants.PROCESS_VM_READ,
                false,
                targetProcess.Id
            );

            if (procHandle == IntPtr.Zero)
            {
                LogProcessOpenFailed();
            }
            else
            {
                LogProcessOpened(procHandle);
            }

            return procHandle;
        }

        private void LogProcessOpenFailed()
        {
            int lastError = Marshal.GetLastWin32Error();
            LogService?.Record(Logs.LOG_PROCESS_OPEN_FAILED);
        }

        private void LogProcessOpened(IntPtr procHandle)
        {
            LogService?.Record($"Successfully opened process with handle: 0x{procHandle.ToInt64():X}");
        }

        private IntPtr GetLoadLibraryAddress()
        {
            IntPtr kernelModule = IntPtr.Zero;
            IntPtr loadLibraryAddr = IntPtr.Zero;

            kernelModule = Utils.Kernel32.GetModuleHandle("kernel32.dll");
            if (kernelModule == IntPtr.Zero)
            {
                LogKernelModuleError();
                return IntPtr.Zero;
            }

            loadLibraryAddr = Utils.Kernel32.GetProcAddress(kernelModule, "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                LogLoadLibraryError();
            }
            else
            {
                LogService?.Record($"LoadLibraryA address: 0x{loadLibraryAddr.ToInt64():X}");
            }

            return loadLibraryAddr;
        }

        private void LogKernelModuleError()
        {
            int lastError = Marshal.GetLastWin32Error();
            LogService?.Record($"Failed to get kernel32.dll handle. Error: {lastError} (0x{lastError:X8})");
        }

        private void LogLoadLibraryError()
        {
            int lastError = Marshal.GetLastWin32Error();
            LogService?.Record(Logs.LOG_LOADLIBRARY_NOT_FOUND);
            LogService?.Record($"Failed to get LoadLibraryA address. Error: {lastError} (0x{lastError:X8})");
        }

        private IntPtr AllocateMemory(IntPtr procHandle, uint size)
        {
            IntPtr allocMemAddress = IntPtr.Zero;

            allocMemAddress = Utils.Kernel32.VirtualAllocEx(
                procHandle,
                IntPtr.Zero,
                size,
                Utils.Constants.MEM_COMMIT | Utils.Constants.MEM_RESERVE,
                Utils.Constants.PAGE_READWRITE
            );

            if (allocMemAddress == IntPtr.Zero)
            {
                LogMemoryAllocationFailed();
            }
            else
            {
                LogService?.Record($"Memory allocated at: 0x{allocMemAddress.ToInt64():X}");
            }

            return allocMemAddress;
        }

        private void LogMemoryAllocationFailed()
        {
            int lastError = Marshal.GetLastWin32Error();
            LogService?.Record(Logs.LOG_MEMORY_ALLOCATION_FAILED);
            LogService?.Record($"Memory allocation failed. Error: {lastError} (0x{lastError:X8})");
        }

        private byte[] GetPathBytes(string dllPath)
        {
            return Encoding.ASCII.GetBytes(dllPath + '\0');
        }

        private bool WriteMemory(IntPtr procHandle, IntPtr allocMemAddress, byte[] data)
        {
            UIntPtr bytesWritten;
            bool success;

            success = Utils.Kernel32.WriteProcessMemory(
                procHandle,
                allocMemAddress,
                data,
                (uint)data.Length,
                out bytesWritten
            );

            if (!success)
            {
                LogWriteMemoryFailed();
            }
            else
            {
                LogService?.Record($"Successfully wrote {bytesWritten.ToUInt32()} bytes to memory");
            }

            return success;
        }

        private void LogWriteMemoryFailed()
        {
            int lastError = Marshal.GetLastWin32Error();
            LogService?.Record(Logs.LOG_WRITE_MEMORY_FAILED);
            LogService?.Record($"Write memory failed. Error: {lastError} (0x{lastError:X8})");
        }

        private bool CreateRemoteThread(IntPtr procHandle, IntPtr loadLibraryAddr, IntPtr allocMemAddress)
        {
            IntPtr threadHandle = IntPtr.Zero;

            threadHandle = Utils.Kernel32.CreateRemoteThread(
                procHandle,
                IntPtr.Zero,
                0,
                loadLibraryAddr,
                allocMemAddress,
                0,
                IntPtr.Zero
            );

            if (threadHandle == IntPtr.Zero)
            {
                LogCreateRemoteThreadFailed();
                return false;
            }

            LogThreadStarted(threadHandle);
            bool success = WaitForThreadCompletion(threadHandle);
            Utils.Kernel32.CloseHandle(threadHandle);
            return success;
        }

        private void LogCreateRemoteThreadFailed()
        {
            int lastError = Marshal.GetLastWin32Error();
            LogService?.Record(Logs.LOG_CREATE_THREAD_FAILED);
            LogService?.Record($"Create remote thread failed. Error: {lastError} (0x{lastError:X8})");
        }

        private void LogThreadStarted(IntPtr threadHandle)
        {
            LogService?.Record(Logs.LOG_THREAD_STARTED);
            LogService?.Record($"Remote thread created with handle: 0x{threadHandle.ToInt64():X}");
        }

        private bool WaitForThreadCompletion(IntPtr threadHandle)
        {
            uint waitResult;

            waitResult = Utils.Kernel32.WaitForSingleObject(threadHandle, 10000);

            if (waitResult == Utils.Constants.WAIT_OBJECT_0)
            {
                LogService?.Record("Thread wait completed successfully");
                return VerifyThreadExitCode(threadHandle);
            }
            else if (waitResult == Utils.Constants.WAIT_TIMEOUT)
            {
                LogService?.Record("Thread wait timed out");
            }
            else
            {
                LogThreadWaitFailed(waitResult);
            }

            return false;
        }

        private void LogThreadWaitFailed(uint waitResult)
        {
            int lastError = Marshal.GetLastWin32Error();

            LogService?.Record($"Thread wait failed. Error: {lastError} (0x{lastError:X8})");
        }

        private bool VerifyThreadExitCode(IntPtr threadHandle)
        {
            uint exitCode = 0;

            if (Utils.Kernel32.GetExitCodeThread(threadHandle, out exitCode))
            {
                LogService?.Record($"Thread exit code: 0x{exitCode:X8}");
                if (exitCode == 0)
                {
                    LogService?.Record("Thread exited with code 0, which may indicate DLL load failure");
                    return false;
                }
            }
            else
            {
                LogGetExitCodeFailed();
            }

            return true;
        }

        private void LogGetExitCodeFailed()
        {
            int lastError = Marshal.GetLastWin32Error();

            LogService?.Record($"Failed to get thread exit code. Error: {lastError} (0x{lastError:X8})");
        }

        private void CleanupInjection(IntPtr procHandle, IntPtr allocMemAddress, bool success)
        {
            if (allocMemAddress != IntPtr.Zero && !success)
            {
                LogService?.Record(Logs.LOG_CLEANUP_MEMORY);
                if (Utils.Kernel32.VirtualFreeEx(procHandle, allocMemAddress, 0, Utils.Constants.MEM_RELEASE))
                {
                    LogService?.Record(Logs.LOG_CLEANUP_MEMORY_SUCCESS);
                }
                else
                {
                    LogCleanupMemoryFailed();
                }
            }

            CloseProcessHandle(procHandle);
        }

        private void LogCleanupMemoryFailed()
        {
            int lastError = Marshal.GetLastWin32Error();

            LogService?.Record(Logs.LOG_CLEANUP_MEMORY_FAILED);
        }

        private void CloseProcessHandle(IntPtr procHandle)
        {
            if (procHandle != IntPtr.Zero)
            {
                Utils.Kernel32.CloseHandle(procHandle);
                LogService?.Record("Process handle closed");
            }
        }

        private bool UnloadDll(IntPtr procHandle, IntPtr dllBaseAddr)
        {
            IntPtr freeLibraryAddr = IntPtr.Zero;
            IntPtr threadHandle = IntPtr.Zero;
            uint waitResult;

            freeLibraryAddr = Utils.Kernel32.GetProcAddress(
                Utils.Kernel32.GetModuleHandle("kernel32.dll"),
                "FreeLibrary"
            );

            if (freeLibraryAddr == IntPtr.Zero)
            {
                LogGetFreeLibraryFailed();
                return false;
            }

            threadHandle = Utils.Kernel32.CreateRemoteThread(
                procHandle,
                IntPtr.Zero,
                0,
                freeLibraryAddr,
                dllBaseAddr,
                0,
                IntPtr.Zero
            );

            if (threadHandle == IntPtr.Zero)
            {
                LogCreateEjectionThreadFailed();
                return false;
            }

            LogThreadStarted(threadHandle);
            waitResult = Utils.Kernel32.WaitForSingleObject(threadHandle, 5000);

            if (waitResult != Utils.Constants.WAIT_OBJECT_0)
            {
                LogService?.Record($"Ejection thread wait failed or timed out. Result: {waitResult}");
            }

            LogService?.Record(Logs.LOG_THREAD_COMPLETED);
            Utils.Kernel32.CloseHandle(threadHandle);
            return true;
        }

        private void LogGetFreeLibraryFailed()
        {
            int lastError = Marshal.GetLastWin32Error();

            LogService?.Record(Logs.LOG_GET_FREELIBRARY_FAILED);
        }

        private void LogCreateEjectionThreadFailed()
        {
            int lastError = Marshal.GetLastWin32Error();

            LogService?.Record(Logs.LOG_CREATE_EJECTION_THREAD_FAILED);
        }

        private IntPtr GetDllBaseAddress(Process process, string dllName)
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (string.Equals(module.ModuleName, dllName, StringComparison.OrdinalIgnoreCase))
                {
                    LogService?.Record($"Found DLL '{dllName}' at base address: 0x{module.BaseAddress.ToInt64():X}");
                    return module.BaseAddress;
                }
            }
            LogService?.Record($"DLL '{dllName}' not found in process modules");
            return IntPtr.Zero;
        }
    }
}
