using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CRC32
{
    public class NativeMethods
    {
        protected static class Libloaderapi
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

            [DllImport("kernel32", SetLastError = true)]
            public static extern IntPtr GetModuleHandleA(string lpModuleName);
        }
        protected static class Debugapi
        {
            [DllImport("kernel32", SetLastError = true)]
            public static extern bool IsDebuggerPresent();
        }
        protected static class Processthreadsapi
        {
            [DllImport("kernel32", SetLastError = true)]
            public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);
            
            [DllImport("Kernel32", SetLastError = true)]
            public static extern bool GetExitCodeProcess(IntPtr hProcess, out IntPtr lpExitCode);
        }
        protected static class Memoryapi
        {
            [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
            public static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, Winnt.AllocationType flAllocationType, Winnt.MemoryProtection flProtect);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool VirtualFree(IntPtr lpAddress, int size, int dwFreeType);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        }
        protected static class Wdm
        {
            [DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
            public static extern void ZeroMemory(IntPtr dest, IntPtr size);
        }
        protected static class Winnt
        {
            public enum AllocationType
            {
                MEM_COMMIT = 0x1000,
                MEM_FREE = 0x10000,
                MEM_RESERVE = 0x2000,
            }
            public enum ProcessAccessFlags
            {
                PROCESS_ALL_ACCESS = 0xFFFF,
            }
            public enum MemoryProtection : uint
            {
                PAGE_EXECUTE = 0x10,
                PAGE_EXECUTE_READ = 0x20,
                PAGE_EXECUTE_READWRITE = 0x40,
                PAGE_EXECUTE_WRITECOPY = 0x80,
                PAGE_NOACCESS = 0x01,
                PAGE_READONLY = 0x02,
                PAGE_READWRITE = 0x04,
                PAGE_WRITECOPY = 0x08,
                PAGE_TARGETS_INVALID = 0x40000000,
                PAGE_TARGETS_NO_UPDATE = 0x40000000,
                PAGE_GUARD = 0x100,
                PAGE_NOCACHE = 0x200,
                PAGE_WRITECOMBINE = 0x400,

            }
        }
    }
}
